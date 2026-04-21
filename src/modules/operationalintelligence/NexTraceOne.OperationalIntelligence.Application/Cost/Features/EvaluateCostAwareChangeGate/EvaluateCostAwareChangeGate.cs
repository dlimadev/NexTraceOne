using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.EvaluateCostAwareChangeGate;

/// <summary>
/// Feature: EvaluateCostAwareChangeGate — gate de promoção orientado a custo.
/// Avalia se o custo corrente do serviço no ambiente alvo permite uma nova mudança.
/// Integra-se com Change Governance para bloquear promoções em serviços com custo crítico.
/// Owner: OI Cost. Pilar: FinOps contextual + Change Confidence.
/// </summary>
public static class EvaluateCostAwareChangeGate
{
    public sealed record Query(
        string ServiceName,
        string TargetEnvironment,
        decimal? ExpectedCostImpact = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(
        IServiceCostProfileRepository profileRepo,
        IWasteSignalRepository wasteRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var profile = await profileRepo.GetByServiceAndEnvironmentAsync(request.ServiceName, request.TargetEnvironment, cancellationToken);
            var pendingWasteSignals = await wasteRepo.ListByServiceAsync(request.ServiceName, request.TargetEnvironment, cancellationToken);
            var activeWaste = pendingWasteSignals.Where(w => !w.IsAcknowledged).ToList();

            GateDecision decision;
            string reason;
            decimal? budgetUtilizationPct = null;

            if (profile is null)
            {
                decision = GateDecision.Unknown;
                reason = $"No cost profile found for '{request.ServiceName}' in '{request.TargetEnvironment}'.";
            }
            else
            {
                budgetUtilizationPct = (decimal?)profile.BudgetUsagePercent;

                var projectedCost = profile.CurrentMonthCost + (request.ExpectedCostImpact ?? 0m);
                decimal? projectedUtilization = profile.MonthlyBudget.HasValue && profile.MonthlyBudget.Value > 0
                    ? projectedCost / profile.MonthlyBudget.Value * 100m
                    : null;

                if (profile.IsOverBudget)
                {
                    decision = GateDecision.Blocked;
                    reason = $"Service '{request.ServiceName}' is over budget in '{request.TargetEnvironment}' ({budgetUtilizationPct:N1}% utilization).";
                }
                else if (activeWaste.Count >= 3)
                {
                    decision = GateDecision.Review;
                    reason = $"Service '{request.ServiceName}' has {activeWaste.Count} unacknowledged waste signals — review before promoting.";
                }
                else if (projectedUtilization.HasValue && projectedUtilization.Value > 90m)
                {
                    decision = GateDecision.Review;
                    reason = $"Projected budget utilization after change: {projectedUtilization.Value:N1}%. Recommend review.";
                }
                else
                {
                    decision = GateDecision.Approved;
                    reason = $"Cost gate passed. Current utilization: {budgetUtilizationPct:N1}%.";
                }
            }

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                TargetEnvironment: request.TargetEnvironment,
                Decision: decision.ToString(),
                Reason: reason,
                BudgetUtilizationPercent: budgetUtilizationPct,
                ActiveWasteSignals: activeWaste.Count,
                ExpectedCostImpact: request.ExpectedCostImpact,
                EvaluatedAt: DateTimeOffset.UtcNow));
        }
    }

    public enum GateDecision { Unknown, Approved, Review, Blocked }

    public sealed record Response(
        string ServiceName,
        string TargetEnvironment,
        string Decision,
        string Reason,
        decimal? BudgetUtilizationPercent,
        int ActiveWasteSignals,
        decimal? ExpectedCostImpact,
        DateTimeOffset EvaluatedAt);
}
