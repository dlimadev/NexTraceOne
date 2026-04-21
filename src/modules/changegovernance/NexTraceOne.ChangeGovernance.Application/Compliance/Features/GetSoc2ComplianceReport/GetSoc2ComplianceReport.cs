using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetSoc2ComplianceReport;

/// <summary>
/// Feature: GetSoc2ComplianceReport — gera relatório de conformidade SOC 2 para auditores externos.
///
/// Controlos SOC 2 cobertos pelo NexTraceOne:
/// - CC6: Logical and Physical Access Controls (IdentityAccess — Access Reviews)
/// - CC7: System Operations (releases auditadas, change tracking, incident management)
/// - CC8: Change Management (releases + Evidence Pack assinados via Change Intelligence)
/// - CC9: Risk Mitigation (Risk Center, blast radius, vulnerability gates)
/// - A1:  Availability (DORA metrics, service reliability, profiling sessions)
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave G.1 — SOC 2 Compliance Report.
/// </summary>
public static class GetSoc2ComplianceReport
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

            // CC6 — Access Controls: requer integração com IdentityAccess
            const string cc6Note = "Access review and logical access control status requires integration with IdentityAccess module.";

            // CC7 — System Operations: releases auditadas indicam operações monitorizadas
            var cc7Status = filteredReleases.Count > 0 ? Nis2ControlStatus.Compliant : Nis2ControlStatus.NotAssessed;
            var cc7Note = $"{filteredReleases.Count} releases tracked with full operational audit trail in the last {request.Days} days.";

            // CC8 — Change Management: releases + Evidence Packs assinados
            var cc8Status = evidencePacks.Count == 0
                ? (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == evidencePacks.Count
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.NonCompliant;
            var cc8Note = filteredReleases.Count == 0
                ? "No changes recorded in the evaluation period."
                : $"{filteredReleases.Count} changes tracked; {signedCount}/{evidencePacks.Count} evidence packs signed and verifiable.";

            // CC9 — Risk Mitigation: requer integração com Risk Center e Catalog
            const string cc9Note = "Risk mitigation assessment (Risk Center, vulnerability gates) requires integration with Catalog and Risk Center modules.";

            // A1 — Availability: monitorização via DORA/reliability; MTTR requer integração OI
            var a1Status = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            const string a1Note = "Availability monitoring is tracked via service reliability metrics. Full DORA MTTR requires OperationalIntelligence integration.";

            var controls = new List<Soc2ControlResult>
            {
                new("CC6", "Logical and Physical Access Controls", Nis2ControlStatus.NotAssessed, cc6Note),
                new("CC7", "System Operations",                   cc7Status,                     cc7Note),
                new("CC8", "Change Management",                   cc8Status,                     cc8Note),
                new("CC9", "Risk Mitigation",                     Nis2ControlStatus.NotAssessed, cc9Note),
                new("A1",  "Availability",                        a1Status,                      a1Note),
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

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<Soc2ControlResult> controls)
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

    public sealed record Soc2ControlResult(
        string ControlId,
        string ControlName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<Soc2ControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
