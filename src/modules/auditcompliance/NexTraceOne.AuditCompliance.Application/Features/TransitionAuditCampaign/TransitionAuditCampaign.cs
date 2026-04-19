using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.AuditCompliance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.TransitionAuditCampaign;

/// <summary>
/// Feature: TransitionAuditCampaign — faz a transição de estado de uma campanha de auditoria.
///
/// Transições suportadas:
///   - Start:    Planned    → InProgress
///   - Complete: InProgress → Completed
///   - Cancel:   Planned    → Cancelled | InProgress → Cancelled
///
/// Persona primária: Auditor, Platform Admin.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class TransitionAuditCampaign
{
    /// <summary>Ações de transição disponíveis para uma campanha.</summary>
    public enum CampaignAction { Start, Complete, Cancel }

    /// <summary>Comando de transição de estado de campanha.</summary>
    public sealed record Command(Guid CampaignId, CampaignAction Action) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CampaignId).NotEmpty();
            RuleFor(x => x.Action).IsInEnum();
        }
    }

    /// <summary>Handler que executa a transição de estado da campanha.</summary>
    public sealed class Handler(
        IAuditCampaignRepository campaignRepository,
        IDateTimeProvider dateTimeProvider,
        IAuditComplianceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var campaignId = Domain.Entities.AuditCampaignId.From(request.CampaignId);
            var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);

            if (campaign is null)
                return AuditErrors.CampaignNotFound(request.CampaignId);

            var now = dateTimeProvider.UtcNow;

            try
            {
                switch (request.Action)
                {
                    case CampaignAction.Start:
                        campaign.Start(now);
                        break;
                    case CampaignAction.Complete:
                        campaign.Complete(now);
                        break;
                    case CampaignAction.Cancel:
                        campaign.Cancel(now);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                return Error.Conflict("Audit.Campaign.InvalidTransition", ex.Message);
            }

            campaignRepository.Update(campaign);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(campaign.Id.Value, campaign.Status, campaign.StartedAt, campaign.CompletedAt);
        }
    }

    /// <summary>Resposta da transição de estado da campanha.</summary>
    public sealed record Response(
        Guid CampaignId,
        CampaignStatus Status,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);
}
