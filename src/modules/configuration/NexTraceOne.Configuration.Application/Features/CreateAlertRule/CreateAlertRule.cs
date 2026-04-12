using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.IntegrationEvents;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateAlertRule;

/// <summary>Feature: CreateAlertRule — cria uma nova regra de alerta personalizada.</summary>
public static class CreateAlertRule
{
    private static readonly string[] ValidChannels = ["in-app", "email", "webhook"];

    public sealed record Command(string Name, string Condition, string Channel) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Condition).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Channel).NotEmpty()
                .Must(c => ValidChannels.Contains(c, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Channel must be one of: {string.Join(", ", ValidChannels)}");
        }
    }

    public sealed class Handler(
        IUserAlertRuleRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock,
        IEventBus eventBus) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rule = UserAlertRule.Create(currentUser.Id, currentTenant.Id.ToString(), request.Name, request.Condition, request.Channel, clock.UtcNow);
            await repository.AddAsync(rule, cancellationToken);

            await eventBus.PublishAsync(
                new ConfigurationIntegrationEvents.ConfigurationValueChanged(
                    Key: $"alert-rule:{rule.Name}",
                    Scope: "user",
                    ScopeReferenceId: rule.Id.Value,
                    PreviousValue: null,
                    NewValue: $"channel={rule.Channel},condition={rule.Condition}",
                    ChangedBy: currentUser.Id),
                cancellationToken);

            return new Response(rule.Id.Value, rule.Name, rule.Channel, rule.IsEnabled, rule.CreatedAt);
        }
    }

    public sealed record Response(Guid RuleId, string Name, string Channel, bool IsEnabled, DateTimeOffset CreatedAt);
}
