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
/// Inclui: moeda, gate de orçamento, thresholds de alerta, configurações de aprovação.
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

            // ── Threshold de alerta ──
            var thresholdConfig = await configService.ResolveEffectiveValueAsync(
                "finops.budget_alert_threshold", ConfigurationScope.Tenant, null, cancellationToken);
            var alertThresholdPct = decimal.TryParse(thresholdConfig?.EffectiveValue, out var t) ? t : 80m;

            return Result<Response>.Success(new Response(
                Currency: currency,
                BudgetGateEnabled: gateEnabled,
                BlockOnExceed: blockOnExceed,
                RequireApproval: requireApproval,
                Approvers: approvers,
                AlertThresholdPct: alertThresholdPct,
                ResolvedAt: dateTimeProvider.UtcNow));
        }

        private static IReadOnlyList<string> ParseStringArray(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch
            {
                return [];
            }
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
        DateTimeOffset ResolvedAt);
}
