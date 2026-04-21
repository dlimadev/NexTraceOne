using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetGdprComplianceReport;

/// <summary>
/// Feature: GetGdprComplianceReport — gera relatório de conformidade GDPR
/// para clientes europeus ou que tratem dados pessoais de cidadãos europeus.
///
/// Artigos GDPR cobertos pelo NexTraceOne:
/// - Art. 5:  Princípios relativos ao tratamento de dados pessoais (accountability, integridade)
/// - Art. 13: Informação a fornecer quando os dados são recolhidos (transparência)
/// - Art. 17: Direito ao apagamento ("direito a ser esquecido")
/// - Art. 25: Proteção de dados por design e por defeito (Privacy by Design)
/// - Art. 33: Notificação de violação de dados pessoais à autoridade de controlo
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave J.1 — GDPR Compliance Report.
/// </summary>
public static class GetGdprComplianceReport
{
    public sealed record Query(
        int Days = 90,
        string? ServiceName = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IEvidencePackRepository evidencePackRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var releases = await releaseRepository.ListInRangeAsync(since, now, null, currentTenant.Id, cancellationToken);
            var filteredReleases = request.ServiceName is not null
                ? releases.Where(r => r.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase)).ToList()
                : releases.ToList();

            var releaseIds = filteredReleases.Select(r => r.Id.Value);
            var evidencePacks = await evidencePackRepository.ListByReleaseIdsAsync(releaseIds, cancellationToken);
            var signedCount = evidencePacks.Count(e => e.IsIntegritySigned);

            // Art. 5 — Princípios: rastreabilidade completa como proxy de accountability/integridade
            var art5Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var art5Note = filteredReleases.Count > 0
                ? $"{filteredReleases.Count} releases with full audit trail recorded in the last {request.Days} days. " +
                  "Accountability principle supported via immutable change history. Lawfulness and data minimisation " +
                  "require integration with data processing records outside change governance scope."
                : "No change activity recorded. Accountability principle cannot be assessed for this period.";

            // Art. 13 — Transparência: evidence pack como proxy de documentação de tratamento
            var art13Status = evidencePacks.Count > 0 || filteredReleases.Count > 0
                ? Nis2ControlStatus.PartiallyCompliant
                : Nis2ControlStatus.NotAssessed;
            var art13Note = filteredReleases.Count > 0
                ? $"{evidencePacks.Count} evidence packs found for recorded changes. " +
                  "Transparency obligations (Art. 13) require integration with data subject notice records and processing registers."
                : "No transparency-related change activity recorded for this period.";

            // Art. 17 — Direito ao apagamento: requer integração com sistema de dados pessoais
            const string art17Note =
                "Right to erasure (Art. 17) cannot be assessed from change governance data alone. " +
                "Integration with personal data processing systems and erasure workflows is required for full assessment.";

            // Art. 25 — Privacy by Design: evidence pack assinado = design intencional de integridade
            var art25Status = evidencePacks.Count == 0
                ? (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == evidencePacks.Count
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.NonCompliant;
            var art25Note = filteredReleases.Count == 0
                ? "No privacy-by-design signals recorded in the evaluation period."
                : $"{signedCount}/{evidencePacks.Count} evidence packs cryptographically signed — demonstrating intentional integrity controls " +
                  "per Art. 25 (privacy by design). Remaining controls (data minimisation, pseudonymisation) require integration with data architecture.";

            // Art. 33 — Breach Notification: rastreabilidade de changes como proxy de preparação
            var art33Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var art33Note = filteredReleases.Count > 0
                ? "Change traceability supports breach investigation workflows. Full Art. 33 compliance " +
                  "(72-hour notification, supervisory authority contact) requires integration with incident response tooling."
                : "Insufficient change activity to assess breach notification readiness for this period.";

            var controls = new List<GdprControlResult>
            {
                new("Art. 5",  "Principles of Processing",       art5Status,                    art5Note),
                new("Art. 13", "Transparency",                   art13Status,                   art13Note),
                new("Art. 17", "Right to Erasure",               Nis2ControlStatus.NotAssessed, art17Note),
                new("Art. 25", "Privacy by Design",              art25Status,                   art25Note),
                new("Art. 33", "Breach Notification Readiness",  art33Status,                   art33Note),
            };

            var overallStatus = DetermineOverall(controls);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                ServiceFilter: request.ServiceName,
                OverallStatus: overallStatus,
                Controls: controls,
                TotalReleases: filteredReleases.Count,
                SignedEvidencePacks: signedCount,
                TotalEvidencePacks: evidencePacks.Count));
        }

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<GdprControlResult> controls)
        {
            if (controls.Any(c => c.Status == Nis2ControlStatus.NonCompliant))
                return Nis2ControlStatus.NonCompliant;
            if (controls.Any(c => c.Status == Nis2ControlStatus.PartiallyCompliant))
                return Nis2ControlStatus.PartiallyCompliant;
            if (controls.All(c => c.Status == Nis2ControlStatus.NotAssessed))
                return Nis2ControlStatus.NotAssessed;
            return Nis2ControlStatus.Compliant;
        }
    }

    public sealed record GdprControlResult(
        string ArticleId,
        string ControlName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<GdprControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
