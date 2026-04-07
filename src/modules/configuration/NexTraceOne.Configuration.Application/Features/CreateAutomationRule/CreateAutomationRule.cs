using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateAutomationRule;

/// <summary>Feature: CreateAutomationRule — cria uma regra de automação If-Then para o tenant.</summary>
public static class CreateAutomationRule
{
    private static readonly string[] ValidTriggers =
        ["on_change_created", "on_incident_opened", "on_contract_published", "on_approval_expired"];

    public sealed record Command(
        string Name,
        string Trigger,
        string ConditionsJson,
        string ActionsJson) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Trigger).NotEmpty()
                .Must(t => ValidTriggers.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Trigger must be one of: {string.Join(", ", ValidTriggers)}");
            RuleFor(x => x.ConditionsJson).NotNull();
            RuleFor(x => x.ActionsJson).NotNull();
        }
    }

    public sealed class Handler(
        IAutomationRuleRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rule = AutomationRule.Create(
                currentTenant.Id.ToString(),
                request.Name,
                request.Trigger,
                request.ConditionsJson,
                request.ActionsJson,
                currentUser.Id,
                clock.UtcNow);

            await repository.AddAsync(rule, cancellationToken);

            return new Response(
                rule.Id.Value,
                rule.Name,
                rule.Trigger,
                rule.ConditionsJson,
                rule.ActionsJson,
                rule.IsEnabled,
                rule.CreatedAt);
        }
    }

    public sealed record Response(
        Guid RuleId,
        string Name,
        string Trigger,
        string ConditionsJson,
        string ActionsJson,
        bool IsEnabled,
        DateTimeOffset CreatedAt);
}
