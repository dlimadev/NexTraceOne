using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ProposeSelfHealingAction;

/// <summary>
/// Feature: ProposeSelfHealingAction — propõe uma acção de auto-remediação para um incidente.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ProposeSelfHealingAction
{
    private static readonly string[] ValidActionTypes = ["automatic", "one_click", "suggestion"];

    public sealed record Command(
        string IncidentId,
        string ServiceName,
        string ActionType,
        string ActionDescription,
        double Confidence,
        string RiskLevel,
        Guid TenantId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.ActionType).NotEmpty().Must(t => ValidActionTypes.Contains(t))
                .WithMessage("ActionType must be 'automatic', 'one_click' or 'suggestion'.");
            RuleFor(x => x.ActionDescription).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Confidence).InclusiveBetween(0.0, 1.0);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISelfHealingActionRepository repository,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var action = SelfHealingAction.Propose(
                request.IncidentId,
                request.ServiceName,
                request.ActionType,
                request.ActionDescription,
                request.Confidence,
                request.RiskLevel,
                request.TenantId,
                clock.UtcNow);

            repository.Add(action);
            await unitOfWork.CommitAsync(ct);

            return new Response(
                action.Id.Value,
                action.ActionType,
                action.ActionDescription,
                action.Confidence,
                action.RiskLevel,
                action.Status);
        }
    }

    public sealed record Response(
        Guid ActionId,
        string ActionType,
        string ActionDescription,
        double Confidence,
        string RiskLevel,
        string Status);
}
