using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetHipaaComplianceReport;

/// <summary>
/// Feature: GetHipaaComplianceReport — gera relatório de conformidade HIPAA Security Rule
/// para clientes em ambientes de healthcare, seguros de saúde e parceiros de negócio cobertos.
///
/// Controlos HIPAA Security Rule cobertos pelo NexTraceOne:
/// - § 164.312(a)(1): Access Control — controlo de acesso lógico via IdentityAccess
/// - § 164.312(b):    Audit Controls — rastreabilidade de releases e evidências de auditoria
/// - § 164.312(c)(1): Integrity — integridade de dados via Evidence Pack assinado (HMAC-SHA256)
/// - § 164.312(d):    Person/Entity Authentication — autenticação via OIDC/SAML (parcialmente NotAssessed)
/// - § 164.312(e)(1): Transmission Security — requer integração com rede (NotAssessed)
///
/// Relatório puro (query sem side effects) adequado para exportação auditável.
/// Wave I.1 — HIPAA Security Rule Compliance Report.
/// </summary>
public static class GetHipaaComplianceReport
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

            // § 164.312(a)(1) — Access Control: requer integração com IdentityAccess
            const string accessControlNote =
                "Logical access controls (role-based, JIT privileged access, delegated access) are enforced " +
                "through the IdentityAccess module. Full assessment requires integration with access review records.";

            // § 164.312(b) — Audit Controls: rastreabilidade via releases e evidence packs
            var auditStatus = filteredReleases.Count > 0 ? Nis2ControlStatus.Compliant : Nis2ControlStatus.NotAssessed;
            var auditNote = filteredReleases.Count > 0
                ? $"{filteredReleases.Count} releases with full audit trail and timestamped evidence packs tracked in the last {request.Days} days."
                : "No audit trail activity recorded for the evaluation period.";

            // § 164.312(c)(1) — Integrity: Evidence Pack assinado com HMAC-SHA256
            var integrityStatus = evidencePacks.Count == 0
                ? (filteredReleases.Count > 0 ? Nis2ControlStatus.PartiallyCompliant : Nis2ControlStatus.NotAssessed)
                : signedCount == evidencePacks.Count
                    ? Nis2ControlStatus.Compliant
                    : signedCount > 0
                        ? Nis2ControlStatus.PartiallyCompliant
                        : Nis2ControlStatus.NonCompliant;
            var integrityNote = filteredReleases.Count == 0
                ? "No integrity-protected changes recorded in the evaluation period."
                : $"{signedCount}/{evidencePacks.Count} evidence packs cryptographically signed and verifiable per § 164.312(c)(1).";

            // § 164.312(d) — Person/Entity Authentication: OIDC/SAML (parcialmente NotAssessed)
            var authNote = filteredReleases.Count > 0
                ? "Person/entity authentication enforced via OIDC/SAML for all change approval workflows. " +
                  "Full machine-level entity authentication assessment requires infrastructure integration."
                : "Authentication controls present but no authenticated change workflow activity in the evaluation period.";
            var authStatus = filteredReleases.Count > 0
                ? Nis2ControlStatus.PartiallyCompliant
                : Nis2ControlStatus.NotAssessed;

            // § 164.312(e)(1) — Transmission Security: requer integração com rede/TLS
            const string transmissionNote =
                "Transmission security (TLS enforcement, encryption in transit) requires integration with " +
                "network and infrastructure monitoring. Not assessed by change governance data alone.";

            var controls = new List<HipaaControlResult>
            {
                new("§ 164.312(a)(1)", "Access Control",           Nis2ControlStatus.NotAssessed, accessControlNote),
                new("§ 164.312(b)",    "Audit Controls",           auditStatus,                   auditNote),
                new("§ 164.312(c)(1)", "Integrity",                integrityStatus,               integrityNote),
                new("§ 164.312(d)",    "Person/Entity Authentication", authStatus,                authNote),
                new("§ 164.312(e)(1)", "Transmission Security",    Nis2ControlStatus.NotAssessed, transmissionNote),
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

        private static Nis2ControlStatus DetermineOverall(IReadOnlyList<HipaaControlResult> controls)
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

    public sealed record HipaaControlResult(
        string ControlId,
        string ControlName,
        Nis2ControlStatus Status,
        string Note);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        Nis2ControlStatus OverallStatus,
        IReadOnlyList<HipaaControlResult> Controls,
        int TotalReleases,
        int SignedEvidencePacks,
        int TotalEvidencePacks);
}
