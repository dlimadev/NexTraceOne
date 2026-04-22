using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiTokenBudgetReport;

/// <summary>
/// Feature: GetAiTokenBudgetReport — relatório de consumo de tokens por período com burn rate.
/// Agrega consumo por equipa, caso de uso e utilizador para suportar FinOps e governança de IA.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetAiTokenBudgetReport
{
    private const string MonthlyLimitKey = "ai.budget.monthly_token_limit_default";
    private const string AlertThresholdKey = "ai.budget.alert_threshold_pct";
    private const long DefaultMonthlyLimit = 1_000_000;
    private const int DefaultAlertPct = 80;

    /// <summary>Query para o relatório de budget de tokens de IA.</summary>
    public sealed record Query(
        Guid? TenantId,
        string? TeamId,
        int PeriodDays = 30) : IQuery<Response>;

    /// <summary>Validador da query de budget report.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PeriodDays).InclusiveBetween(1, 365);
        }
    }

    /// <summary>Handler que agrega o consumo de tokens e calcula burn rate.</summary>
    public sealed class Handler(
        IAiTokenUsageLedgerRepository ledgerRepository,
        IDateTimeProvider dateTimeProvider,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var cutoff = dateTimeProvider.UtcNow.AddDays(-request.PeriodDays);
            var entries = await ledgerRepository.ListByPeriodAsync(cutoff, ct);

            var filtered = entries.AsEnumerable();
            if (request.TenantId.HasValue)
                filtered = filtered.Where(e => e.TenantId == request.TenantId.Value);

            var list = filtered.ToList();

            var monthlyLimitCfg = await configService.ResolveEffectiveValueAsync(
                MonthlyLimitKey, ConfigurationScope.System, null, ct);
            var alertPctCfg = await configService.ResolveEffectiveValueAsync(
                AlertThresholdKey, ConfigurationScope.System, null, ct);

            var monthlyLimit = long.TryParse(monthlyLimitCfg?.EffectiveValue, out var ml)
                ? ml : DefaultMonthlyLimit;
            var alertPct = int.TryParse(alertPctCfg?.EffectiveValue, out var ap)
                ? ap : DefaultAlertPct;

            var totalTokens = list.Sum(e => (long)e.TotalTokens);
            var totalCost = list.Sum(e => e.EstimatedCostUsd ?? 0m);
            var burnRate = monthlyLimit > 0
                ? Math.Round((double)totalTokens / monthlyLimit * 100, 2) : 0.0;

            // Agrega por utilizador (proxy de equipa, sem TeamId no ledger)
            var byTeam = list
                .GroupBy(e => e.UserId)
                .Select(g => new BudgetByDimension(
                    Label: g.Key,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    CostUsd: g.Sum(e => e.EstimatedCostUsd ?? 0m)))
                .OrderByDescending(x => x.TotalTokens)
                .Take(20)
                .ToList();

            // Classifica caso de uso por prefixo de modelo
            var byUseCase = list
                .GroupBy(e => ClassifyUseCase(e.ModelId))
                .Select(g => new BudgetByDimension(
                    Label: g.Key,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    CostUsd: g.Sum(e => e.EstimatedCostUsd ?? 0m)))
                .ToList();

            var topConsumers = list
                .GroupBy(e => e.UserId)
                .Select(g => new TopConsumer(
                    UserId: g.Key,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    CostUsd: g.Sum(e => e.EstimatedCostUsd ?? 0m),
                    RequestCount: g.Count()))
                .OrderByDescending(x => x.TotalTokens)
                .Take(10)
                .ToList();

            var timeline = list
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new DailyUsage(
                    Date: g.Key,
                    Tokens: g.Sum(e => (long)e.TotalTokens),
                    CostUsd: g.Sum(e => e.EstimatedCostUsd ?? 0m)))
                .OrderBy(x => x.Date)
                .ToList();

            return new Response(
                TotalTokens: totalTokens,
                TotalCostUsd: totalCost,
                BurnRatePct: burnRate,
                AlertThresholdPct: alertPct,
                IsApproachingLimit: burnRate >= alertPct,
                ByTeam: byTeam,
                ByUseCase: byUseCase,
                TopConsumers: topConsumers,
                Timeline: timeline,
                PeriodDays: request.PeriodDays);
        }

        private static string ClassifyUseCase(string modelId)
        {
            if (string.IsNullOrEmpty(modelId)) return "InternalAI";
            var lower = modelId.ToLowerInvariant();
            return lower.StartsWith("gpt", StringComparison.Ordinal) ||
                   lower.StartsWith("claude", StringComparison.Ordinal)
                ? "ExternalAI" : "InternalAI";
        }
    }

    public sealed record BudgetByDimension(string Label, long TotalTokens, decimal CostUsd);
    public sealed record TopConsumer(string UserId, long TotalTokens, decimal CostUsd, int RequestCount);
    public sealed record DailyUsage(DateTime Date, long Tokens, decimal CostUsd);

    /// <summary>Resposta do relatório de budget de tokens.</summary>
    public sealed record Response(
        long TotalTokens,
        decimal TotalCostUsd,
        double BurnRatePct,
        int AlertThresholdPct,
        bool IsApproachingLimit,
        IReadOnlyList<BudgetByDimension> ByTeam,
        IReadOnlyList<BudgetByDimension> ByUseCase,
        IReadOnlyList<TopConsumer> TopConsumers,
        IReadOnlyList<DailyUsage> Timeline,
        int PeriodDays);
}
