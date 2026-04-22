using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentKnowledgeBaseReport;

/// <summary>
/// Feature: GetIncidentKnowledgeBaseReport — base de conhecimento operacional de incidentes.
///
/// Agrega o aprendizado operacional a partir do histórico de incidentes e runbooks:
/// - <c>ResolutionConfidence</c>: % de ocorrências com runbook aprovado disponível
/// - <c>MeanTimeToRunbookMinutes</c>: tempo médio de abertura do incidente até aplicação de runbook
/// - <c>RunbookEffectivenessScore</c>: % de resoluções via runbook sem reabertura
/// - <c>KnowledgeGap</c>: tipo recorrente (>3 ocorrências) sem runbook aprovado
/// - <c>StaleRunbook</c>: runbook não revisto dentro do prazo configurado
/// - <c>KnowledgeMaturityScore</c>: média ponderada de ResolutionConfidence (50%) + RunbookEffectivenessScore (50%)
/// - <c>KnowledgeMaturityLevel</c>: Exemplary ≥0.85, Mature ≥0.70, Developing ≥0.50, Nascent &lt;0.50
/// - <c>TopGaps</c>: top N tipos com KnowledgeGap=true, ordenados por frequência
///
/// Orienta Tech Lead, Platform Admin e Engineer a fechar o loop entre incidentes operacionais
/// e o Knowledge Hub, transformando histórico em aprendizado governado e mensurável.
///
/// Wave AB.3 — GetIncidentKnowledgeBaseReport (OperationalIntelligence Runtime).
/// </summary>
public static class GetIncidentKnowledgeBaseReport
{
    // ── Limiares de maturidade ────────────────────────────────────────────
    private const double ExemplaryThreshold = 0.85;
    private const double MatureThreshold = 0.70;
    private const double DevelopingThreshold = 0.50;

    // ── Limiar de knowledge gap ───────────────────────────────────────────
    private const int KnowledgeGapMinOccurrences = 3;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Nível de maturidade do conhecimento operacional do tenant.</summary>
    public enum KnowledgeMaturityLevel
    {
        /// <summary>Score &lt; 0.50 — conhecimento incipiente, cobertura de runbooks muito baixa.</summary>
        Nascent,
        /// <summary>Score ≥ 0.50 — cobertura e efectividade em crescimento.</summary>
        Developing,
        /// <summary>Score ≥ 0.70 — boa cobertura e efectividade de runbooks.</summary>
        Mature,
        /// <summary>Score ≥ 0.85 — excelente cobertura e efectividade de runbooks.</summary>
        Exemplary
    }

    /// <summary>Resumo de conhecimento operacional de um tipo de incidente.</summary>
    public sealed record IncidentTypeKnowledgeSummary(
        string IncidentType,
        int TotalOccurrences,
        double ResolutionConfidence,
        double MeanTimeToRunbookMinutes,
        double RunbookEffectivenessScore,
        bool KnowledgeGap,
        bool StaleRunbook,
        bool IsTrendIncreasing);

    /// <summary>Resultado do relatório de base de conhecimento de incidentes.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<IncidentTypeKnowledgeSummary> IncidentTypes,
        IReadOnlyList<IncidentTypeKnowledgeSummary> TopGaps,
        int TotalIncidentTypes,
        int TypesWithKnowledgeGap,
        int TypesWithStaleRunbook,
        double KnowledgeMaturityScore,
        KnowledgeMaturityLevel MaturityLevel);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–730, default 365).</para>
    /// <para><c>StaleRunbookDays</c>: dias sem revisão para classificar runbook como stale (1–730, default 180).</para>
    /// <para><c>TopIncidents</c>: número máximo de tipos de incidente no ranking de gaps (1–100, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 365,
        int StaleRunbookDays = 180,
        int TopIncidents = 10) : IQuery<Report>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 730);
            RuleFor(q => q.StaleRunbookDays).InclusiveBetween(1, 730);
            RuleFor(q => q.TopIncidents).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IIncidentKnowledgeReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IIncidentKnowledgeReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, cancellationToken);

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TenantId: query.TenantId,
                    IncidentTypes: [],
                    TopGaps: [],
                    TotalIncidentTypes: 0,
                    TypesWithKnowledgeGap: 0,
                    TypesWithStaleRunbook: 0,
                    KnowledgeMaturityScore: 0.0,
                    MaturityLevel: KnowledgeMaturityLevel.Nascent));
            }

            var summaries = entries
                .Where(e => e.TotalOccurrences > 0)
                .Select(e => BuildSummary(e))
                .ToList();

            var topGaps = summaries
                .Where(s => s.KnowledgeGap)
                .OrderByDescending(s => s.TotalOccurrences)
                .Take(query.TopIncidents)
                .ToList();

            int typesWithGap = summaries.Count(s => s.KnowledgeGap);
            int typesWithStaleRunbook = summaries.Count(s => s.StaleRunbook);

            // Score de maturidade: média ponderada apenas para tipos com runbooks (ocorrências > 0)
            var typesWithRunbook = summaries.Where(s => s.TotalOccurrences > 0).ToList();
            double maturityScore = typesWithRunbook.Count > 0
                ? typesWithRunbook.Average(s =>
                    s.ResolutionConfidence * 0.5 + s.RunbookEffectivenessScore * 0.5)
                : 0.0;

            maturityScore = Math.Round(maturityScore, 4);

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                IncidentTypes: summaries,
                TopGaps: topGaps,
                TotalIncidentTypes: summaries.Count,
                TypesWithKnowledgeGap: typesWithGap,
                TypesWithStaleRunbook: typesWithStaleRunbook,
                KnowledgeMaturityScore: maturityScore,
                MaturityLevel: ClassifyMaturityLevel(maturityScore)));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static IncidentTypeKnowledgeSummary BuildSummary(IncidentTypeKnowledgeEntry entry)
        {
            double resolutionConfidence = entry.TotalOccurrences > 0
                ? Math.Round((double)entry.OccurrencesWithApprovedRunbook / entry.TotalOccurrences, 4)
                : 0.0;

            double runbookEffectiveness = entry.OccurrencesWithApprovedRunbook > 0
                ? Math.Round((double)entry.RunbookEffectiveResolutions / entry.OccurrencesWithApprovedRunbook, 4)
                : 0.0;

            bool knowledgeGap = entry.TotalOccurrences > KnowledgeGapMinOccurrences
                && !entry.HasApprovedRunbook;

            return new IncidentTypeKnowledgeSummary(
                IncidentType: entry.IncidentType,
                TotalOccurrences: entry.TotalOccurrences,
                ResolutionConfidence: resolutionConfidence,
                MeanTimeToRunbookMinutes: entry.AvgTimeToRunbookMinutes,
                RunbookEffectivenessScore: runbookEffectiveness,
                KnowledgeGap: knowledgeGap,
                StaleRunbook: entry.IsRunbookStale,
                IsTrendIncreasing: entry.IsTrendIncreasing);
        }

        private static KnowledgeMaturityLevel ClassifyMaturityLevel(double score) => score switch
        {
            >= ExemplaryThreshold => KnowledgeMaturityLevel.Exemplary,
            >= MatureThreshold => KnowledgeMaturityLevel.Mature,
            >= DevelopingThreshold => KnowledgeMaturityLevel.Developing,
            _ => KnowledgeMaturityLevel.Nascent
        };
    }
}
