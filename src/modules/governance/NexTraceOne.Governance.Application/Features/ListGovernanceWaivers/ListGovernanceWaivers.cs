using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListGovernanceWaivers;

/// <summary>
/// Feature: ListGovernanceWaivers — catálogo de waivers de governança.
/// Retorna pedidos de exceção com estado de aprovação, justificação e metadados.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class ListGovernanceWaivers
{
    /// <summary>Query para listar waivers de governança. Permite filtragem por pack e status.</summary>
    public sealed record Query(
        string? PackId = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo de waivers de governança.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var waivers = new List<WaiverDto>
            {
                new("waiver-001", "pack-001", "Contracts Baseline", "rule-003", "CONTRACT-EXAMPLES-PRESENT",
                    "payment-api", "Service", "Legacy service migration in progress — examples to be added in Q2",
                    "Approved", "engineer@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-30),
                    "architect@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-28),
                    DateTimeOffset.UtcNow.AddDays(60)),
                new("waiver-002", "pack-003", "Change Governance", "rule-010", "BLAST-RADIUS-ASSESSMENT",
                    "notification-hub", "Service", "Hotfix deployment — blast radius assessment deferred",
                    "Approved", "techlead@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-15),
                    "admin@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-14),
                    DateTimeOffset.UtcNow.AddDays(7)),
                new("waiver-003", "pack-001", "Contracts Baseline", "rule-002", "CONTRACT-VERSION-SEMVER",
                    "billing-soap-v3", "Contract", "SOAP contract uses legacy versioning scheme — migration planned",
                    "Pending", "engineer@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-5),
                    null, null,
                    DateTimeOffset.UtcNow.AddDays(90)),
                new("waiver-004", "pack-004", "AI Usage Policy", "rule-020", "AI-MODEL-APPROVED",
                    "data-engineering", "Team", "Testing unapproved model in sandbox for evaluation purposes",
                    "Rejected", "datascientist@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-10),
                    "admin@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-8),
                    null),
                new("waiver-005", "pack-005", "Operational Readiness", "rule-015", "RUNBOOK-PRESENT",
                    "analytics-pipeline", "Service", "New service — runbook in progress, expected completion next sprint",
                    "Pending", "engineer@nextraceone.io", DateTimeOffset.UtcNow.AddDays(-3),
                    null, null,
                    DateTimeOffset.UtcNow.AddDays(30))
            };

            IEnumerable<WaiverDto> filtered = waivers;

            if (!string.IsNullOrEmpty(request.PackId))
                filtered = filtered.Where(w => w.PackId == request.PackId);

            if (!string.IsNullOrEmpty(request.Status))
                filtered = filtered.Where(w =>
                    string.Equals(w.Status, request.Status, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();

            var response = new Response(
                TotalWaivers: waivers.Count,
                PendingCount: waivers.Count(w => w.Status == "Pending"),
                ApprovedCount: waivers.Count(w => w.Status == "Approved"),
                Waivers: list);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de waivers de governança.</summary>
    public sealed record Response(
        int TotalWaivers,
        int PendingCount,
        int ApprovedCount,
        IReadOnlyList<WaiverDto> Waivers);

    /// <summary>DTO de um waiver de governança.</summary>
    public sealed record WaiverDto(
        string WaiverId,
        string PackId,
        string PackName,
        string RuleId,
        string RuleName,
        string Scope,
        string ScopeType,
        string Justification,
        string Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        string? ReviewedBy,
        DateTimeOffset? ReviewedAt,
        DateTimeOffset? ExpiresAt);
}
