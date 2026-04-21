using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeGraphQlSchema;

/// <summary>
/// Feature: AnalyzeGraphQlSchema — analisa um schema GraphQL SDL e persiste um snapshot estruturado.
///
/// Parsing leve sem dependências externas: contagem de tipos, campos e operations
/// por análise de keywords na SDL. Adequado para schemas típicos de produção até 512 KB.
///
/// O snapshot persistido permite diff semântico futuro e detecção de breaking changes
/// sem re-parsing do schema completo.
///
/// Wave G.3 — GraphQL Schema Analysis (GAP-CTR-01).
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class AnalyzeGraphQlSchema
{
    public sealed record Command(
        Guid ApiAssetId,
        string ContractVersion,
        string SchemaContent,
        Guid TenantId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ContractVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SchemaContent).NotEmpty().MaximumLength(524_288); // 512 KB
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        IGraphQlSchemaSnapshotRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var analysis = ParseSchema(request.SchemaContent);

            var snapshot = GraphQlSchemaSnapshot.Create(
                apiAssetId: request.ApiAssetId,
                contractVersion: request.ContractVersion,
                schemaContent: request.SchemaContent,
                typeCount: analysis.TypeNames.Count,
                fieldCount: analysis.TotalFieldCount,
                operationCount: analysis.Operations.Count,
                typeNamesJson: JsonSerializer.Serialize(analysis.TypeNames),
                operationsJson: JsonSerializer.Serialize(analysis.Operations),
                fieldsByTypeJson: JsonSerializer.Serialize(analysis.FieldsByType),
                hasQueryType: analysis.HasQueryType,
                hasMutationType: analysis.HasMutationType,
                hasSubscriptionType: analysis.HasSubscriptionType,
                tenantId: request.TenantId,
                capturedAt: clock.UtcNow);

            repository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                SnapshotId: snapshot.Id.Value,
                ApiAssetId: snapshot.ApiAssetId,
                ContractVersion: snapshot.ContractVersion,
                TypeCount: snapshot.TypeCount,
                FieldCount: snapshot.FieldCount,
                OperationCount: snapshot.OperationCount,
                HasQueryType: snapshot.HasQueryType,
                HasMutationType: snapshot.HasMutationType,
                HasSubscriptionType: snapshot.HasSubscriptionType,
                CapturedAt: snapshot.CapturedAt);
        }

        /// <summary>
        /// Analisa o schema GraphQL SDL por keywords.
        /// Parsing leve e sem dependências externas: conta tipos e operations linha a linha.
        /// Suficiente para governança e diff semântico; não substitui parser SDL completo.
        /// </summary>
        private static SchemaAnalysis ParseSchema(string schema)
        {
            var lines = schema.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var typeNames = new List<string>();
            var operations = new List<OperationDto>();
            var fieldsByType = new Dictionary<string, List<string>>();
            string? currentType = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith('#')) continue;

                // Object/Input/Interface/Enum/Union type declarations
                if (IsTypeDeclaration(line, out var typeName))
                {
                    currentType = typeName;
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        typeNames.Add(typeName);
                        fieldsByType[typeName] = [];
                    }
                    continue;
                }

                // Fields inside types — lines with a colon that aren't empty or opening braces
                if (currentType is not null && line.Contains(':') && !line.StartsWith('{') && !line.StartsWith('}'))
                {
                    // Extract field name: handle arguments like `createUser(name: String!): User`
                    // Split on first '(' or ':' to get just the field/operation name
                    var fieldName = line.Split(['(', ':'])[0].Trim().TrimStart('-').Trim();
                    if (!string.IsNullOrWhiteSpace(fieldName) && !fieldName.StartsWith('#'))
                    {
                        fieldsByType[currentType].Add(fieldName);

                        // Detect query/mutation/subscription operations (fields inside Query/Mutation/Subscription)
                        if (currentType == "Query")
                            operations.Add(new OperationDto(fieldName, "Query"));
                        else if (currentType == "Mutation")
                            operations.Add(new OperationDto(fieldName, "Mutation"));
                        else if (currentType == "Subscription")
                            operations.Add(new OperationDto(fieldName, "Subscription"));
                    }
                }

                if (line.StartsWith('}')) currentType = null;
            }

            var hasQuery = typeNames.Contains("Query");
            var hasMutation = typeNames.Contains("Mutation");
            var hasSubscription = typeNames.Contains("Subscription");
            var totalFields = fieldsByType.Values.Sum(f => f.Count);

            // Exclude root operation types from typeNames count (they are operations, not data types)
            var dataTypeNames = typeNames
                .Where(t => t is not ("Query" or "Mutation" or "Subscription" or "Subscription"))
                .ToList();

            return new SchemaAnalysis(
                TypeNames: dataTypeNames,
                Operations: operations,
                FieldsByType: fieldsByType.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.AsReadOnly()),
                TotalFieldCount: totalFields,
                HasQueryType: hasQuery,
                HasMutationType: hasMutation,
                HasSubscriptionType: hasSubscription);
        }

        private static bool IsTypeDeclaration(string line, out string typeName)
        {
            typeName = string.Empty;
            string[] keywords = ["type ", "input ", "interface ", "enum ", "union "];
            foreach (var keyword in keywords)
            {
                if (!line.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) continue;
                var rest = line[keyword.Length..];
                typeName = rest.Split([' ', '{', '(', '@'])[0].Trim();
                return !string.IsNullOrWhiteSpace(typeName);
            }
            return false;
        }

        private sealed record SchemaAnalysis(
            List<string> TypeNames,
            List<OperationDto> Operations,
            Dictionary<string, IReadOnlyList<string>> FieldsByType,
            int TotalFieldCount,
            bool HasQueryType,
            bool HasMutationType,
            bool HasSubscriptionType);
    }

    public sealed record OperationDto(string Name, string Kind);

    public sealed record Response(
        Guid SnapshotId,
        Guid ApiAssetId,
        string ContractVersion,
        int TypeCount,
        int FieldCount,
        int OperationCount,
        bool HasQueryType,
        bool HasMutationType,
        bool HasSubscriptionType,
        DateTimeOffset CapturedAt);
}
