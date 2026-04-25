using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDataContractSchemaHistory;

/// <summary>
/// Feature: GetDataContractSchemaHistory — lista o histórico de schemas de um Data Contract
/// por ApiAsset, do mais recente para o mais antigo.
///
/// Referência: CC-03.
/// Ownership: módulo Catalog (Contracts).
/// </summary>
public static class GetDataContractSchemaHistory
{
    public sealed record Query(
        string TenantId,
        Guid ApiAssetId,
        int Page = 1,
        int PageSize = 10) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    public sealed class Handler(IDataContractSchemaRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (schemas, totalCount) = await repository.ListByApiAssetAsync(
                request.ApiAssetId, request.TenantId, request.Page, request.PageSize, cancellationToken);

            var items = schemas.Select(s => new DataContractSchemaDto(
                SchemaId: s.Id.Value,
                Owner: s.Owner,
                SlaFreshnessHours: s.SlaFreshnessHours,
                ColumnCount: s.ColumnCount,
                PiiClassification: s.PiiClassification.ToString(),
                SourceSystem: s.SourceSystem,
                Version: s.Version,
                CapturedAt: s.CapturedAt)).ToList();

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                Items: items,
                TotalCount: totalCount));
        }
    }

    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<DataContractSchemaDto> Items,
        int TotalCount);

    public sealed record DataContractSchemaDto(
        Guid SchemaId,
        string Owner,
        int SlaFreshnessHours,
        int ColumnCount,
        string PiiClassification,
        string SourceSystem,
        int Version,
        DateTimeOffset CapturedAt);
}
