using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetPlatformAdoptionReport;

/// <summary>
/// Feature: GetPlatformAdoptionReport — adoção de capacidades core da plataforma NexTraceOne por equipa.
///
/// Avalia cada equipa em sete capacidades da plataforma:
/// SloTracking, ChaosEngineering, ContinuousProfiling, ComplianceReports,
/// ChangeConfidence, ReleaseCalendar e AiAssistant.
///
/// Calcula AdoptionScore = CapabilitiesUsed / 7 × 100 e atribui <see cref="AdoptionTier"/>
/// com base em thresholds configuráveis. Identifica GrowthOpportunities —
/// capacidades com taxa de adoção inferior a 30%.
///
/// Wave AC.3 — GetPlatformAdoptionReport (OperationalIntelligence).
/// </summary>
public static class GetPlatformAdoptionReport
{
    // ── Tier de adoção ────────────────────────────────────────────────────

    /// <summary>Nível de adoção de capacidades da plataforma por uma equipa.</summary>
    public enum AdoptionTier
    {
        /// <summary>Score ≥ threshold Pioneer (padrão: 80).</summary>
        Pioneer,
        /// <summary>Score ≥ threshold Adopter (padrão: 60) e &lt; Pioneer.</summary>
        Adopter,
        /// <summary>Score ≥ threshold Explorer (padrão: 40) e &lt; Adopter.</summary>
        Explorer,
        /// <summary>Score &lt; threshold Explorer.</summary>
        Laggard
    }

    private const int TotalCapabilities = 7;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Sumário de adoção de capacidades de uma equipa.</summary>
    public sealed record TeamAdoptionSummary(
        string TeamName,
        bool UsesSloTracking,
        bool UsesChaosEngineering,
        bool UsesContinuousProfiling,
        bool UsesComplianceReports,
        bool UsesChangeConfidence,
        bool UsesReleaseCalendar,
        bool UsesAiAssistant,
        int CapabilitiesUsed,
        double AdoptionScore,
        AdoptionTier Tier);

    /// <summary>Taxa de adoção de uma capacidade específica.</summary>
    public sealed record CapabilityAdoptionRate(
        string CapabilityName,
        int TeamsUsing,
        int TotalTeams,
        double AdoptionRate);

    /// <summary>Relatório completo de adoção de plataforma do tenant.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<TeamAdoptionSummary> Teams,
        IReadOnlyList<CapabilityAdoptionRate> CapabilityRates,
        IReadOnlyList<string> GrowthOpportunities,
        double GlobalAdoptionScore,
        int PioneerCount,
        int AdopterCount,
        int ExplorerCount,
        int LaggardCount,
        int TotalTeams);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>SloLookbackDays</c>: janela de lookback para SLO (7–90, padrão 30).</para>
    /// <para><c>FeatureLookbackDays</c>: janela de lookback para features (7–365, padrão 90).</para>
    /// <para><c>PioneerThreshold</c>: score mínimo para tier Pioneer (padrão 80).</para>
    /// <para><c>AdopterThreshold</c>: score mínimo para tier Adopter (padrão 60).</para>
    /// <para><c>ExplorerThreshold</c>: score mínimo para tier Explorer (padrão 40).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int SloLookbackDays = 30,
        int FeatureLookbackDays = 90,
        int PioneerThreshold = 80,
        int AdopterThreshold = 60,
        int ExplorerThreshold = 40) : IQuery<Report>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.SloLookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.FeatureLookbackDays).InclusiveBetween(7, 365);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IPlatformAdoptionReader _reader;

        public Handler(IPlatformAdoptionReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(
                query.TenantId,
                query.SloLookbackDays,
                query.FeatureLookbackDays,
                cancellationToken);

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TenantId: query.TenantId,
                    Teams: [],
                    CapabilityRates: [],
                    GrowthOpportunities: [],
                    GlobalAdoptionScore: 0.0,
                    PioneerCount: 0,
                    AdopterCount: 0,
                    ExplorerCount: 0,
                    LaggardCount: 0,
                    TotalTeams: 0));
            }

            var teams = entries.Select(e => BuildTeamSummary(e, query)).ToList();

            var capabilityRates = BuildCapabilityRates(entries, teams.Count);

            var growthOpportunities = capabilityRates
                .Where(r => r.AdoptionRate < 30.0)
                .Select(r => r.CapabilityName)
                .OrderBy(name => name)
                .ToList();

            var globalScore = teams.Average(t => t.AdoptionScore);

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                Teams: teams,
                CapabilityRates: capabilityRates,
                GrowthOpportunities: growthOpportunities,
                GlobalAdoptionScore: globalScore,
                PioneerCount: teams.Count(t => t.Tier == AdoptionTier.Pioneer),
                AdopterCount: teams.Count(t => t.Tier == AdoptionTier.Adopter),
                ExplorerCount: teams.Count(t => t.Tier == AdoptionTier.Explorer),
                LaggardCount: teams.Count(t => t.Tier == AdoptionTier.Laggard),
                TotalTeams: teams.Count));
        }

        // ── Construção do sumário de equipa ───────────────────────────────

        private static TeamAdoptionSummary BuildTeamSummary(TeamCapabilityAdoptionEntry entry, Query query)
        {
            var used =
                (entry.UsesSloTracking ? 1 : 0) +
                (entry.UsesChaosEngineering ? 1 : 0) +
                (entry.UsesContinuousProfiling ? 1 : 0) +
                (entry.UsesComplianceReports ? 1 : 0) +
                (entry.UsesChangeConfidence ? 1 : 0) +
                (entry.UsesReleaseCalendar ? 1 : 0) +
                (entry.UsesAiAssistant ? 1 : 0);

            var score = (double)used / TotalCapabilities * 100.0;

            var tier = score >= query.PioneerThreshold ? AdoptionTier.Pioneer
                : score >= query.AdopterThreshold ? AdoptionTier.Adopter
                : score >= query.ExplorerThreshold ? AdoptionTier.Explorer
                : AdoptionTier.Laggard;

            return new TeamAdoptionSummary(
                TeamName: entry.TeamName,
                UsesSloTracking: entry.UsesSloTracking,
                UsesChaosEngineering: entry.UsesChaosEngineering,
                UsesContinuousProfiling: entry.UsesContinuousProfiling,
                UsesComplianceReports: entry.UsesComplianceReports,
                UsesChangeConfidence: entry.UsesChangeConfidence,
                UsesReleaseCalendar: entry.UsesReleaseCalendar,
                UsesAiAssistant: entry.UsesAiAssistant,
                CapabilitiesUsed: used,
                AdoptionScore: score,
                Tier: tier);
        }

        // ── Cálculo de taxas de adoção por capacidade ─────────────────────

        private static IReadOnlyList<CapabilityAdoptionRate> BuildCapabilityRates(
            IReadOnlyList<TeamCapabilityAdoptionEntry> entries,
            int totalTeams)
        {
            return
            [
                BuildRate("SloTracking", entries.Count(e => e.UsesSloTracking), totalTeams),
                BuildRate("ChaosEngineering", entries.Count(e => e.UsesChaosEngineering), totalTeams),
                BuildRate("ContinuousProfiling", entries.Count(e => e.UsesContinuousProfiling), totalTeams),
                BuildRate("ComplianceReports", entries.Count(e => e.UsesComplianceReports), totalTeams),
                BuildRate("ChangeConfidence", entries.Count(e => e.UsesChangeConfidence), totalTeams),
                BuildRate("ReleaseCalendar", entries.Count(e => e.UsesReleaseCalendar), totalTeams),
                BuildRate("AiAssistant", entries.Count(e => e.UsesAiAssistant), totalTeams),
            ];
        }

        private static CapabilityAdoptionRate BuildRate(string name, int teamsUsing, int total) =>
            new(
                CapabilityName: name,
                TeamsUsing: teamsUsing,
                TotalTeams: total,
                AdoptionRate: total > 0 ? (double)teamsUsing / total * 100.0 : 0.0);
    }
}
