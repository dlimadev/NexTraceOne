using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetFedRampComplianceReport;

/// <summary>
/// Feature: GetFedRampComplianceReport — gera relatório de conformidade FedRAMP Moderate
/// para organizações sujeitas ao Federal Risk and Authorization Management Program (EUA).
///
/// Controlos FedRAMP Moderate (NIST SP 800-53 Rev 5) cobertos pelo NexTraceOne:
/// - AC-2: Account Management — controlo de acessos e identidades
/// - AU-2: Event Logging — registo de eventos auditáveis de mudança
/// - CM-6: Configuration Settings — mudanças de configuração rastreadas
/// - IR-4: Incident Handling — capacidade de resposta a incidentes via traceabilidade
/// - SI-2: Flaw Remediation — vulnerabilidades tratadas e rastreadas por release
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave L.2 — FedRAMP Moderate Compliance Report.
/// </summary>
public static class GetFedRampComplianceReport
{
    public sealed record Query(
        int Days = 90,
        string? ServiceName = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 365);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
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

            var releaseCount = filteredReleases.Count;

            // AC-2: Account Management
            // Proxy: existence of governed releases indicates controlled access to deployment workflows
            var ac2Status = releaseCount > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var ac2Note = releaseCount > 0
                ? $"{releaseCount} governed releases recorded in the last {request.Days} days. " +
                  "Deployment workflow access indicates partial account management controls. " +
                  "Full AC-2 compliance requires integration with identity provider and account lifecycle records."
                : "No deployment activity detected. Account management posture cannot be assessed for this period.";

            // AU-2: Event Logging
            // Proxy: evidence packs are the primary audit event artifacts for all change activities
            var au2Status = totalPacks == 0
                ? (releaseCount > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == totalPacks
                    ? Nis2ControlStatus.Compliant
                    : Nis2ControlStatus.PartiallyCompliant;
            var au2Note = totalPacks > 0
                ? $"{signedCount}/{totalPacks} evidence packs cryptographically signed (HMAC-SHA256). " +
                  "Change events are logged and tamper-evidenced per AU-2 requirements for organizational information systems. " +
                  "Extend to OS-level and network event logs for full AU-2 scope."
                : releaseCount > 0
                    ? "Releases recorded without signed evidence packs. Enable evidence pack signing for stronger AU-2 compliance."
                    : "No event logging artifacts recorded in the evaluation period.";

            // CM-6: Configuration Settings
            // Proxy: releases represent discrete configuration changes with version tracking
            var cm6Status = releaseCount > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var cm6Note = releaseCount > 0
                ? $"{releaseCount} versioned releases with configuration change tracking. " +
                  "Configuration change governance via release identity, blast radius analysis, and evidence packs " +
                  "partially satisfies CM-6. Full compliance requires integration with CM tooling and configuration baseline records."
                : "No configuration change activity detected for this period.";

            // IR-4: Incident Handling
            // Proxy: release traceability enables change-to-incident correlation for incident investigation
            var ir4Status = releaseCount > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var ir4Note = releaseCount > 0
                ? "Change-to-incident correlation, blast radius mapping, and rollback intelligence " +
                  "support IR-4 incident handling capabilities. Full IR-4 compliance requires documented " +
                  "incident response plans, tabletop exercises, and integration with incident management systems."
                : "No incident-relevant change activity recorded for this period.";

            // SI-2: Flaw Remediation
            // Proxy: vulnerability gate in promotion workflow ensures flaws are addressed before deployment
            var si2Status = releaseCount > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;
            var si2Note = releaseCount > 0
                ? "Vulnerability promotion gates and advisory tracking support flaw identification and remediation " +
                  "evidence per SI-2. Full compliance requires formal patch management SLAs, " +
                  "CVE tracking integration, and remediation verification workflows."
                : "No flaw remediation evidence recorded in the evaluation period.";

            var controls = new List<FedRampControlResult>
            {
                new("AC-2",  "Access Control",         "Account Management",      ac2Status, ac2Note),
                new("AU-2",  "Audit and Accountability","Event Logging",           au2Status, au2Note),
                new("CM-6",  "Configuration Management","Configuration Settings",  cm6Status, cm6Note),
                new("IR-4",  "Incident Response",       "Incident Handling",       ir4Status, ir4Note),
                new("SI-2",  "System and Information Integrity", "Flaw Remediation", si2Status, si2Note),
            };

            var overallStatus = DetermineOverall(controls);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                ServiceFilter: request.ServiceName,
                ImpactLevel: "Moderate",
                OverallStatus: overallStatus,
                Controls: controls,
                TotalReleases: releaseCount,
                SignedEvidencePacks: signedCount,
                TotalEvidencePacks: totalPacks));
        }

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<FedRampControlResult> controls)
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

    public sealed record FedRampControlResult(
        string ControlId,
        string ControlFamily,
        string ControlName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        string ImpactLevel,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<FedRampControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
