using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteAlertRule;

/// <summary>Feature: DeleteAlertRule — remove uma regra de alerta personalizada.</summary>
public static class DeleteAlertRule
{
    public sealed record Command(Guid RuleId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RuleId).NotEmpty();
        }
    }

    public sealed class Handler(
        IUserAlertRuleRepository repository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rule = await repository.GetByIdAsync(new UserAlertRuleId(request.RuleId), cancellationToken);
            if (rule is null)
                return Error.NotFound("AlertRule.NotFound", $"Alert rule '{request.RuleId}' not found.");

            if (rule.UserId != currentUser.Id)
                return Error.Forbidden("AlertRule.Forbidden", "You do not own this alert rule.");

            await repository.DeleteAsync(rule, cancellationToken);
            return new Response(request.RuleId);
        }
    }

    public sealed record Response(Guid RuleId);
}
