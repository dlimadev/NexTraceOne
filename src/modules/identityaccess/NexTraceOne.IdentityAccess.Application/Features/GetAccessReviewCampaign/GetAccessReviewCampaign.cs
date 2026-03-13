using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.GetAccessReviewCampaign;

/// <summary>
/// Feature: GetAccessReviewCampaign — obtém o detalhe completo de uma campanha de revisão de acessos.
///
/// Retorna a campanha com todos os seus itens, incluindo decisões já tomadas e itens pendentes.
/// Permite ao reviewer ver o estado completo da campanha antes de tomar decisões.
/// </summary>
public static class GetAccessReviewCampaign
{
    /// <summary>Query de detalhe de campanha por identificador.</summary>
    public sealed record Query(Guid CampaignId) : IQuery<CampaignDetail>;

    /// <summary>Detalhe completo da campanha com seus itens.</summary>
    public sealed record CampaignDetail(
        Guid CampaignId,
        string Name,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset Deadline,
        DateTimeOffset? CompletedAt,
        Guid? InitiatedBy,
        IReadOnlyList<ReviewItemDetail> Items);

    /// <summary>Detalhe de um item individual de revisão.</summary>
    public sealed record ReviewItemDetail(
        Guid ItemId,
        Guid UserId,
        Guid RoleId,
        string RoleName,
        Guid ReviewerId,
        string Decision,
        string? ReviewerComment,
        DateTimeOffset? DecidedAt);

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CampaignId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que busca a campanha completa com todos os itens de revisão.
    /// Valida que a campanha pertence ao tenant do caller.
    /// </summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IAccessReviewRepository accessReviewRepository) : IQueryHandler<Query, CampaignDetail>
    {
        public async Task<Result<CampaignDetail>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var campaign = await accessReviewRepository.GetByIdWithItemsAsync(
                AccessReviewCampaignId.From(request.CampaignId),
                cancellationToken);

            if (campaign is null)
            {
                return IdentityErrors.AccessReviewCampaignNotFound(request.CampaignId);
            }

            // Garante isolamento de tenant — campanha deve pertencer ao tenant do caller
            if (campaign.TenantId.Value != currentTenant.Id)
            {
                return IdentityErrors.AccessReviewCampaignNotFound(request.CampaignId);
            }

            var items = campaign.Items.Select(i => new ReviewItemDetail(
                i.Id.Value,
                i.UserId.Value,
                i.RoleId.Value,
                i.RoleName,
                i.ReviewerId.Value,
                i.Decision.ToString(),
                i.ReviewerComment,
                i.DecidedAt)).ToList();

            return new CampaignDetail(
                campaign.Id.Value,
                campaign.Name,
                campaign.Status.ToString(),
                campaign.CreatedAt,
                campaign.Deadline,
                campaign.CompletedAt,
                campaign.InitiatedBy?.Value,
                items);
        }
    }
}
