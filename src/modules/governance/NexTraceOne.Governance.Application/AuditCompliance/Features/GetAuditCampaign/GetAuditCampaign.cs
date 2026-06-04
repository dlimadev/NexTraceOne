using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.Governance.Application.AuditCompliance.Abstractions;
using NexTraceOne.Governance.Domain.AuditCompliance.Entities;
using NexTraceOne.Governance.Domain.AuditCompliance.Enums;
using NexTraceOne.Governance.Domain.AuditCompliance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.AuditCompliance.Features.GetAuditCampaign;

/// <summary>
/// Feature: GetAuditCampaign — obtém uma campanha de auditoria pelo identificador.
/// </summary>
public static class GetAuditCampaign
{
    /// <summary>Query de obtenção de campanha de auditoria.</summary>
    public sealed record Query(Guid CampaignId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CampaignId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém a campanha pelo Id.</summary>
    public sealed class Handler(IAuditCampaignRepository auditCampaignRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var campaign = await auditCampaignRepository.GetByIdAsync(
                AuditCampaignId.From(request.CampaignId), cancellationToken);

            if (campaign is null)
                return AuditErrors.CampaignNotFound(request.CampaignId);

            return new Response(
                campaign.Id.Value,
                campaign.Name,
                campaign.Description,
                campaign.CampaignType,
                campaign.Status,
                campaign.ScheduledStartAt,
                campaign.StartedAt,
                campaign.CompletedAt,
                campaign.CreatedBy,
                campaign.CreatedAt);
        }
    }

    /// <summary>Resposta da obtenção de campanha de auditoria.</summary>
    public sealed record Response(
        Guid CampaignId,
        string Name,
        string? Description,
        string CampaignType,
        CampaignStatus Status,
        DateTimeOffset? ScheduledStartAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string CreatedBy,
        DateTimeOffset CreatedAt);
}
