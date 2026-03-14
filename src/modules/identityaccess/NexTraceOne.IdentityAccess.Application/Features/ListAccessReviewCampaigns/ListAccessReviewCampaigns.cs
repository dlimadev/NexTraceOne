using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.ListAccessReviewCampaigns;

/// <summary>
/// Feature: ListAccessReviewCampaigns — lista as campanhas de revisão de acessos abertas no tenant.
///
/// Retorna campanhas no estado Open do tenant autenticado.
/// Usado pelo compliance officer e pelo admin para acompanhar o estado das recertificações.
/// </summary>
public static class ListAccessReviewCampaigns
{
    /// <summary>Query de listagem de campanhas abertas.</summary>
    public sealed record Query : IQuery<IReadOnlyList<CampaignSummary>>;

    /// <summary>Resumo de uma campanha para listagem.</summary>
    public sealed record CampaignSummary(
        Guid CampaignId,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset Deadline,
        int TotalItems,
        int PendingItems,
        string Status);

    /// <summary>
    /// Handler que lista as campanhas de revisão abertas do tenant corrente.
    /// Inclui contagem de itens pendentes para facilitar o acompanhamento do progresso.
    /// </summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IAccessReviewRepository accessReviewRepository) : IQueryHandler<Query, IReadOnlyList<CampaignSummary>>
    {
        public async Task<Result<IReadOnlyList<CampaignSummary>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
            {
                return IdentityErrors.TenantContextRequired();
            }

            var tenantId = TenantId.From(currentTenant.Id);
            var campaigns = await accessReviewRepository.ListOpenByTenantAsync(tenantId, cancellationToken);

            var summaries = campaigns.Select(c => new CampaignSummary(
                c.Id.Value,
                c.Name,
                c.CreatedAt,
                c.Deadline,
                c.Items.Count,
                c.Items.Count(i => i.Decision == AccessReviewDecision.Pending),
                c.Status.ToString())).ToList();

            return Result<IReadOnlyList<CampaignSummary>>.Success(summaries);
        }
    }
}
