using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCmmcComplianceReport;

/// <summary>
/// Feature: GetCmmcComplianceReport — gera relatório de conformidade CMMC 2.0 Level 2
/// para clientes em ambiente de Contratação Federal dos EUA (Controlled Unclassified Information).
///
/// Práticas CMMC 2.0 Level 2 cobertas pelo NexTraceOne:
/// - AC.1.001: Limit access to authorized users (Access Control)
/// - IA.1.076: Identify information system users and devices (Identification &amp; Authentication)
/// - AU.2.041: Create and retain system audit logs (Audit &amp; Accountability)
/// - IR.2.092: Establish incident-handling capability (Incident Response)
/// - RM.2.141: Periodically assess organizational risk (Risk Management)
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave K.2 — CMMC 2.0 Compliance Report.
/// </summary>
public static class GetCmmcComplianceReport
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
            var totalPacks = evidencePacks.Count;

            // AC.1.001 — Access Control: Limit access to authorized users
            // Proxy: presence of releases with approval workflow indicates access-controlled deployments
            var ac1Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var ac1Note = filteredReleases.Count > 0
                ? $"{filteredReleases.Count} controlled releases recorded in the last {request.Days} days. " +
                  "Deployment access control is partially evidenced via change governance workflow. " +
                  "Full AC.1.001 assessment requires integration with identity and access management records."
                : "No change activity recorded. Access control posture cannot be assessed for this period.";

            // IA.1.076 — Identification and Authentication: Identify users, processes and devices
            // Proxy: evidence packs with audit trail demonstrate authenticated change actors
            var ia1Status = totalPacks > 0
                ? Nis2ControlStatus.PartiallyCompliant
                : (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed);
            var ia1Note = filteredReleases.Count > 0
                ? $"{totalPacks} evidence packs with audit trail attest change actors. " +
                  "Full IA.1.076 compliance requires integration with authentication system logs and MFA controls."
                : "No identification-related change activity recorded for this period.";

            // AU.2.041 — Audit and Accountability: Create and retain audit logs
            // Proxy: evidence packs (cryptographically signed) as primary audit artifacts
            var au2Status = totalPacks == 0
                ? (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == totalPacks
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.PartiallyCompliant;
            var au2Note = totalPacks > 0
                ? $"{signedCount}/{totalPacks} evidence packs cryptographically signed (HMAC-SHA256). " +
                  "Immutable audit trail for all recorded changes satisfies AU.2.041 change audit requirements. " +
                  "Extend to system/network logs for full AU.2.041 scope."
                : filteredReleases.Count > 0
                    ? "Releases recorded without signed evidence packs. Enable evidence pack signing for full AU.2.041 compliance."
                    : "No audit trail recorded in the evaluation period.";

            // IR.2.092 — Incident Response: Establish incident-handling capability
            // Proxy: release traceability supports incident investigation; requires IR tooling integration
            var ir2Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var ir2Note = filteredReleases.Count > 0
                ? "Change traceability and evidence packs support incident investigation and recovery workflows. " +
                  "Full IR.2.092 assessment (preparation, containment, recovery procedures) requires integration with " +
                  "incident response tooling and documented IR plans."
                : "No incident-relevant change activity recorded for this period.";

            // RM.2.141 — Risk Management: Periodically assess organizational risk
            // Proxy: Risk Center profiles + change governance demonstrate active risk assessment posture
            const string rm2Note =
                "Risk assessment posture is partially supported via Risk Center risk profiles, blast radius analysis, " +
                "and vulnerability gate evaluations per change. Full RM.2.141 compliance requires formal risk assessment " +
                "documentation, CUI boundary mapping, and periodic risk review records.";
            var rm2Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;

            var controls = new List<CmmcControlResult>
            {
                new("AC.1.001", "Access Control",              "Limit Access to Authorized Users",       ac1Status, ac1Note),
                new("IA.1.076", "Identification & Auth.",      "Identify Information System Users",       ia1Status, ia1Note),
                new("AU.2.041", "Audit & Accountability",     "Create and Retain Audit Logs",            au2Status, au2Note),
                new("IR.2.092", "Incident Response",          "Establish Incident-Handling Capability",  ir2Status, ir2Note),
                new("RM.2.141", "Risk Management",            "Periodically Assess Organizational Risk", rm2Status, rm2Note),
            };

            var overallStatus = DetermineOverall(controls);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                ServiceFilter: request.ServiceName,
                CmmcLevel: 2,
                OverallStatus: overallStatus,
                Controls: controls,
                TotalReleases: filteredReleases.Count,
                SignedEvidencePacks: signedCount,
                TotalEvidencePacks: totalPacks));
        }

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<CmmcControlResult> controls)
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

    public sealed record CmmcControlResult(
        string PracticeId,
        string Domain,
        string PracticeName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        int CmmcLevel,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<CmmcControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
