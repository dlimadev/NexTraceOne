using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;

namespace NexTraceOne.Governance.Application.Features.EvaluateReleaseBudgetGate;

/// <summary>
/// Feature: EvaluateReleaseBudgetGate — avalia se uma release ultrapassa o orçamento FinOps
/// e determina a acção a tomar (Allow / Warn / Block / RequireApproval).
///
/// Lógica:
///   1. Se o gate estiver desabilitado → Allow (bypass).
///   2. Se actualCost &lt;= budget * alertThreshold% → Allow.
///   3. Se actualCost &gt; budget mas blockOnExceed=false → Warn.
///   4. Se actualCost &gt; budget e blockOnExceed=true e requireApproval=false → Block.
///   5. Se actualCost &gt; budget e blockOnExceed=true e requireApproval=true → RequireApproval.
///
/// Pilar: FinOps contextual — ConfidenceGate antes de promover para produção.
/// </summary>
public static class EvaluateReleaseBudgetGate
{
    /// <summary>Query para avaliar o gate de orçamento de uma release.</summary>
    public sealed record Query(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal ActualCostPerDay,
        decimal BaselineCostPerDay,
        int MeasurementDays) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ActualCostPerDay).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BaselineCostPerDay).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MeasurementDays).GreaterThan(0);
        }
    }

    /// <summary>Handler que avalia o gate de orçamento de uma release.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Ler configuração do gate ──
            var gateEnabledConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var gateEnabled = gateEnabledConfig?.EffectiveValue == "true";

            var currencyConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsCurrency, ConfigurationScope.Tenant, null, cancellationToken);
            var currency = currencyConfig?.EffectiveValue is { Length: 3 } c ? c.ToUpperInvariant() : "USD";

            // ── Custo total da janela de medição ──
            var actualTotalCost = request.ActualCostPerDay * request.MeasurementDays;
            var baselineTotalCost = request.BaselineCostPerDay * request.MeasurementDays;
            var costDelta = actualTotalCost - baselineTotalCost;
            var costDeltaPct = baselineTotalCost > 0
                ? Math.Round((costDelta / baselineTotalCost) * 100, 2)
                : 0m;

            // ── Gate desabilitado → bypass ──
            if (!gateEnabled)
            {
                return Result<Response>.Success(new Response(
                    ReleaseId: request.ReleaseId,
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    ActualTotalCost: actualTotalCost,
                    BaselineTotalCost: baselineTotalCost,
                    CostDelta: costDelta,
                    CostDeltaPct: costDeltaPct,
                    Currency: currency,
                    Action: BudgetGateAction.Allow,
                    Reason: "FinOps budget gate is disabled for this tenant.",
                    EvaluatedAt: dateTimeProvider.UtcNow));
            }

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
            var alertThresholdPct = resolvedThreshold.Value;

            // ── Dentro do threshold → Allow ──
            if (baselineTotalCost == 0 || costDeltaPct <= alertThresholdPct)
            {
                return Result<Response>.Success(new Response(
                    ReleaseId: request.ReleaseId,
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    ActualTotalCost: actualTotalCost,
                    BaselineTotalCost: baselineTotalCost,
                    CostDelta: costDelta,
                    CostDeltaPct: costDeltaPct,
                    Currency: currency,
                    Action: BudgetGateAction.Allow,
                    Reason: $"Cost delta {costDeltaPct:F1}% is within the alert threshold ({alertThresholdPct:F1}%).",
                    EvaluatedAt: dateTimeProvider.UtcNow));
            }

            // ── Orçamento excedido: determinar acção ──
            var blockConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateBlockOnExceed, ConfigurationScope.Tenant, null, cancellationToken);
            var blockOnExceed = blockConfig?.EffectiveValue != "false";

            if (!blockOnExceed)
            {
                return Result<Response>.Success(new Response(
                    ReleaseId: request.ReleaseId,
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    ActualTotalCost: actualTotalCost,
                    BaselineTotalCost: baselineTotalCost,
                    CostDelta: costDelta,
                    CostDeltaPct: costDeltaPct,
                    Currency: currency,
                    Action: BudgetGateAction.Warn,
                    Reason: $"Cost delta {costDeltaPct:F1}% exceeds alert threshold ({alertThresholdPct:F1}%). Gate is in warning-only mode.",
                    EvaluatedAt: dateTimeProvider.UtcNow));
            }

            var approvalConfig = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetGateRequireApproval, ConfigurationScope.Tenant, null, cancellationToken);
            var requireApproval = approvalConfig?.EffectiveValue != "false";

            var action = requireApproval ? BudgetGateAction.RequireApproval : BudgetGateAction.Block;
            var reason = action == BudgetGateAction.RequireApproval
                ? $"Cost delta {costDeltaPct:F1}% exceeds threshold. Promotion is blocked pending budget approval."
                : $"Cost delta {costDeltaPct:F1}% exceeds threshold. Promotion is blocked — manual override is required.";

            return Result<Response>.Success(new Response(
                ReleaseId: request.ReleaseId,
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                ActualTotalCost: actualTotalCost,
                BaselineTotalCost: baselineTotalCost,
                CostDelta: costDelta,
                CostDeltaPct: costDeltaPct,
                Currency: currency,
                Action: action,
                Reason: reason,
                EvaluatedAt: dateTimeProvider.UtcNow));
        }

        /// <summary>
        /// Extrai o threshold de alerta (%) da chave multi-tier <c>finops.budget.alert_thresholds</c>.
        /// Retorna o menor percent encontrado, ou null se o JSON for inválido/vazio.
        /// </summary>
        private static decimal? ResolveAlertThreshold(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != System.Text.Json.JsonValueKind.Array) return null;
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

    /// <summary>Acção determinada pelo gate de orçamento.</summary>
    public enum BudgetGateAction
    {
        /// <summary>Promoção permitida — dentro do orçamento.</summary>
        Allow,
        /// <summary>Promoção permitida com aviso — orçamento excedido mas gate em modo soft.</summary>
        Warn,
        /// <summary>Promoção bloqueada — orçamento excedido e sem possibilidade de override por aprovação.</summary>
        Block,
        /// <summary>Promoção bloqueada mas pode ser desbloqueada mediante aprovação de um responsável.</summary>
        RequireApproval,
    }

    /// <summary>Resultado da avaliação do gate de orçamento de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal ActualTotalCost,
        decimal BaselineTotalCost,
        decimal CostDelta,
        decimal CostDeltaPct,
        string Currency,
        BudgetGateAction Action,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
