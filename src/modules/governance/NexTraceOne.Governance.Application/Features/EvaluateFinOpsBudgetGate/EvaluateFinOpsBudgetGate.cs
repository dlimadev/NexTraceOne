using Ardalis.GuardClauses;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;

namespace NexTraceOne.Governance.Application.Features.EvaluateFinOpsBudgetGate;

/// <summary>
/// Feature: EvaluateFinOpsBudgetGate — avalia se um serviço/equipa está dentro do orçamento FinOps.
/// Consulta:
///   - finops.budget.alert_thresholds: thresholds multi-tier (fallback: finops.budget_alert_threshold)
///   - finops.chargeback.enabled: se chargeback está ativo
///   - finops.budget.by_service: budget por serviço
///   - finops.budget.by_environment: restrições de orçamento por ambiente
/// Pilar: FinOps Contextual
/// </summary>
public static class EvaluateFinOpsBudgetGate
{
    /// <summary>Query para avaliar o gate de FinOps.</summary>
    public sealed record Query(
        string ServiceName,
        string TeamName,
        decimal CurrentSpendPct,
        string? Environment = null) : IQuery<Response>;

    /// <summary>Handler que avalia o gate de FinOps.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Threshold de alerta: usa chave multi-tier; cai na chave legada se não configurada ──
            var multiTierConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetAlertThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var resolvedThreshold = ResolveAlertThreshold(multiTierConfig?.EffectiveValue);
            if (resolvedThreshold == null)
            {
                var legacyConfig = await configService.ResolveEffectiveValueAsync(
                    "finops.budget_alert_threshold", ConfigurationScope.Tenant, null, cancellationToken);
                resolvedThreshold = decimal.TryParse(legacyConfig?.EffectiveValue, out var tp) ? tp : 80m;
            }
            var threshold = resolvedThreshold.Value;

            // ── Chargeback ──
            var chargebackConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsChargebackEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var chargebackEnabled = chargebackConfig?.EffectiveValue == "true";

            // ── Orçamento por serviço ──
            var serviceBudgetConfig = await configService.ResolveEffectiveValueAsync(
                "finops.budget.by_service", ConfigurationScope.Tenant, null, cancellationToken);
            decimal? serviceBudget = ExtractServiceBudget(serviceBudgetConfig?.EffectiveValue, request.ServiceName);

            // ── Orçamento por ambiente ──
            decimal? environmentBudget = null;
            if (!string.IsNullOrWhiteSpace(request.Environment))
            {
                var envBudgetConfig = await configService.ResolveEffectiveValueAsync(
                    GovernanceConfigKeys.FinOpsBudgetByEnvironment, ConfigurationScope.Tenant, null, cancellationToken);
                environmentBudget = ExtractEnvironmentBudget(envBudgetConfig?.EffectiveValue, request.Environment);
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
            if (environmentBudget.HasValue && request.CurrentSpendPct >= 100)
                alerts.Add($"Environment budget cap: {environmentBudget.Value:N2} for {request.Environment}");

            return new Response(
                ServiceName: request.ServiceName,
                TeamName: request.TeamName,
                CurrentSpendPct: request.CurrentSpendPct,
                AlertThresholdPct: threshold,
                ServiceMonthlyBudget: serviceBudget,
                EnvironmentMonthlyBudget: environmentBudget,
                ChargebackEnabled: chargebackEnabled,
                IsOverThreshold: isOverThreshold,
                IsOverBudget: isOverBudget,
                Alerts: alerts,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }

        private static decimal? ResolveAlertThreshold(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array) return null;
                decimal? min = null;
                foreach (var tier in root.EnumerateArray())
                {
                    if (tier.TryGetProperty("percent", out var p) && p.TryGetDecimal(out var pv))
                        min = min == null ? pv : Math.Min(min.Value, pv);
                }
                return min;
            }
            catch { return null; }
        }

        private static decimal? ExtractServiceBudget(string? json, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var budgets = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (budgets == null) return null;
                if (budgets.TryGetValue(serviceName, out var entry) && entry.TryGetProperty("monthlyBudget", out var mb))
                    return mb.GetDecimal();
                if (budgets.TryGetValue("default", out var defaultEntry) && defaultEntry.TryGetProperty("monthlyBudget", out var dm))
                    return dm.GetDecimal();
                return null;
            }
            catch { return null; }
        }

        private static decimal? ExtractEnvironmentBudget(string? json, string environment)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var budgets = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (budgets?.TryGetValue(environment, out var entry) == true
                    && entry.TryGetProperty("monthlyBudget", out var mb))
                    return mb.GetDecimal();
                return null;
            }
            catch { return null; }
        }
    }

    /// <summary>Resposta da avaliação de FinOps.</summary>
    public sealed record Response(
        string ServiceName,
        string TeamName,
        decimal CurrentSpendPct,
        decimal AlertThresholdPct,
        decimal? ServiceMonthlyBudget,
        decimal? EnvironmentMonthlyBudget,
        bool ChargebackEnabled,
        bool IsOverThreshold,
        bool IsOverBudget,
        List<string> Alerts,
        DateTimeOffset EvaluatedAt);
}
