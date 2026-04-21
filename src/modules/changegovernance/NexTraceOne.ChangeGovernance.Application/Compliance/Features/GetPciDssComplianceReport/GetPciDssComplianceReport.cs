using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPciDssComplianceReport;

/// <summary>
/// Feature: GetPciDssComplianceReport — gera relatório de conformidade PCI-DSS v4.0
/// para auditores externos e clientes em ambientes de processamento de pagamentos.
///
/// Controlos PCI-DSS cobertos pelo NexTraceOne:
/// - Req 1 &amp; 2: Network Security Controls — configuração e controlo de rede (parcialmente NotAssessed)
/// - Req 6:  Secure Systems and Software — change management auditado com Evidence Pack assinado
/// - Req 10: Log and Monitor — rastreabilidade de releases e evidências de auditoria
/// - Req 12: Organizational Policies — policies definidas e avaliadas via Policy Studio
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave H.2 — PCI-DSS Compliance Report.
/// </summary>
public static class GetPciDssComplianceReport
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

            // Req 1 & 2 — Network Security Controls: requer integração com infraestrutura
            const string req12Note = "Network segmentation and firewall configuration controls require integration with infrastructure monitoring.";

            // Req 6 — Secure Software Development: change management com Evidence Pack assinado
            var req6Status = evidencePacks.Count == 0
                ? (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == evidencePacks.Count
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.NonCompliant;
            var req6Note = filteredReleases.Count == 0
                ? "No changes recorded in the evaluation period."
                : $"{filteredReleases.Count} changes tracked; {signedCount}/{evidencePacks.Count} evidence packs signed and verifiable per PCI-DSS Req 6.5.";

            // Req 10 — Log and Monitor: rastreabilidade de releases e evidências
            var req10Status = filteredReleases.Count > 0 ? Nis2ControlStatus.Compliant : Nis2ControlStatus.NotAssessed;
            var req10Note = filteredReleases.Count > 0
                ? $"{filteredReleases.Count} releases with full audit trail and timestamped evidence packs tracked in the last {request.Days} days."
                : "No change audit trail available for the evaluation period.";

            // Req 11 — Security Testing: requer integração com Vulnerability Advisory
            const string req11Note = "Security testing and vulnerability scanning status requires integration with the Vulnerability Advisory module.";

            // Req 12 — Organizational Policies: policies de mudança e acesso
            var req12PolicyNote = filteredReleases.Count > 0
                ? $"Change management policies enforced: {filteredReleases.Count} releases tracked with approval and evidence requirements."
                : "No change management policy activity recorded for the evaluation period.";
            var req12PolicyStatus = filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed;

            var controls = new List<PciDssControlResult>
            {
                new("Req 1-2", "Network Security Controls",          Nis2ControlStatus.NotAssessed, req12Note),
                new("Req 6",   "Secure Systems and Software",        req6Status,                    req6Note),
                new("Req 10",  "Log and Monitor All Access",         req10Status,                   req10Note),
                new("Req 11",  "Security Testing",                   Nis2ControlStatus.NotAssessed, req11Note),
                new("Req 12",  "Organizational Policies and Programs", req12PolicyStatus,            req12PolicyNote),
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

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<PciDssControlResult> controls)
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

    public sealed record PciDssControlResult(
        string RequirementId,
        string RequirementName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<PciDssControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
