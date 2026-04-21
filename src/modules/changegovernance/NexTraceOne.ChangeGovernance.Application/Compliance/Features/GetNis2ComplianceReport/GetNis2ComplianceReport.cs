using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetNis2ComplianceReport;

/// <summary>
/// Feature: GetNis2ComplianceReport — gera relatório de conformidade NIS2 para auditores externos.
///
/// Os controlos NIS2 relevantes cobertos por NexTraceOne:
/// - RCM-1: Gestão de risco (releases rastreadas no período)
/// - RCM-2: Integridade de evidência de mudanças (Evidence Pack assinado via Wave C.2)
/// - RCM-3: Gestão de vulnerabilidades (gate externo — NotAssessed por padrão)
/// - RCM-4: Controlo de acesso e revisão periódica (módulo externo — NotAssessed por padrão)
/// - RCM-5: Rastreabilidade de mudanças em produção (Release audit trail)
///
/// O relatório é puro (query sem side effects) e adequado para exportação auditável.
/// Wave C.2 backlog — NIS2 Compliance Report.
/// </summary>
public static class GetNis2ComplianceReport
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

            // ── RCM-1/RCM-5: Releases no período, filtradas por tenant ──────────
            var releases = await releaseRepository.ListInRangeAsync(since, now, null, currentTenant.Id, cancellationToken);
            var filteredReleases = request.ServiceName is not null
                ? releases.Where(r => r.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase)).ToList()
                : releases.ToList();

            var rcm1Status = filteredReleases.Count > 0 ? Nis2ControlStatus.Compliant : Nis2ControlStatus.NotAssessed;
            var rcm1Note = $"{filteredReleases.Count} releases tracked in the last {request.Days} days.";

            // ── RCM-2: Integridade de evidência — Evidence Packs assinados ────────
            var releaseIds = filteredReleases.Select(r => r.Id.Value);
            var evidencePacks = await evidencePackRepository.ListByReleaseIdsAsync(releaseIds, cancellationToken);

            var signedCount = evidencePacks.Count(e => e.IsIntegritySigned);
            var rcm2Status = evidencePacks.Count == 0
                ? Nis2ControlStatus.NotAssessed
                : signedCount == evidencePacks.Count
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.NonCompliant;
            var rcm2Note = $"{signedCount}/{evidencePacks.Count} evidence packs signed.";

            // ── RCM-3: Vulnerability Management (requer integração com Catalog) ───
            var rcm3Note = "Vulnerability gate assessment requires integration with Catalog module.";

            // ── RCM-4: Access Control Review (requer integração com IdentityAccess) ─
            var rcm4Note = "Access review status requires integration with IdentityAccess module.";

            // ── RCM-5: Change Traceability ─────────────────────────────────────────
            var rcm5Status = filteredReleases.Count > 0 ? Nis2ControlStatus.Compliant : Nis2ControlStatus.NotAssessed;
            var rcm5Note = $"Release audit trail: {filteredReleases.Count} releases with full change record.";

            var controls = new List<Nis2ControlResult>
            {
                new("RCM-1", "Risk Change Management", rcm1Status, rcm1Note),
                new("RCM-2", "Evidence Integrity", rcm2Status, rcm2Note),
                new("RCM-3", "Vulnerability Management", Nis2ControlStatus.NotAssessed, rcm3Note),
                new("RCM-4", "Access Control Review", Nis2ControlStatus.NotAssessed, rcm4Note),
                new("RCM-5", "Change Traceability", rcm5Status, rcm5Note),
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

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<Nis2ControlResult> controls)
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

    public sealed record Nis2ControlResult(
        string ControlId,
        string ControlName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<Nis2ControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
