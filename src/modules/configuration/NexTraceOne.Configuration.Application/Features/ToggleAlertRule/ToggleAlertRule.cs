using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.ToggleAlertRule;

/// <summary>Feature: ToggleAlertRule — activa ou desactiva uma regra de alerta.</summary>
public static class ToggleAlertRule
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
        IUserAlertRuleRepository repository,
        ICurrentUser currentUser,
        IDateTimeProvider clock) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rule = await repository.GetByIdAsync(new UserAlertRuleId(request.RuleId), cancellationToken);
            if (rule is null)
                return Error.NotFound("AlertRule.NotFound", $"Alert rule '{request.RuleId}' not found.");

            if (rule.UserId != currentUser.Id)
                return Error.Forbidden("AlertRule.Forbidden", "You do not own this alert rule.");

            rule.Toggle(request.Enabled, clock.UtcNow);
            await repository.UpdateAsync(rule, cancellationToken);
            return true;
        }
    }
}
