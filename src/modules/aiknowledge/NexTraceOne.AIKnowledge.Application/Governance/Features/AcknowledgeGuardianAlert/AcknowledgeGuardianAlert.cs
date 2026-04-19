using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.AcknowledgeGuardianAlert;

/// <summary>
/// Feature: AcknowledgeGuardianAlert — marca um alerta do Guardian como reconhecido.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class AcknowledgeGuardianAlert
{
    public sealed record Command(Guid AlertId, string AcknowledgedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AlertId).NotEmpty();
            RuleFor(x => x.AcknowledgedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGuardianAlertRepository alertRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var alert = await alertRepository.GetByIdAsync(GuardianAlertId.From(request.AlertId), ct);

            if (alert is null)
                return AiGovernanceErrors.GuardianAlertNotFound(request.AlertId.ToString());

            alert.Acknowledge(request.AcknowledgedBy, DateTimeOffset.UtcNow);
            await unitOfWork.CommitAsync(ct);

            return new Response(alert.Id.Value, alert.Status, alert.AcknowledgedBy!, alert.AcknowledgedAt);
        }
    }

    public sealed record Response(Guid AlertId, string Status, string AcknowledgedBy, DateTimeOffset? AcknowledgedAt);
}
