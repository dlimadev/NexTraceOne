using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.CreatePipelineRule;

/// <summary>
/// Feature: CreatePipelineRule — cria uma nova regra de pipeline por tenant.
/// As regras são aplicadas pelo TenantPipelineEngine a cada sinal de telemetria ingerido.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class CreatePipelineRule
{
    /// <summary>Comando para criar uma nova regra de pipeline.</summary>
    public sealed record Command(
        string TenantId,
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        string ConditionJson,
        string ActionJson,
        int Priority,
        bool IsEnabled = true,
        string? Description = null) : ICommand<Response>;

    /// <summary>Validador do comando CreatePipelineRule.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.RuleType).IsInEnum();
            RuleFor(x => x.SignalType).IsInEnum();
            RuleFor(x => x.ConditionJson).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ActionJson).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Priority).GreaterThan(0).LessThanOrEqualTo(1000);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        }
    }

    /// <summary>Handler que cria e persiste uma nova regra de pipeline.</summary>
    public sealed class Handler(
        ITenantPipelineRuleRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var rule = TenantPipelineRule.Create(
                tenantId: request.TenantId,
                name: request.Name,
                ruleType: request.RuleType,
                signalType: request.SignalType,
                conditionJson: request.ConditionJson,
                actionJson: request.ActionJson,
                priority: request.Priority,
                isEnabled: request.IsEnabled,
                description: request.Description,
                utcNow: clock.UtcNow);

            await repository.AddAsync(rule, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                RuleId: rule.Id.Value,
                Name: rule.Name,
                RuleType: rule.RuleType,
                SignalType: rule.SignalType,
                Priority: rule.Priority,
                IsEnabled: rule.IsEnabled,
                CreatedAt: rule.CreatedAt));
        }
    }

    /// <summary>Resposta do comando CreatePipelineRule.</summary>
    public sealed record Response(
        Guid RuleId,
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        int Priority,
        bool IsEnabled,
        DateTimeOffset CreatedAt);
}
