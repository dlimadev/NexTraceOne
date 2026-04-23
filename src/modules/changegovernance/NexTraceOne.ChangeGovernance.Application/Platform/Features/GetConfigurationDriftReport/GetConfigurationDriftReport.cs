using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Platform.Features.GetConfigurationDriftReport;

/// <summary>
/// Feature: GetConfigurationDriftReport — detecção de deriva de configuração entre ambientes.
///
/// Analisa chaves de configuração do tenant e classifica divergências em:
/// <list type="bullet">
///   <item><c>Intentional</c> — diferença esperada e documentada</item>
///   <item><c>Unexplained</c> — divergência sem justificação</item>
///   <item><c>Stale</c> — chave não actualizada em nenhum ambiente há muito tempo</item>
/// </list>
///
/// Thresholds configuráveis via IConfigurationResolutionService:
/// - <c>platform.config_drift.stale_days</c> (default 90) — dias sem actualização para considerar Stale
/// - <c>platform.config_drift.high_impact_modules</c> (default "governance,sre,sbom") — módulos de alto impacto
///
/// Wave AU.1 — Platform Self-Optimization &amp; Adaptive Intelligence (ChangeGovernance Platform).
/// </summary>
public static class GetConfigurationDriftReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string StaleDaysKey = "platform.config_drift.stale_days";
    internal const string HighImpactModulesKey = "platform.config_drift.high_impact_modules";
    internal const int DefaultStaleDays = 90;
    internal const string DefaultHighImpactModules = "governance,sre,sbom";

    // ── Rollout readiness key fragments ───────────────────────────────────
    private static readonly string[] RolloutBlockPatterns = ["approval", "sla", "threshold"];

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tipo de divergência de configuração.</summary>
    public enum DivergenceType { Intentional, Unexplained, Stale }

    /// <summary>Tier de deriva de configuração do tenant.</summary>
    public enum ConfigDriftTier { Aligned, MinorDrift, MajorDrift, Critical }

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de deriva de configuração.</summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 180);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Relatório completo de deriva de configuração.</summary>
    public sealed record Report(
        string TenantId,
        ConfigDriftTier Tier,
        decimal TenantConfigurationHealthScore,
        IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow> HighImpactDivergences,
        IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow> StaleConfigKeys,
        IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow> RolloutReadinessBlocks,
        IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow> ConfigAlignmentRecommendations,
        int TotalKeysAnalysed,
        int UnexplainedCount,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IConfigurationDriftReader driftReader,
        IConfigurationResolutionService configService,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.LookbackDays);

            // Resolve config
            var staleDaysCfg = await configService.ResolveEffectiveValueAsync(
                StaleDaysKey, ConfigurationScope.System, null, cancellationToken);
            var highImpactCfg = await configService.ResolveEffectiveValueAsync(
                HighImpactModulesKey, ConfigurationScope.System, null, cancellationToken);

            var staleDays = int.TryParse(staleDaysCfg?.EffectiveValue, out var sd) ? sd : DefaultStaleDays;
            var highImpactModules = (highImpactCfg?.EffectiveValue ?? DefaultHighImpactModules)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allKeys = await driftReader.GetConfigKeyDriftAsync(request.TenantId, since, cancellationToken);

            var unexplainedKeys = allKeys.Where(k => k.DivergenceType == DivergenceType.Unexplained).ToList();
            var staleKeys = allKeys
                .Where(k => k.LastUpdatedAt.HasValue && (now - k.LastUpdatedAt.Value).TotalDays > staleDays)
                .ToList();

            var tier = ClassifyTier(unexplainedKeys.Count);

            var totalKeys = allKeys.Count;
            var unhealthyCount = allKeys.Count(k =>
                k.DivergenceType == DivergenceType.Unexplained || k.DivergenceType == DivergenceType.Stale);
            var healthScore = totalKeys == 0
                ? 100m
                : Math.Round((decimal)(totalKeys - unhealthyCount) / totalKeys * 100m, 2);

            var highImpactDivergences = unexplainedKeys
                .Where(k => highImpactModules.Contains(k.Module) || k.IsHighImpact)
                .ToList();

            var rolloutBlocks = unexplainedKeys
                .Where(k => RolloutBlockPatterns.Any(p =>
                    k.Key.Contains(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var recommendations = unexplainedKeys
                .OrderByDescending(k => k.IsHighImpact)
                .Take(5)
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                Tier: tier,
                TenantConfigurationHealthScore: healthScore,
                HighImpactDivergences: highImpactDivergences,
                StaleConfigKeys: staleKeys,
                RolloutReadinessBlocks: rolloutBlocks,
                ConfigAlignmentRecommendations: recommendations,
                TotalKeysAnalysed: totalKeys,
                UnexplainedCount: unexplainedKeys.Count,
                GeneratedAt: now));
        }

        private static ConfigDriftTier ClassifyTier(int unexplainedCount) => unexplainedCount switch
        {
            0 => ConfigDriftTier.Aligned,
            <= 3 => ConfigDriftTier.MinorDrift,
            <= 10 => ConfigDriftTier.MajorDrift,
            _ => ConfigDriftTier.Critical
        };
    }
}
