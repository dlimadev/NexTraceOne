using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetEvidencePackIntegrityReport;

/// <summary>
/// Feature: GetEvidencePackIntegrityReport — integridade de Evidence Packs por tenant.
///
/// Por cada Evidence Pack avalia:
/// - <c>IntegrityStatus</c>   — Intact / Modified / Missing / Unverified
/// - <c>CoherenceStatus</c>   — Coherent / Incomplete / Inconsistent
/// - <c>SignatureStatus</c>   — Valid / Invalid / Missing / NotRequired
/// - <c>EvidencePackScore</c> — Integrity 40% + Coherence 35% + Signature 25%
///
/// <c>EvidencePackIntegrityTier</c>: Trustworthy / Acceptable / Questionable / Invalid
/// - <c>IntegrityAnomalies</c>                      — packs com hash divergente
/// - <c>ProductionReleasesWithInvalidEvidence</c>   — releases em produção com evidência inválida
///
/// Wave BC.2 — Production Change Confidence (ChangeGovernance Compliance).
/// </summary>
public static class GetEvidencePackIntegrityReport
{
    // ── Score thresholds ───────────────────────────────────────────────────
    internal const decimal TrustworthyThreshold = 90m;
    internal const decimal AcceptableThreshold = 70m;
    internal const decimal QuestionableThreshold = 50m;

    // ── Weights ────────────────────────────────────────────────────────────
    internal const decimal IntegrityWeight = 0.40m;
    internal const decimal CoherenceWeight = 0.35m;
    internal const decimal SignatureWeight = 0.25m;

    internal const int DefaultLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        bool ProductionOnly = false) : IQuery<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 180);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Estado de integridade do hash do Evidence Pack.</summary>
    public enum IntegrityStatus { Intact, Modified, Missing, Unverified }

    /// <summary>Estado de coerência interna do Evidence Pack.</summary>
    public enum CoherenceStatus { Coherent, Incomplete, Inconsistent }

    /// <summary>Estado da assinatura digital do Evidence Pack.</summary>
    public enum SignatureStatus { Valid, Invalid, Missing, NotRequired }

    /// <summary>Tier geral de integridade do tenant.</summary>
    public enum EvidencePackIntegrityTier
    {
        /// <summary>Score ≥ 90% — packs de evidência fiáveis.</summary>
        Trustworthy,
        /// <summary>Score ≥ 70% — aceitável, monitorização recomendada.</summary>
        Acceptable,
        /// <summary>Score ≥ 50% — questionável, revisão necessária.</summary>
        Questionable,
        /// <summary>Score &lt; 50% — inválido, auditoria urgente.</summary>
        Invalid
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Avaliação de integridade de um Evidence Pack.</summary>
    public sealed record EvidencePackIntegrityEntry(
        string EvidencePackId,
        string ReleaseId,
        string ServiceId,
        string ServiceName,
        bool IsProductionRelease,
        IntegrityStatus Integrity,
        CoherenceStatus Coherence,
        SignatureStatus Signature,
        decimal EvidencePackScore,
        EvidencePackIntegrityTier Tier,
        DateTimeOffset CreatedAt);

    /// <summary>Anomalia de integridade detectada.</summary>
    public sealed record IntegrityAnomaly(
        string EvidencePackId,
        string ReleaseId,
        string ServiceName,
        IntegrityStatus Status,
        string AnomalyDescription);

    /// <summary>Resultado do relatório de integridade de Evidence Packs.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        decimal TenantEvidencePackScore,
        EvidencePackIntegrityTier OverallTier,
        int TotalPacks,
        int IntactPacks,
        int ProductionReleasesWithInvalidEvidenceCount,
        IReadOnlyList<EvidencePackIntegrityEntry> Packs,
        IReadOnlyList<IntegrityAnomaly> IntegrityAnomalies);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IEvidencePackIntegrityReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);

            var packs = await reader.ListByTenantAsync(request.TenantId, from, cancellationToken);

            if (request.ProductionOnly)
                packs = packs.Where(p => p.IsProductionRelease).ToList();

            if (packs.Count == 0)
                return Result<Report>.Success(EmptyReport(now, request.TenantId, request.LookbackDays));

            var evaluated = packs
                .Select(p =>
                {
                    var integrity = ClassifyIntegrity(p);
                    var coherence = ClassifyCoherence(p);
                    var signature = ClassifySignature(p);
                    decimal score = ComputePackScore(integrity, coherence, signature);
                    return new EvidencePackIntegrityEntry(
                        EvidencePackId: p.EvidencePackId,
                        ReleaseId: p.ReleaseId,
                        ServiceId: p.ServiceId,
                        ServiceName: p.ServiceName,
                        IsProductionRelease: p.IsProductionRelease,
                        Integrity: integrity,
                        Coherence: coherence,
                        Signature: signature,
                        EvidencePackScore: score,
                        Tier: ClassifyTier(score),
                        CreatedAt: p.CreatedAt);
                })
                .ToList();

            var anomalies = evaluated
                .Where(e => e.Integrity is IntegrityStatus.Modified or IntegrityStatus.Missing)
                .Select(e => new IntegrityAnomaly(
                    EvidencePackId: e.EvidencePackId,
                    ReleaseId: e.ReleaseId,
                    ServiceName: e.ServiceName,
                    Status: e.Integrity,
                    AnomalyDescription: e.Integrity == IntegrityStatus.Modified
                        ? "Hash divergence detected — pack may have been tampered"
                        : "Evidence pack missing — no integrity baseline available"))
                .ToList();

            decimal tenantScore = Math.Round(evaluated.Average(e => e.EvidencePackScore), 1);
            int intactCount = evaluated.Count(e => e.Integrity == IntegrityStatus.Intact);
            int prodInvalid = evaluated.Count(e =>
                e.IsProductionRelease
                && e.Tier is EvidencePackIntegrityTier.Questionable or EvidencePackIntegrityTier.Invalid);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                LookbackDays: request.LookbackDays,
                TenantEvidencePackScore: tenantScore,
                OverallTier: ClassifyTier(tenantScore),
                TotalPacks: packs.Count,
                IntactPacks: intactCount,
                ProductionReleasesWithInvalidEvidenceCount: prodInvalid,
                Packs: evaluated,
                IntegrityAnomalies: anomalies));
        }

        private static IntegrityStatus ClassifyIntegrity(IEvidencePackIntegrityReader.EvidencePackEntry p)
        {
            if (!p.IsHashValid) return p.EvidenceItemCount == 0 ? IntegrityStatus.Missing : IntegrityStatus.Modified;
            if (p.EvidenceItemCount == 0) return IntegrityStatus.Missing;
            return IntegrityStatus.Intact;
        }

        private static CoherenceStatus ClassifyCoherence(IEvidencePackIntegrityReader.EvidencePackEntry p)
        {
            if (!p.IsComplete) return CoherenceStatus.Incomplete;
            if (!p.IsConsistent) return CoherenceStatus.Inconsistent;
            return CoherenceStatus.Coherent;
        }

        private static SignatureStatus ClassifySignature(IEvidencePackIntegrityReader.EvidencePackEntry p)
        {
            if (!p.HasSignature) return SignatureStatus.Missing;
            return p.IsSignatureValid ? SignatureStatus.Valid : SignatureStatus.Invalid;
        }

        private static decimal ComputePackScore(IntegrityStatus integrity, CoherenceStatus coherence, SignatureStatus sig)
        {
            decimal integrityScore = integrity switch
            {
                IntegrityStatus.Intact => 100m,
                IntegrityStatus.Unverified => 60m,
                IntegrityStatus.Modified => 20m,
                IntegrityStatus.Missing => 0m,
                _ => 0m
            };
            decimal coherenceScore = coherence switch
            {
                CoherenceStatus.Coherent => 100m,
                CoherenceStatus.Incomplete => 50m,
                CoherenceStatus.Inconsistent => 20m,
                _ => 0m
            };
            decimal signatureScore = sig switch
            {
                SignatureStatus.Valid => 100m,
                SignatureStatus.NotRequired => 100m,
                SignatureStatus.Missing => 50m,
                SignatureStatus.Invalid => 0m,
                _ => 0m
            };
            return Math.Round(
                integrityScore * IntegrityWeight
                + coherenceScore * CoherenceWeight
                + signatureScore * SignatureWeight, 1);
        }

        private static EvidencePackIntegrityTier ClassifyTier(decimal score) => score switch
        {
            _ when score >= TrustworthyThreshold => EvidencePackIntegrityTier.Trustworthy,
            _ when score >= AcceptableThreshold => EvidencePackIntegrityTier.Acceptable,
            _ when score >= QuestionableThreshold => EvidencePackIntegrityTier.Questionable,
            _ => EvidencePackIntegrityTier.Invalid
        };

        private static Report EmptyReport(DateTimeOffset now, string tenantId, int lookbackDays)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                LookbackDays: lookbackDays,
                TenantEvidencePackScore: 100m,
                OverallTier: EvidencePackIntegrityTier.Trustworthy,
                TotalPacks: 0,
                IntactPacks: 0,
                ProductionReleasesWithInvalidEvidenceCount: 0,
                Packs: [],
                IntegrityAnomalies: []);
    }
}
