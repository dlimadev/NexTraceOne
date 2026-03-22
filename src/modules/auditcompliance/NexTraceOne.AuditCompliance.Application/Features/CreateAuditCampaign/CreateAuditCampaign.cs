using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.CreateAuditCampaign;

/// <summary>
/// Feature: CreateAuditCampaign — cria uma nova campanha de auditoria.
/// </summary>
public static class CreateAuditCampaign
{
    /// <summary>Comando de criação de campanha de auditoria.</summary>
    public sealed record Command(
        string Name,
        string? Description,
        string CampaignType,
        DateTimeOffset? ScheduledStartAt,
        Guid TenantId,
        string CreatedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CampaignType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que cria a campanha e persiste no repositório.</summary>
    public sealed class Handler(
        IAuditCampaignRepository auditCampaignRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var campaign = AuditCampaign.Create(
                request.Name,
                request.Description,
                request.CampaignType,
                request.ScheduledStartAt,
                request.TenantId,
                request.CreatedBy,
                now);

            auditCampaignRepository.Add(campaign);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(campaign.Id.Value, campaign.Name, campaign.Status.ToString());
        }
    }

    /// <summary>Resposta da criação de campanha de auditoria.</summary>
    public sealed record Response(Guid CampaignId, string Name, string Status);
}
