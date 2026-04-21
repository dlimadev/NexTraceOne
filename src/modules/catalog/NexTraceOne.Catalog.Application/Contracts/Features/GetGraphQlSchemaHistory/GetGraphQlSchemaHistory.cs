using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetGraphQlSchemaHistory;

/// <summary>
/// Feature: GetGraphQlSchemaHistory — lista o histórico de snapshots de schema GraphQL
/// para um dado ApiAsset, do mais recente para o mais antigo.
///
/// Permite ao utilizador navegar pela evolução temporal do schema GraphQL de um contrato,
/// seleccionar dois snapshots para comparar (diff) e auditar mudanças ao longo do tempo.
///
/// Wave G.3 — GraphQL Schema Analysis (GAP-CTR-01).
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetGraphQlSchemaHistory
{
    public sealed record Query(
        Guid ApiAssetId,
        Guid TenantId,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(
        IGraphQlSchemaSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshots = await repository.ListByApiAssetAsync(
                request.ApiAssetId,
                request.TenantId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = snapshots.Select(s => new SnapshotSummaryDto(
                SnapshotId: s.Id.Value,
                ContractVersion: s.ContractVersion,
                TypeCount: s.TypeCount,
                FieldCount: s.FieldCount,
                OperationCount: s.OperationCount,
                HasQueryType: s.HasQueryType,
                HasMutationType: s.HasMutationType,
                HasSubscriptionType: s.HasSubscriptionType,
                CapturedAt: s.CapturedAt)).ToList();

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                Page: request.Page,
                PageSize: request.PageSize,
                Count: items.Count,
                Snapshots: items));
        }
    }

    public sealed record SnapshotSummaryDto(
        Guid SnapshotId,
        string ContractVersion,
        int TypeCount,
        int FieldCount,
        int OperationCount,
        bool HasQueryType,
        bool HasMutationType,
        bool HasSubscriptionType,
        DateTimeOffset CapturedAt);

    public sealed record Response(
        Guid ApiAssetId,
        int Page,
        int PageSize,
        int Count,
        IReadOnlyList<SnapshotSummaryDto> Snapshots);
}
