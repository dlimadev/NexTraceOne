using Ardalis.GuardClauses;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.EvaluateFinOpsBudgetGate;

/// <summary>
/// Feature: EvaluateFinOpsBudgetGate — avalia se um serviço/equipa está dentro do orçamento FinOps.
/// Consulta:
///   - finops.budget_alert_threshold: threshold de alerta
///   - finops.chargeback.enabled: se chargeback está ativo
///   - finops.budget.by_service: budget por serviço
/// Pilar: FinOps Contextual
/// </summary>
public static class EvaluateFinOpsBudgetGate
{
    /// <summary>Query para avaliar o gate de FinOps.</summary>
    public sealed record Query(
        string ServiceName,
        string TeamName,
        decimal CurrentSpendPct) : IQuery<Response>;

    /// <summary>Handler que avalia o gate de FinOps.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Get alert threshold
            var thresholdConfig = await configService.ResolveEffectiveValueAsync(
                "finops.budget_alert_threshold",
                ConfigurationScope.Tenant, null, cancellationToken);

            var threshold = decimal.TryParse(thresholdConfig?.EffectiveValue, out var t) ? t : 80m;

            // Check chargeback
            var chargebackConfig = await configService.ResolveEffectiveValueAsync(
                "finops.chargeback.enabled",
                ConfigurationScope.Tenant, null, cancellationToken);

            var chargebackEnabled = chargebackConfig?.EffectiveValue == "true";

            // Get service budget
            var serviceBudgetConfig = await configService.ResolveEffectiveValueAsync(
                "finops.budget.by_service",
                ConfigurationScope.Tenant, null, cancellationToken);

            decimal? serviceBudget = null;
            if (serviceBudgetConfig?.EffectiveValue is not null)
            {
                try
                {
                    var budgets = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serviceBudgetConfig.EffectiveValue);
                    if (budgets?.TryGetValue("default", out var defaultBudget) == true)
                    {
                        if (defaultBudget.TryGetProperty("monthlyBudget", out var monthly))
                            serviceBudget = monthly.GetDecimal();
                    }
                }
                catch
                {
                    // Invalid JSON — use null
                }
            }

            var isOverThreshold = request.CurrentSpendPct >= threshold;
            var isOverBudget = request.CurrentSpendPct >= 100;

            var alerts = new List<string>();
            if (isOverThreshold && !isOverBudget)
                alerts.Add($"Budget alert: spending at {request.CurrentSpendPct:F1}% (threshold: {threshold:F1}%)");
            if (isOverBudget)
                alerts.Add($"Budget exceeded: spending at {request.CurrentSpendPct:F1}%");
            if (chargebackEnabled)
                alerts.Add("Chargeback is active — costs will be allocated to team");

            return new Response(
                ServiceName: request.ServiceName,
                TeamName: request.TeamName,
                CurrentSpendPct: request.CurrentSpendPct,
                AlertThresholdPct: threshold,
                ServiceMonthlyBudget: serviceBudget,
                ChargebackEnabled: chargebackEnabled,
                IsOverThreshold: isOverThreshold,
                IsOverBudget: isOverBudget,
                Alerts: alerts,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação de FinOps.</summary>
    public sealed record Response(
        string ServiceName,
        string TeamName,
        decimal CurrentSpendPct,
        decimal AlertThresholdPct,
        decimal? ServiceMonthlyBudget,
        bool ChargebackEnabled,
        bool IsOverThreshold,
        bool IsOverBudget,
        List<string> Alerts,
        DateTimeOffset EvaluatedAt);
}
