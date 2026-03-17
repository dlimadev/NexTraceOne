using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateBudget;

/// <summary>
/// Feature: UpdateBudget — atualiza limites e período de um budget de IA.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdateBudget
{
    /// <summary>Comando de atualização de um budget de IA.</summary>
    public sealed record Command(
        Guid BudgetId,
        long? MaxTokens,
        int? MaxRequests,
        string? Period) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de budget.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BudgetId).NotEmpty();
            RuleFor(x => x.MaxTokens).GreaterThan(0).When(x => x.MaxTokens.HasValue);
            RuleFor(x => x.MaxRequests).GreaterThan(0).When(x => x.MaxRequests.HasValue);
            RuleFor(x => x.Period)
                .Must(p => p is null || Enum.TryParse<BudgetPeriod>(p, ignoreCase: true, out _))
                .WithMessage("Period must be one of: Daily, Weekly, Monthly.");
        }
    }

    /// <summary>Handler que atualiza limites e período de um budget de IA.</summary>
    public sealed class Handler(
        IAiBudgetRepository budgetRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var budget = await budgetRepository.GetByIdAsync(
                AIBudgetId.From(request.BudgetId),
                cancellationToken);

            if (budget is null)
            {
                return AiGovernanceErrors.BudgetNotFound(request.BudgetId.ToString());
            }

            var maxTokens = request.MaxTokens ?? budget.MaxTokens;
            var maxRequests = request.MaxRequests ?? budget.MaxRequests;
            var period = request.Period is not null
                ? Enum.Parse<BudgetPeriod>(request.Period, ignoreCase: true)
                : budget.Period;

            var result = budget.Update(maxTokens, maxRequests, period);
            if (result.IsFailure)
            {
                return result.Error;
            }

            await budgetRepository.UpdateAsync(budget, cancellationToken);

            return Unit.Value;
        }
    }
}
