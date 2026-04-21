using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeProtobufSchema;

/// <summary>
/// Feature: AnalyzeProtobufSchema — analisa um schema Protobuf (.proto) e persiste um snapshot estruturado.
///
/// Parsing leve sem dependências externas: contagem de messages, fields, services e RPCs
/// por análise de keywords no ficheiro .proto. Adequado para schemas típicos de produção até 256 KB.
///
/// O snapshot persistido permite diff semântico futuro e detecção de breaking changes
/// sem re-parsing do schema completo.
///
/// Wave H.1 — Protobuf Schema Analysis (GAP-CTR-02).
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class AnalyzeProtobufSchema
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
            RuleFor(x => x.SchemaContent).NotEmpty().MaximumLength(262_144); // 256 KB
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        IProtobufSchemaSnapshotRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var analysis = ParseSchema(request.SchemaContent);

            var snapshot = ProtobufSchemaSnapshot.Create(
                apiAssetId: request.ApiAssetId,
                contractVersion: request.ContractVersion,
                schemaContent: request.SchemaContent,
                messageCount: analysis.MessageNames.Count,
                fieldCount: analysis.TotalFieldCount,
                serviceCount: analysis.RpcsByService.Count,
                rpcCount: analysis.RpcsByService.Values.Sum(r => r.Count),
                messageNamesJson: JsonSerializer.Serialize(analysis.MessageNames),
                fieldsByMessageJson: JsonSerializer.Serialize(analysis.FieldsByMessage),
                rpcsByServiceJson: JsonSerializer.Serialize(analysis.RpcsByService),
                syntax: analysis.Syntax,
                tenantId: request.TenantId,
                capturedAt: clock.UtcNow);

            repository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                SnapshotId: snapshot.Id.Value,
                ApiAssetId: snapshot.ApiAssetId,
                ContractVersion: snapshot.ContractVersion,
                MessageCount: snapshot.MessageCount,
                FieldCount: snapshot.FieldCount,
                ServiceCount: snapshot.ServiceCount,
                RpcCount: snapshot.RpcCount,
                Syntax: snapshot.Syntax,
                CapturedAt: snapshot.CapturedAt);
        }

        /// <summary>
        /// Analisa o schema Protobuf por keywords.
        /// Parsing leve e sem dependências externas: conta messages, fields, services e RPCs linha a linha.
        /// Suficiente para governança e diff semântico; não substitui um parser .proto completo.
        /// </summary>
        internal static SchemaAnalysis ParseSchema(string schema)
        {
            var lines = schema.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var messageNames = new List<string>();
            var fieldsByMessage = new Dictionary<string, List<string>>();
            var rpcsByService = new Dictionary<string, List<string>>();
            string? currentMessage = null;
            string? currentService = null;
            var syntax = "proto3";

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("//")) continue;

                // syntax declaration
                if (line.StartsWith("syntax", StringComparison.OrdinalIgnoreCase) && line.Contains('"'))
                {
                    syntax = line.Contains("proto2") ? "proto2" : "proto3";
                    continue;
                }

                // message declaration
                if (line.StartsWith("message ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = ExtractName(line, "message ");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        currentMessage = name;
                        currentService = null;
                        messageNames.Add(name);
                        fieldsByMessage[name] = [];
                    }
                    continue;
                }

                // service declaration
                if (line.StartsWith("service ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = ExtractName(line, "service ");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        currentService = name;
                        currentMessage = null;
                        rpcsByService[name] = [];
                    }
                    continue;
                }

                // rpc inside service
                if (currentService is not null && line.StartsWith("rpc ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = ExtractName(line, "rpc ");
                    if (!string.IsNullOrWhiteSpace(name))
                        rpcsByService[currentService].Add(name);
                    continue;
                }

                // field inside message — lines with '=' (field number assignment in proto)
                if (currentMessage is not null && line.Contains('=') && !line.StartsWith('{') && !line.StartsWith('}') && !line.StartsWith("//"))
                {
                    // Field line format: `  string name = 1;`
                    // Split on '=' to get the left part, then extract the last token
                    var leftPart = line.Split('=')[0].Trim();
                    var parts = leftPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var fieldName = parts[^1].Trim();
                        if (!string.IsNullOrWhiteSpace(fieldName) && !fieldName.StartsWith("//"))
                            fieldsByMessage[currentMessage].Add(fieldName);
                    }
                }

                if (line.StartsWith('}'))
                {
                    currentMessage = null;
                    currentService = null;
                }
            }

            var totalFields = fieldsByMessage.Values.Sum(f => f.Count);

            return new SchemaAnalysis(
                MessageNames: messageNames,
                FieldsByMessage: fieldsByMessage.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.AsReadOnly()),
                RpcsByService: rpcsByService.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.AsReadOnly()),
                TotalFieldCount: totalFields,
                Syntax: syntax);
        }

        private static string ExtractName(string line, string keyword)
        {
            var rest = line[keyword.Length..];
            return rest.Split([' ', '{', '(', '<'])[0].Trim();
        }

        internal sealed record SchemaAnalysis(
            List<string> MessageNames,
            Dictionary<string, IReadOnlyList<string>> FieldsByMessage,
            Dictionary<string, IReadOnlyList<string>> RpcsByService,
            int TotalFieldCount,
            string Syntax);
    }

    public sealed record Response(
        Guid SnapshotId,
        Guid ApiAssetId,
        string ContractVersion,
        int MessageCount,
        int FieldCount,
        int ServiceCount,
        int RpcCount,
        string Syntax,
        DateTimeOffset CapturedAt);
}
