using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRiskTrendReport;

/// <summary>
/// Feature: GetRiskTrendReport — relatório de distribuição e tendência de risco por serviço.
///
/// Agrega todos os perfis de risco activos do tenant e produz:
/// - distribuição de serviços por nível de risco (Negligible/Low/Medium/High/Critical)
/// - lista dos serviços de maior risco (top N por score descrescente)
/// - score médio global de risco
/// - percentagem de serviços em risco alto ou crítico
/// - score médio por dimensão (vulnerabilidade, change failure, blast radius, policy violation)
///
/// Serve Architect, Risk Manager e Executive como painel de priorização de risco operacional.
///
/// Wave N.2 — Risk Trend Report (ChangeGovernance Compliance).
/// </summary>
public static class GetRiskTrendReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>MaxHighRiskServices</c>: número máximo de serviços de alto risco no ranking (1–100, default 20).</para>
    /// <para><c>HighRiskThreshold</c>: score mínimo para classificação como High/Critical (1–100, default 60).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int MaxHighRiskServices = 20,
        int HighRiskThreshold = 60) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por nível de risco.</summary>
    public sealed record RiskDistribution(
        int NegligibleCount,
        int LowCount,
        int MediumCount,
        int HighCount,
        int CriticalCount);

    /// <summary>Score médio por dimensão de risco.</summary>
    public sealed record RiskDimensionAverages(
        decimal AvgVulnerabilityScore,
        decimal AvgChangeFailureScore,
        decimal AvgBlastRadiusScore,
        decimal AvgPolicyViolationScore,
        decimal AvgOverallScore);

    /// <summary>Entrada de um serviço no ranking de risco.</summary>
    public sealed record HighRiskServiceEntry(
        string ServiceName,
        RiskLevel RiskLevel,
        int OverallScore,
        int VulnerabilityScore,
        int ChangeFailureScore,
        int BlastRadiusScore,
        int PolicyViolationScore,
        int ActiveSignalCount);

    /// <summary>Resultado do relatório de tendência de risco.</summary>
    public sealed record Report(
        int TotalServicesAnalyzed,
        decimal HighCriticalPercent,
        decimal AvgOverallRiskScore,
        RiskDistribution RiskDistribution,
        RiskDimensionAverages DimensionAverages,
        IReadOnlyList<HighRiskServiceEntry> TopHighRiskServices,
        string TenantId,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxHighRiskServices).InclusiveBetween(1, 100);
            RuleFor(q => q.HighRiskThreshold).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IServiceRiskProfileRepository serviceRiskProfileRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            // Fetch latest risk profiles for all services in the tenant (up to 500)
            var profiles = await serviceRiskProfileRepository.ListByTenantRankedAsync(
                query.TenantId, maxResults: 500, ct: cancellationToken);

            if (profiles.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalServicesAnalyzed: 0,
                    HighCriticalPercent: 0m,
                    AvgOverallRiskScore: 0m,
                    RiskDistribution: new RiskDistribution(0, 0, 0, 0, 0),
                    DimensionAverages: new RiskDimensionAverages(0m, 0m, 0m, 0m, 0m),
                    TopHighRiskServices: [],
                    TenantId: query.TenantId,
                    GeneratedAt: clock.UtcNow));
            }

            var total = profiles.Count;

            // Risk distribution
            var dist = new RiskDistribution(
                NegligibleCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Negligible),
                LowCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Low),
                MediumCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Medium),
                HighCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.High),
                CriticalCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Critical));

            var highCriticalCount = dist.HighCount + dist.CriticalCount;
            var highCriticalPercent = Math.Round((decimal)highCriticalCount / total * 100m, 1);
            var avgOverall = Math.Round(profiles.Average(p => (decimal)p.OverallScore), 1);

            // Dimension averages
            var dims = new RiskDimensionAverages(
                AvgVulnerabilityScore: Math.Round(profiles.Average(p => (decimal)p.VulnerabilityScore), 1),
                AvgChangeFailureScore: Math.Round(profiles.Average(p => (decimal)p.ChangeFailureScore), 1),
                AvgBlastRadiusScore: Math.Round(profiles.Average(p => (decimal)p.BlastRadiusScore), 1),
                AvgPolicyViolationScore: Math.Round(profiles.Average(p => (decimal)p.PolicyViolationScore), 1),
                AvgOverallScore: avgOverall);

            // Top high-risk services (already sorted descending by ListByTenantRankedAsync)
            var topHighRisk = profiles
                .Where(p => p.OverallScore >= query.HighRiskThreshold)
                .Take(query.MaxHighRiskServices)
                .Select(p => new HighRiskServiceEntry(
                    ServiceName: p.ServiceName,
                    RiskLevel: p.OverallRiskLevel,
                    OverallScore: p.OverallScore,
                    VulnerabilityScore: p.VulnerabilityScore,
                    ChangeFailureScore: p.ChangeFailureScore,
                    BlastRadiusScore: p.BlastRadiusScore,
                    PolicyViolationScore: p.PolicyViolationScore,
                    ActiveSignalCount: p.ActiveSignalCount))
                .ToList();

            return Result<Report>.Success(new Report(
                TotalServicesAnalyzed: total,
                HighCriticalPercent: highCriticalPercent,
                AvgOverallRiskScore: avgOverall,
                RiskDistribution: dist,
                DimensionAverages: dims,
                TopHighRiskServices: topHighRisk,
                TenantId: query.TenantId,
                GeneratedAt: clock.UtcNow));
        }
    }
}
