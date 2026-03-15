using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetComplianceGaps;

/// <summary>
/// Feature: GetComplianceGaps — gaps de compliance agrupados por dimensão.
/// Permite filtragem por equipa, domínio ou serviço.
/// </summary>
public static class GetComplianceGaps
{
    /// <summary>Query para gaps de compliance.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null) : IQuery<Response>;

    /// <summary>Handler que retorna gaps de compliance agrupados.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var gaps = new List<ComplianceGapDto>
            {
                new("gap-001", "svc-legacy-adapter", "Legacy Adapter", "integration-squad", "Integration",
                    "No owner, no contract, no runbook", PolicySeverity.Critical,
                    new[] { "pol-001", "pol-002", "pol-003" }, 3, DateTimeOffset.UtcNow.AddDays(-30)),
                new("gap-002", "svc-batch-processor", "Batch Processor", "operations-squad", "Operations",
                    "Owner unassigned, missing runbook", PolicySeverity.High,
                    new[] { "pol-001", "pol-003" }, 2, DateTimeOffset.UtcNow.AddDays(-15)),
                new("gap-003", "svc-catalog-sync", "Catalog Sync", "platform-squad", "Platform",
                    "No semantic versioning, outdated publication", PolicySeverity.Medium,
                    new[] { "pol-009", "pol-006" }, 2, DateTimeOffset.UtcNow.AddDays(-10)),
                new("gap-004", "svc-notification-worker", "Notification Worker", "platform-squad", "Platform",
                    "Dependencies not mapped, documentation incomplete", PolicySeverity.Medium,
                    new[] { "pol-007", "pol-006" }, 2, DateTimeOffset.UtcNow.AddDays(-8)),
                new("gap-005", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    "Missing contract and runbook for critical service", PolicySeverity.Critical,
                    new[] { "pol-002", "pol-003" }, 2, DateTimeOffset.UtcNow.AddDays(-5)),
                new("gap-006", "svc-chat-service", "Chat Service", "platform-squad", "Platform",
                    "External AI integration without complete audit trail", PolicySeverity.High,
                    new[] { "pol-005" }, 1, DateTimeOffset.UtcNow.AddDays(-3))
            };

            var response = new Response(
                TotalGaps: gaps.Count,
                CriticalCount: gaps.Count(g => g.Severity == PolicySeverity.Critical),
                HighCount: gaps.Count(g => g.Severity == PolicySeverity.High),
                MediumCount: gaps.Count(g => g.Severity == PolicySeverity.Medium),
                LowCount: gaps.Count(g => g.Severity == PolicySeverity.Low),
                Gaps: gaps,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com gaps de compliance.</summary>
    public sealed record Response(
        int TotalGaps,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        IReadOnlyList<ComplianceGapDto> Gaps,
        DateTimeOffset GeneratedAt);

    /// <summary>DTO de gap de compliance.</summary>
    public sealed record ComplianceGapDto(
        string GapId,
        string ServiceId,
        string ServiceName,
        string Team,
        string Domain,
        string Description,
        PolicySeverity Severity,
        string[] ViolatedPolicyIds,
        int ViolationCount,
        DateTimeOffset DetectedAt);
}
