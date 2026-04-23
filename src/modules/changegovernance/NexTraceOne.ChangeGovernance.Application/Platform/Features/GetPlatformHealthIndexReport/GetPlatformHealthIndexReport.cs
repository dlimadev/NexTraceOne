using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Platform.Features.GetPlatformHealthIndexReport;

/// <summary>
/// Feature: GetPlatformHealthIndexReport — índice composto de saúde da plataforma.
///
/// Mede a profundidade de adopção das capacidades NexTraceOne pelo tenant em 7 dimensões:
/// <list type="bullet">
///   <item><c>ServiceCatalogCompleteness</c> — 15%</item>
///   <item><c>ContractCoverage</c> — 15%</item>
///   <item><c>ChangeGovernanceAdoption</c> — 15%</item>
///   <item><c>SloGovernanceAdoption</c> — 15%</item>
///   <item><c>ObservabilityContextualization</c> — 10%</item>
///   <item><c>AiGovernanceReadiness</c> — 15%</item>
///   <item><c>DataFreshness</c> — 15%</item>
/// </list>
///
/// Thresholds configuráveis via IConfigurationResolutionService:
/// - <c>platform.health.freshness_days</c> (default 30)
/// - <c>platform.health.optimized_threshold</c> (default 85)
/// - <c>platform.health.operational_threshold</c> (default 65)
///
/// Wave AU.2 — Platform Self-Optimization &amp; Adaptive Intelligence (ChangeGovernance Platform).
/// </summary>
public static class GetPlatformHealthIndexReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string FreshnessDaysKey = "platform.health.freshness_days";
    internal const string OptimizedThresholdKey = "platform.health.optimized_threshold";
    internal const string OperationalThresholdKey = "platform.health.operational_threshold";
    internal const int DefaultFreshnessDays = 30;
    internal const decimal DefaultOptimizedThreshold = 85m;
    internal const decimal DefaultOperationalThreshold = 65m;
    private const decimal PartialThreshold = 40m;

    // ── Dimension weights (must sum to 100) ───────────────────────────────
    internal static readonly IReadOnlyDictionary<string, decimal> DimensionWeights =
        new Dictionary<string, decimal>
        {
            ["ServiceCatalogCompleteness"] = 15m,
            ["ContractCoverage"] = 15m,
            ["ChangeGovernanceAdoption"] = 15m,
            ["SloGovernanceAdoption"] = 15m,
            ["ObservabilityContextualization"] = 10m,
            ["AiGovernanceReadiness"] = 15m,
            ["DataFreshness"] = 15m,
        };

    private const int TimelineMonths = 6;
    private const int WeakestDimensionCount = 3;

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tier de saúde da plataforma.</summary>
    public enum PlatformHealthTier { Optimized, Operational, Partial, Underutilized }

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de Platform Health Index.</summary>
    public sealed record Query(string TenantId) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Score de uma dimensão do Platform Health Index.</summary>
    public sealed record PlatformDimensionScore(
        string Name,
        decimal Score,
        decimal WeightPct,
        IReadOnlyList<string> ContributingNegatively);

    /// <summary>Relatório completo do Platform Health Index.</summary>
    public sealed record Report(
        string TenantId,
        decimal PlatformHealthIndex,
        PlatformHealthTier Tier,
        IReadOnlyList<PlatformDimensionScore> Dimensions,
        IReadOnlyList<PlatformDimensionScore> WeakestDimensions,
        decimal ValueRealizationScore,
        IReadOnlyList<IPlatformHealthIndexReader.PlatformHealthTimelinePoint> PlatformHealthTimeline,
        decimal? TenantBenchmarkPosition,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IPlatformHealthIndexReader healthReader,
        IConfigurationResolutionService configService,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            // Resolve config
            var freshnessCfg = await configService.ResolveEffectiveValueAsync(
                FreshnessDaysKey, ConfigurationScope.System, null, cancellationToken);
            var optimizedCfg = await configService.ResolveEffectiveValueAsync(
                OptimizedThresholdKey, ConfigurationScope.System, null, cancellationToken);
            var operationalCfg = await configService.ResolveEffectiveValueAsync(
                OperationalThresholdKey, ConfigurationScope.System, null, cancellationToken);

            var freshnessDays = int.TryParse(freshnessCfg?.EffectiveValue, out var fd) ? fd : DefaultFreshnessDays;
            var optimizedThreshold = decimal.TryParse(optimizedCfg?.EffectiveValue, out var ot) ? ot : DefaultOptimizedThreshold;
            var operationalThreshold = decimal.TryParse(operationalCfg?.EffectiveValue, out var odt) ? odt : DefaultOperationalThreshold;

            var since = now.AddDays(-freshnessDays);
            var rawData = await healthReader.GetPlatformHealthDataAsync(request.TenantId, since, cancellationToken);
            var timeline = await healthReader.GetTimelineAsync(request.TenantId, TimelineMonths, cancellationToken);

            var dimensions = BuildDimensions(rawData);
            var healthIndex = dimensions.Sum(d => d.Score * d.WeightPct / 100m);
            healthIndex = Math.Round(healthIndex, 2);

            var weakest = dimensions
                .OrderBy(d => d.Score)
                .Take(WeakestDimensionCount)
                .ToList();

            var valueRealization = ComputeValueRealizationScore(rawData);
            var tier = ClassifyTier(healthIndex, optimizedThreshold, operationalThreshold);

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                PlatformHealthIndex: healthIndex,
                Tier: tier,
                Dimensions: dimensions,
                WeakestDimensions: weakest,
                ValueRealizationScore: valueRealization,
                PlatformHealthTimeline: timeline,
                TenantBenchmarkPosition: rawData.BenchmarkPercentile,
                GeneratedAt: now));
        }

        private static IReadOnlyList<PlatformDimensionScore> BuildDimensions(
            IPlatformHealthIndexReader.PlatformHealthRawData raw)
        {
            return
            [
                new("ServiceCatalogCompleteness", raw.ServiceCatalogCompleteness, 15m, BuildNegatives("ServiceCatalogCompleteness", raw.ServiceCatalogCompleteness)),
                new("ContractCoverage", raw.ContractCoverage, 15m, BuildNegatives("ContractCoverage", raw.ContractCoverage)),
                new("ChangeGovernanceAdoption", raw.ChangeGovernanceAdoption, 15m, BuildNegatives("ChangeGovernanceAdoption", raw.ChangeGovernanceAdoption)),
                new("SloGovernanceAdoption", raw.SloGovernanceAdoption, 15m, BuildNegatives("SloGovernanceAdoption", raw.SloGovernanceAdoption)),
                new("ObservabilityContextualization", raw.ObservabilityContextualization, 10m, BuildNegatives("ObservabilityContextualization", raw.ObservabilityContextualization)),
                new("AiGovernanceReadiness", raw.AiGovernanceReadiness, 15m, BuildNegatives("AiGovernanceReadiness", raw.AiGovernanceReadiness)),
                new("DataFreshness", raw.DataFreshness, 15m, BuildNegatives("DataFreshness", raw.DataFreshness)),
            ];
        }

        private static IReadOnlyList<string> BuildNegatives(string dimensionName, decimal score)
        {
            if (score >= 70m) return [];
            return [$"{dimensionName} score is {score:F0} — below healthy threshold"];
        }

        private static decimal ComputeValueRealizationScore(IPlatformHealthIndexReader.PlatformHealthRawData raw)
        {
            // Geometric mean of ContractCoverage, ChangeGovernanceAdoption, SloGovernanceAdoption
            if (raw.ContractCoverage <= 0 || raw.ChangeGovernanceAdoption <= 0 || raw.SloGovernanceAdoption <= 0)
                return 0m;

            var product = (double)raw.ContractCoverage * (double)raw.ChangeGovernanceAdoption * (double)raw.SloGovernanceAdoption;
            return Math.Round((decimal)Math.Pow(product, 1.0 / 3.0), 2);
        }

        private static PlatformHealthTier ClassifyTier(
            decimal index, decimal optimizedThreshold, decimal operationalThreshold) =>
            index >= optimizedThreshold ? PlatformHealthTier.Optimized
            : index >= operationalThreshold ? PlatformHealthTier.Operational
            : index >= PartialThreshold ? PlatformHealthTier.Partial
            : PlatformHealthTier.Underutilized;
    }
}
