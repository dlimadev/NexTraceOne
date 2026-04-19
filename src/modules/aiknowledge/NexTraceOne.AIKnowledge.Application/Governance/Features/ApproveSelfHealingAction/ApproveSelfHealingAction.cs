using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ApproveSelfHealingAction;

/// <summary>
/// Feature: ApproveSelfHealingAction — aprova uma acção de auto-remediação pendente.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ApproveSelfHealingAction
{
    public sealed record Command(Guid ActionId, string ApprovedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ActionId).NotEmpty();
            RuleFor(x => x.ApprovedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        ISelfHealingActionRepository repository,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var action = await repository.GetByIdAsync(SelfHealingActionId.From(request.ActionId), ct);
            if (action is null)
                return AiGovernanceErrors.SelfHealingActionNotFound(request.ActionId.ToString());

            action.Approve(request.ApprovedBy, clock.UtcNow);
            await unitOfWork.CommitAsync(ct);

            return new Response(action.Id.Value, action.Status, action.ApprovedBy!, action.ApprovedAt!.Value);
        }
    }

    public sealed record Response(Guid ActionId, string Status, string ApprovedBy, DateTimeOffset ApprovedAt);
}
