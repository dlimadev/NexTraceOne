using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.ToggleAutomationRule;

/// <summary>Feature: ToggleAutomationRule — activa ou desactiva uma regra de automação.</summary>
public static class ToggleAutomationRule
{
    public sealed record Command(Guid RuleId, bool Enabled) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RuleId).NotEmpty();
        }
    }

    public sealed class Handler(
        IAutomationRuleRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rule = await repository.GetByIdAsync(
                new AutomationRuleId(request.RuleId),
                currentTenant.Id.ToString(),
                cancellationToken);

            if (rule is null)
                return Error.NotFound("AutomationRule.NotFound", $"Automation rule '{request.RuleId}' not found.");

            rule.Toggle(request.Enabled, clock.UtcNow);
            await repository.UpdateAsync(rule, cancellationToken);
            return true;
        }
    }
}
