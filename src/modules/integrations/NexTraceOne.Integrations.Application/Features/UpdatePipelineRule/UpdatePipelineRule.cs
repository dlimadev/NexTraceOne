using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.UpdatePipelineRule;

/// <summary>
/// Feature: UpdatePipelineRule — actualiza uma regra de pipeline existente.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class UpdatePipelineRule
{
    /// <summary>Comando para actualizar uma regra de pipeline.</summary>
    public sealed record Command(
        string TenantId,
        Guid RuleId,
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        string ConditionJson,
        string ActionJson,
        int Priority,
        bool IsEnabled,
        string? Description = null) : ICommand<Response>;

    /// <summary>Validador do comando UpdatePipelineRule.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.RuleId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.RuleType).IsInEnum();
            RuleFor(x => x.SignalType).IsInEnum();
            RuleFor(x => x.ConditionJson).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ActionJson).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Priority).GreaterThan(0).LessThanOrEqualTo(1000);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        }
    }

    /// <summary>Handler que actualiza uma regra de pipeline existente.</summary>
    public sealed class Handler(
        ITenantPipelineRuleRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var rule = await repository.GetByIdAsync(new TenantPipelineRuleId(request.RuleId), cancellationToken);
            if (rule is null)
                return Error.NotFound("PipelineRule.NotFound", $"Pipeline rule '{request.RuleId}' not found.");

            rule.Update(
                name: request.Name,
                ruleType: request.RuleType,
                signalType: request.SignalType,
                conditionJson: request.ConditionJson,
                actionJson: request.ActionJson,
                priority: request.Priority,
                isEnabled: request.IsEnabled,
                description: request.Description,
                utcNow: clock.UtcNow);

            await repository.UpdateAsync(rule, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                RuleId: rule.Id.Value,
                Name: rule.Name,
                RuleType: rule.RuleType,
                SignalType: rule.SignalType,
                Priority: rule.Priority,
                IsEnabled: rule.IsEnabled,
                UpdatedAt: rule.UpdatedAt!.Value));
        }
    }

    /// <summary>Resposta do comando UpdatePipelineRule.</summary>
    public sealed record Response(
        Guid RuleId,
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        int Priority,
        bool IsEnabled,
        DateTimeOffset UpdatedAt);
}
