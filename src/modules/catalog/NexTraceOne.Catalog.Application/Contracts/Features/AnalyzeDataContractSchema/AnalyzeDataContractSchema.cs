using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using System.Text.Json;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeDataContractSchema;

/// <summary>
/// Feature: AnalyzeDataContractSchema — regista e analisa o schema de um Data Contract.
/// Parseia o JSON de schema de colunas, calcula classificação PII máxima, conta colunas
/// e persiste um snapshot versionado.
///
/// Referência: CC-03, ADR-007.
/// Ownership: módulo Catalog (Contracts).
/// </summary>
public static class AnalyzeDataContractSchema
{
    public sealed record Command(
        string TenantId,
        Guid ApiAssetId,
        string Owner,
        int SlaFreshnessHours,
        string SchemaJson,
        string SourceSystem) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Owner).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SlaFreshnessHours).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SchemaJson).NotEmpty().MaximumLength(524288);
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(
        IDataContractSchemaRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var (columnCount, piiClassification) = ParseSchema(request.SchemaJson);

            var latest = await repository.GetLatestByApiAssetAsync(
                request.ApiAssetId, request.TenantId, cancellationToken);
            var version = (latest?.Version ?? 0) + 1;

            var schema = DataContractSchema.Create(
                apiAssetId: request.ApiAssetId,
                tenantId: request.TenantId,
                owner: request.Owner,
                slaFreshnessHours: request.SlaFreshnessHours,
                schemaJson: request.SchemaJson,
                piiClassification: piiClassification,
                sourceSystem: request.SourceSystem,
                columnCount: columnCount,
                version: version,
                utcNow: clock.UtcNow);

            await repository.AddAsync(schema, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                SchemaId: schema.Id.Value,
                ApiAssetId: schema.ApiAssetId,
                Owner: schema.Owner,
                SlaFreshnessHours: schema.SlaFreshnessHours,
                ColumnCount: schema.ColumnCount,
                PiiClassification: schema.PiiClassification.ToString(),
                SourceSystem: schema.SourceSystem,
                Version: schema.Version,
                CapturedAt: schema.CapturedAt));
        }

        private static (int ColumnCount, PiiClassification MaxPii) ParseSchema(string schemaJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(schemaJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (0, PiiClassification.None);

                var columns = doc.RootElement.EnumerateArray().ToList();
                var maxPii = PiiClassification.None;

                foreach (var col in columns)
                {
                    if (col.TryGetProperty("piiClassification", out var piiProp) &&
                        Enum.TryParse<PiiClassification>(piiProp.GetString(), true, out var pii) &&
                        pii > maxPii)
                    {
                        maxPii = pii;
                    }
                }

                return (columns.Count, maxPii);
            }
            catch
            {
                return (0, PiiClassification.None);
            }
        }
    }

    public sealed record Response(
        Guid SchemaId,
        Guid ApiAssetId,
        string Owner,
        int SlaFreshnessHours,
        int ColumnCount,
        string PiiClassification,
        string SourceSystem,
        int Version,
        DateTimeOffset CapturedAt);
}
