using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ValidateConsumerDrivenContract;

/// <summary>
/// Feature: ValidateConsumerDrivenContract — valida que o contrato publicado por
/// um provider ainda satisfaz as expectativas definidas pelos consumidores (CDCT).
/// Cada consumidor regista paths, métodos e campos esperados, e esta feature
/// verifica a especificação do provider contra essas expectativas.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ValidateConsumerDrivenContract
{
    /// <summary>Command para validação de contrato orientado pelo consumidor.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        IReadOnlyList<ConsumerExpectation> Expectations) : ICommand<Response>;

    /// <summary>Expectativa de um consumidor sobre o contrato do provider.</summary>
    public sealed record ConsumerExpectation(
        string ConsumerName,
        string ExpectedPath,
        string ExpectedMethod,
        IReadOnlyList<string> RequiredResponseFields);

    /// <summary>Valida a entrada do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Expectations).NotNull().NotEmpty();
            RuleForEach(x => x.Expectations).ChildRules(expectation =>
            {
                expectation.RuleFor(e => e.ConsumerName).NotEmpty();
                expectation.RuleFor(e => e.ExpectedPath).NotEmpty();
                expectation.RuleFor(e => e.ExpectedMethod).NotEmpty();
                expectation.RuleFor(e => e.RequiredResponseFields).NotNull();
            });
        }
    }

    /// <summary>
    /// Handler que obtém o contrato mais recente publicado pelo ApiAsset e valida
    /// cada expectativa de consumidor contra a especificação JSON. Verifica existência
    /// de paths, métodos e campos de resposta obrigatórios.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAsset = await apiAssetRepository.GetByIdAsync(
                ApiAssetId.From(request.ApiAssetId), cancellationToken);

            if (apiAsset is null)
                return CatalogGraphErrors.ApiAssetNotFound(request.ApiAssetId);

            var latestContract = await contractVersionRepository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            if (latestContract is null)
                return Error.NotFound(
                    "Contracts.ConsumerDriven.NoContract",
                    "No published contract found for API asset '{0}'.",
                    request.ApiAssetId);

            JsonElement specRoot;
            try
            {
                using var doc = JsonDocument.Parse(latestContract.SpecContent);
                specRoot = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return Error.Validation(
                    "Contracts.ConsumerDriven.InvalidSpec",
                    "The contract spec content is not valid JSON.");
            }

            var results = new List<ConsumerValidationResult>();
            var totalPassed = 0;
            var totalFailed = 0;

            foreach (var expectation in request.Expectations)
            {
                var pathExists = CheckPathExists(specRoot, expectation.ExpectedPath);
                var methodExists = pathExists && CheckMethodExists(specRoot, expectation.ExpectedPath, expectation.ExpectedMethod);
                var missingFields = methodExists
                    ? FindMissingResponseFields(specRoot, expectation.ExpectedPath, expectation.ExpectedMethod, expectation.RequiredResponseFields)
                    : expectation.RequiredResponseFields.ToList();

                var isCompatible = pathExists && methodExists && missingFields.Count == 0;

                if (isCompatible)
                    totalPassed++;
                else
                    totalFailed++;

                results.Add(new ConsumerValidationResult(
                    ConsumerName: expectation.ConsumerName,
                    ExpectedPath: expectation.ExpectedPath,
                    ExpectedMethod: expectation.ExpectedMethod,
                    PathExists: pathExists,
                    MethodExists: methodExists,
                    MissingFields: missingFields.AsReadOnly(),
                    IsCompatible: isCompatible));
            }

            return new Response(
                ApiAssetId: request.ApiAssetId,
                ApiName: apiAsset.Name,
                TotalExpectations: request.Expectations.Count,
                Passed: totalPassed,
                Failed: totalFailed,
                Results: results.AsReadOnly());
        }

        /// <summary>Verifica se o path existe na especificação OpenAPI.</summary>
        private static bool CheckPathExists(JsonElement root, string path)
        {
            if (!root.TryGetProperty("paths", out var paths))
                return false;

            return paths.TryGetProperty(path, out _);
        }

        /// <summary>Verifica se o método HTTP existe para o path na especificação.</summary>
        private static bool CheckMethodExists(JsonElement root, string path, string method)
        {
            if (!root.TryGetProperty("paths", out var paths))
                return false;

            if (!paths.TryGetProperty(path, out var pathItem))
                return false;

            var normalizedMethod = method.Trim().ToLowerInvariant();
            return pathItem.TryGetProperty(normalizedMethod, out _);
        }

        /// <summary>
        /// Identifica campos obrigatórios em falta no schema de resposta.
        /// Procura no schema de resposta 200/201 os campos esperados pelo consumidor.
        /// </summary>
        private static List<string> FindMissingResponseFields(
            JsonElement root, string path, string method, IReadOnlyList<string> requiredFields)
        {
            if (requiredFields.Count == 0)
                return [];

            var normalizedMethod = method.Trim().ToLowerInvariant();
            var missing = new List<string>();

            if (!root.TryGetProperty("paths", out var paths)
                || !paths.TryGetProperty(path, out var pathItem)
                || !pathItem.TryGetProperty(normalizedMethod, out var operation)
                || !operation.TryGetProperty("responses", out var responses))
            {
                return requiredFields.ToList();
            }

            // Procurar no schema de resposta 200 ou 201
            JsonElement? schemaElement = null;
            foreach (var statusCode in new[] { "200", "201" })
            {
                if (responses.TryGetProperty(statusCode, out var response)
                    && response.TryGetProperty("content", out var content)
                    && content.TryGetProperty("application/json", out var jsonContent)
                    && jsonContent.TryGetProperty("schema", out var schema))
                {
                    schemaElement = schema;
                    break;
                }
            }

            if (schemaElement is null)
                return requiredFields.ToList();

            // Resolver $ref se existir
            var resolvedSchema = ResolveSchemaRef(root, schemaElement.Value);

            if (!resolvedSchema.TryGetProperty("properties", out var properties))
                return requiredFields.ToList();

            foreach (var field in requiredFields)
            {
                if (!properties.TryGetProperty(field, out _))
                    missing.Add(field);
            }

            return missing;
        }

        /// <summary>Resolve referências $ref no schema, retornando o schema real.</summary>
        private static JsonElement ResolveSchemaRef(JsonElement root, JsonElement schema)
        {
            if (!schema.TryGetProperty("$ref", out var refProp))
                return schema;

            var refPath = refProp.GetString();
            if (string.IsNullOrEmpty(refPath) || !refPath.StartsWith("#/"))
                return schema;

            var segments = refPath[2..].Split('/');
            var current = root;

            foreach (var segment in segments)
            {
                if (!current.TryGetProperty(segment, out var next))
                    return schema;
                current = next;
            }

            return current;
        }
    }

    /// <summary>Resultado de validação para uma expectativa de consumidor.</summary>
    public sealed record ConsumerValidationResult(
        string ConsumerName,
        string ExpectedPath,
        string ExpectedMethod,
        bool PathExists,
        bool MethodExists,
        IReadOnlyList<string> MissingFields,
        bool IsCompatible);

    /// <summary>Resposta da validação de contrato orientado pelo consumidor.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string ApiName,
        int TotalExpectations,
        int Passed,
        int Failed,
        IReadOnlyList<ConsumerValidationResult> Results);
}
