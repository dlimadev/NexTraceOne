using System.Text.Json;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsConfiguration;

/// <summary>
/// Feature: GetFinOpsConfiguration — lê e devolve todas as configurações operacionais de FinOps.
/// Inclui: moeda, gate de orçamento, thresholds de alerta, anomalia, desperdício, recomendações e notificações.
/// Pilar: FinOps contextual — parametrização governada e auditável.
/// </summary>
public static class GetFinOpsConfiguration
{
    /// <summary>Query de leitura da configuração de FinOps.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler que agrega as configurações de FinOps via IConfigurationResolutionService.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // ── Moeda padrão ──
            var currencyConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsCurrency, ConfigurationScope.Tenant, null, cancellationToken);
            var currency = currencyConfig?.EffectiveValue is { Length: 3 } c ? c.ToUpperInvariant() : "USD";

            // ── Gate de orçamento por release ──
            var gateEnabledConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var gateEnabled = gateEnabledConfig?.EffectiveValue == "true";

            var blockConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateBlockOnExceed, ConfigurationScope.Tenant, null, cancellationToken);
            var blockOnExceed = blockConfig?.EffectiveValue != "false"; // default true

            var approvalConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateRequireApproval, ConfigurationScope.Tenant, null, cancellationToken);
            var requireApproval = approvalConfig?.EffectiveValue != "false"; // default true

            var approversConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateApprovers, ConfigurationScope.Tenant, null, cancellationToken);
            var approvers = ParseStringArray(approversConfig?.EffectiveValue);

            // ── Threshold de alerta: usa chave multi-tier; cai na chave legada se não configurada ──
            var multiTierConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetAlertThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            decimal? resolvedThreshold = ResolveAlertThreshold(multiTierConfig?.EffectiveValue);
            if (resolvedThreshold == null)
            {
                var legacyConfig = await configService.ResolveEffectiveValueAsync(
                    "finops.budget_alert_threshold", ConfigurationScope.Tenant, null, cancellationToken);
                resolvedThreshold = decimal.TryParse(legacyConfig?.EffectiveValue, out var t) ? t : 80m;
            }
            var alertThresholdPct = resolvedThreshold.Value;

            // ── Detecção de anomalias ──
            var anomalyEnabledConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsAnomalyDetectionEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var anomalyDetectionEnabled = anomalyEnabledConfig?.EffectiveValue != "false"; // default true

            var anomalyThresholdsConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsAnomalyThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var anomalyThresholds = anomalyThresholdsConfig?.EffectiveValue;

            var comparisonWindowConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsAnomalyComparisonWindowDays, ConfigurationScope.Tenant, null, cancellationToken);
            var comparisonWindowDays = int.TryParse(comparisonWindowConfig?.EffectiveValue, out var wd) ? wd : 30;

            // ── Detecção de desperdício ──
            var wasteEnabledConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteDetectionEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var wasteDetectionEnabled = wasteEnabledConfig?.EffectiveValue != "false"; // default true

            var wasteThresholdsConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var wasteThresholds = wasteThresholdsConfig?.EffectiveValue;

            var wasteCategoriesConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteCategories, ConfigurationScope.Tenant, null, cancellationToken);
            var wasteCategories = ParseStringArray(wasteCategoriesConfig?.EffectiveValue);

            // ── Recomendações ──
            var recommendationConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsRecommendationPolicy, ConfigurationScope.Tenant, null, cancellationToken);
            var recommendationPolicy = recommendationConfig?.EffectiveValue;

            // ── Notificações ──
            var notificationConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsNotificationPolicy, ConfigurationScope.Tenant, null, cancellationToken);
            var notificationPolicy = notificationConfig?.EffectiveValue;

            // ── Showback / Chargeback ──
            var showbackConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsShowbackEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var showbackEnabled = showbackConfig?.EffectiveValue == "true";

            var chargebackConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsChargebackEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var chargebackEnabled = chargebackConfig?.EffectiveValue == "true";

            return Result<Response>.Success(new Response(
                Currency: currency,
                BudgetGateEnabled: gateEnabled,
                BlockOnExceed: blockOnExceed,
                RequireApproval: requireApproval,
                Approvers: approvers,
                AlertThresholdPct: alertThresholdPct,
                AnomalyDetectionEnabled: anomalyDetectionEnabled,
                AnomalyThresholds: anomalyThresholds,
                ComparisonWindowDays: comparisonWindowDays,
                WasteDetectionEnabled: wasteDetectionEnabled,
                WasteThresholds: wasteThresholds,
                WasteCategories: wasteCategories,
                RecommendationPolicy: recommendationPolicy,
                NotificationPolicy: notificationPolicy,
                ShowbackEnabled: showbackEnabled,
                ChargebackEnabled: chargebackEnabled,
                ResolvedAt: dateTimeProvider.UtcNow));
        }

        private static IReadOnlyList<string> ParseStringArray(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
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
    }

    /// <summary>Configuração operacional de FinOps devolvida ao cliente.</summary>
    public sealed record Response(
        /// <summary>Código ISO 4217 da moeda padrão (ex: USD, EUR, BRL).</summary>
        string Currency,
        /// <summary>Gate de orçamento para promoções está activo.</summary>
        bool BudgetGateEnabled,
        /// <summary>Quando o orçamento é excedido, bloquear a promoção (vs apenas avisar).</summary>
        bool BlockOnExceed,
        /// <summary>Exigir aprovação manual quando a promoção é bloqueada por orçamento.</summary>
        bool RequireApproval,
        /// <summary>Lista de utilizadores/grupos autorizados a aprovar overrides de orçamento.</summary>
        IReadOnlyList<string> Approvers,
        /// <summary>Percentagem de utilização de orçamento que dispara alerta.</summary>
        decimal AlertThresholdPct,
        /// <summary>Deteção de anomalias de custo está activa.</summary>
        bool AnomalyDetectionEnabled,
        /// <summary>JSON com thresholds de deteção de anomalias (warning, high, critical em %).</summary>
        string? AnomalyThresholds,
        /// <summary>Janela de comparação em dias para deteção de anomalias.</summary>
        int ComparisonWindowDays,
        /// <summary>Deteção de desperdício está activa.</summary>
        bool WasteDetectionEnabled,
        /// <summary>JSON com thresholds de deteção de desperdício.</summary>
        string? WasteThresholds,
        /// <summary>Categorias de desperdício activas.</summary>
        IReadOnlyList<string> WasteCategories,
        /// <summary>JSON com a política de recomendações financeiras.</summary>
        string? RecommendationPolicy,
        /// <summary>JSON com a política de notificações FinOps.</summary>
        string? NotificationPolicy,
        /// <summary>Showback de custos por serviço/equipa/domínio está activo.</summary>
        bool ShowbackEnabled,
        /// <summary>Chargeback — custos debitados às equipas responsáveis — está activo.</summary>
        bool ChargebackEnabled,
        DateTimeOffset ResolvedAt);
}
