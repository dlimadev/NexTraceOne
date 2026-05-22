using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Contracts;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ExecuteComplianceAudit;

/// <summary>
/// Feature: ExecuteComplianceAudit — executa auditoria de compliance, persiste gaps e publica eventos de integração.
/// Ao contrário de RunComplianceChecks (Query), este Command persiste os resultados e notifica os módulos consumidores.
/// Publica ComplianceGapsDetected quando há falhas e RiskReportGenerated ao concluir.
/// </summary>
public static class ExecuteComplianceAudit
{
    /// <summary>Comando de auditoria de compliance com filtros opcionais de escopo.</summary>
    public sealed record Command(
        string? ScopeId = null,
        string? TeamId = null,
        string? DomainId = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ScopeId).MaximumLength(200).When(x => x.ScopeId is not null);
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);
            RuleFor(x => x.DomainId).MaximumLength(200).When(x => x.DomainId is not null);
        }
    }

    public sealed class Handler(
        ITeamRepository teamRepo,
        IGovernanceDomainRepository domainRepo,
        IGovernancePackRepository packRepo,
        IGovernanceWaiverRepository waiverRepo,
        IComplianceGapRepository gapRepo,
        IGovernanceUnitOfWork unitOfWork,
        IEventBus eventBus) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var reportId = Guid.NewGuid().ToString("N");
            var failures = new List<(string ServiceId, string ServiceName, string Team, string Domain, string PolicyId, string Detail)>();

            // ── Check 1: Teams must be active ──────────────────────────────────────
            var teams = await teamRepo.ListAsync(null, cancellationToken);
            foreach (var team in teams)
            {
                if (!string.IsNullOrWhiteSpace(request.TeamId) &&
                    !team.Name.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (team.Status != TeamStatus.Active)
                    failures.Add((team.Name, team.DisplayName, team.Name,
                        team.ParentOrganizationUnit ?? "Unassigned",
                        "pol-team-active",
                        $"Team '{team.DisplayName}' is {team.Status} — must be Active."));
            }

            // ── Check 2: Domains must have criticality ≥ Medium ────────────────────
            var domains = await domainRepo.ListAsync(null, cancellationToken);
            foreach (var domain in domains)
            {
                if (!string.IsNullOrWhiteSpace(request.DomainId) &&
                    !domain.Name.Equals(request.DomainId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (domain.Criticality < DomainCriticality.Medium)
                    failures.Add((domain.Name, domain.DisplayName, "governance", domain.Name,
                        "pol-domain-criticality",
                        $"Domain '{domain.DisplayName}' has criticality {domain.Criticality} — Medium or higher required."));
            }

            // ── Check 3: At least one published governance pack ────────────────────
            var packs = await packRepo.ListAsync(null, null, cancellationToken);
            var publishedPacks = packs.Where(p => p.Status == GovernancePackStatus.Published).ToList();
            if (publishedPacks.Count == 0)
                failures.Add(("platform", "Platform", "governance", "Platform",
                    "pol-pack-published",
                    "No governance packs are published — governance coverage absent."));

            // ── Check 4: No excessive pending waivers ─────────────────────────────
            var waivers = await waiverRepo.ListAsync(null, WaiverStatus.Pending, cancellationToken);
            if (waivers.Count > 3)
                failures.Add(("platform", "Platform", "governance", "Platform",
                    "pol-waiver-review",
                    $"{waivers.Count} waiver(s) pending review — maximum of 3 allowed without action."));

            var totalChecks = teams.Count + domains.Count + 2; // packs + waivers checks
            var failedCount = failures.Count;
            var passedCount = totalChecks - failedCount;

            // ── Persist each failure as a ComplianceGap ────────────────────────────
            foreach (var f in failures)
            {
                var severity = f.PolicyId switch
                {
                    "pol-pack-published" => PolicySeverity.Critical,
                    "pol-waiver-review"  => PolicySeverity.High,
                    "pol-team-active"    => PolicySeverity.Medium,
                    _                    => PolicySeverity.Low
                };

                var gap = ComplianceGap.Create(
                    serviceId: f.ServiceId,
                    serviceName: f.ServiceName,
                    team: f.Team,
                    domain: f.Domain,
                    description: f.Detail,
                    severity: severity,
                    violatedPolicyIds: [f.PolicyId],
                    detectedAt: now);

                await gapRepo.AddAsync(gap, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            // ── Publish integration events ─────────────────────────────────────────
            if (failedCount > 0)
            {
                await eventBus.PublishAsync(new IntegrationEvents.ComplianceGapsDetected(
                    ReportId: reportId,
                    GapCount: failedCount,
                    DetectedAt: now,
                    ScopeId: request.ScopeId,
                    OwnerUserId: null), cancellationToken);
            }

            await eventBus.PublishAsync(new IntegrationEvents.RiskReportGenerated(
                ReportId: reportId,
                Scope: request.ScopeId ?? "platform",
                GeneratedAt: now,
                OwnerUserId: null), cancellationToken);

            return Result<Response>.Success(new Response(
                ReportId: reportId,
                TotalChecks: totalChecks,
                PassedCount: passedCount,
                FailedCount: failedCount,
                GapsPersisted: failedCount,
                ExecutedAt: now));
        }
    }

    /// <summary>Resposta da auditoria de compliance com resumo e referência ao relatório gerado.</summary>
    public sealed record Response(
        string ReportId,
        int TotalChecks,
        int PassedCount,
        int FailedCount,
        int GapsPersisted,
        DateTimeOffset ExecutedAt);
}
