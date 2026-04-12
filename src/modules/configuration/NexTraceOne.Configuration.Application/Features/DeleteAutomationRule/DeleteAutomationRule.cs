using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteAutomationRule;

/// <summary>Feature: DeleteAutomationRule — remove uma regra de automação.</summary>
public static class DeleteAutomationRule
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
        IAutomationRuleRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
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

            await repository.DeleteAsync(new AutomationRuleId(request.RuleId), cancellationToken);
            return new Response(request.RuleId);
        }
    }

    public sealed record Response(Guid RuleId);
}
