using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContractChangelogs;

/// <summary>
/// Feature: ListContractChangelogs — lista entradas de changelog de contrato
/// filtradas por ativo de API ou pendentes de aprovação formal.
/// Estrutura VSA: Query + Handler + Response em arquivo único.
/// </summary>
public static class ListContractChangelogs
{
    /// <summary>Query de listagem de changelogs de contrato com filtros opcionais.</summary>
    public sealed record Query(
        string? ApiAssetId,
        bool PendingApprovalOnly) : IQuery<Response>;

    /// <summary>
    /// Handler que lista changelogs de contrato por ativo de API ou pendentes de aprovação.
    /// Prioriza filtro por aprovação pendente quando solicitado.
    /// </summary>
    public sealed class Handler(IContractChangelogRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (request.PendingApprovalOnly)
            {
                var pending = await repository.ListPendingApprovalAsync(cancellationToken);

                var items = pending
                    .Where(c => string.IsNullOrWhiteSpace(request.ApiAssetId) || c.ApiAssetId == request.ApiAssetId)
                    .Select(MapToSummary)
                    .ToList();

                return new Response(items, items.Count);
            }

            if (!string.IsNullOrWhiteSpace(request.ApiAssetId))
            {
                var changelogs = await repository.ListByApiAssetAsync(
                    request.ApiAssetId, cancellationToken);

                var items = changelogs.Select(MapToSummary).ToList();

                return new Response(items, items.Count);
            }

            return new Response([], 0);
        }

        private static ChangelogSummary MapToSummary(Domain.Contracts.Entities.ContractChangelog c)
            => new(
                c.Id.Value,
                c.ApiAssetId,
                c.ServiceName,
                c.FromVersion,
                c.ToVersion,
                c.Source.ToString(),
                c.Summary,
                c.IsApproved,
                c.CreatedAt);
    }

    /// <summary>Resumo de uma entrada de changelog para listagem.</summary>
    public sealed record ChangelogSummary(
        Guid ChangelogId,
        string ApiAssetId,
        string ServiceName,
        string? FromVersion,
        string ToVersion,
        string Source,
        string Summary,
        bool IsApproved,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de changelogs de contrato.</summary>
    public sealed record Response(
        IReadOnlyList<ChangelogSummary> Items,
        int TotalCount);
}
