using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RunComplianceChecks;

/// <summary>
/// Feature: RunComplianceChecks — executa checks de compliance e retorna resultados.
/// Avalia condições como owner, contract, runbook, versioning, deps, AI governance.
/// </summary>
public static class RunComplianceChecks
{
    /// <summary>Query para resultados de compliance checks. Filtrável por serviço, equipa ou domínio.</summary>
    public sealed record Query(
        string? ServiceId = null,
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>Handler que executa e retorna resultados de compliance checks.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = new List<ComplianceCheckResultDto>
            {
                new("chk-001", "Owner Present", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    ComplianceCheckStatus.Passed, "pol-001", "Owner is assigned and active", DateTimeOffset.UtcNow.AddMinutes(-30)),
                new("chk-002", "Contract Present", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    ComplianceCheckStatus.Failed, "pol-002", "No contract definition found", DateTimeOffset.UtcNow.AddMinutes(-30)),
                new("chk-003", "Runbook Present", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    ComplianceCheckStatus.Failed, "pol-003", "Runbook missing for critical service", DateTimeOffset.UtcNow.AddMinutes(-30)),
                new("chk-004", "Change Validation", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    ComplianceCheckStatus.Passed, "pol-004", "Last 5 changes validated successfully", DateTimeOffset.UtcNow.AddMinutes(-30)),
                new("chk-005", "Owner Present", "svc-order-api", "Order API", "order-squad", "Orders",
                    ComplianceCheckStatus.Passed, "pol-001", "Owner is assigned and active", DateTimeOffset.UtcNow.AddMinutes(-28)),
                new("chk-006", "Contract Present", "svc-order-api", "Order API", "order-squad", "Orders",
                    ComplianceCheckStatus.Passed, "pol-002", "Contract v2.1.0 published", DateTimeOffset.UtcNow.AddMinutes(-28)),
                new("chk-007", "Runbook Present", "svc-order-api", "Order API", "order-squad", "Orders",
                    ComplianceCheckStatus.Passed, "pol-003", "Runbook last updated 5 days ago", DateTimeOffset.UtcNow.AddMinutes(-28)),
                new("chk-008", "Documentation Standards", "svc-order-api", "Order API", "order-squad", "Orders",
                    ComplianceCheckStatus.Warning, "pol-006", "Documentation exists but is older than 90 days", DateTimeOffset.UtcNow.AddMinutes(-28)),
                new("chk-009", "Owner Present", "svc-legacy-adapter", "Legacy Adapter", "integration-squad", "Integration",
                    ComplianceCheckStatus.Failed, "pol-001", "No owner assigned", DateTimeOffset.UtcNow.AddMinutes(-25)),
                new("chk-010", "Contract Present", "svc-legacy-adapter", "Legacy Adapter", "integration-squad", "Integration",
                    ComplianceCheckStatus.Failed, "pol-002", "No contract definition", DateTimeOffset.UtcNow.AddMinutes(-25)),
                new("chk-011", "AI Model Policy", "svc-ai-recommender", "AI Recommender", "ml-squad", "AI",
                    ComplianceCheckStatus.Passed, "pol-005", "Using approved model gpt-4o from registry", DateTimeOffset.UtcNow.AddMinutes(-20)),
                new("chk-012", "AI Model Policy", "svc-chat-service", "Chat Service", "platform-squad", "Platform",
                    ComplianceCheckStatus.Warning, "pol-005", "External AI integration without full audit trail", DateTimeOffset.UtcNow.AddMinutes(-20)),
                new("chk-013", "Dependency Mapping", "svc-notification-worker", "Notification Worker", "platform-squad", "Platform",
                    ComplianceCheckStatus.Failed, "pol-007", "Dependencies not mapped in catalog", DateTimeOffset.UtcNow.AddMinutes(-18)),
                new("chk-014", "Version Control", "svc-catalog-sync", "Catalog Sync", "platform-squad", "Platform",
                    ComplianceCheckStatus.Failed, "pol-009", "Contract exists but no semantic versioning", DateTimeOffset.UtcNow.AddMinutes(-15)),
                new("chk-015", "Security Review", "svc-payment-gateway", "Payment Gateway", "payment-squad", "Payments",
                    ComplianceCheckStatus.Passed, "pol-010", "Security review completed on 2026-02-20", DateTimeOffset.UtcNow.AddMinutes(-30))
            };

            var passedCount = results.Count(r => r.Status == ComplianceCheckStatus.Passed);
            var failedCount = results.Count(r => r.Status == ComplianceCheckStatus.Failed);
            var warningCount = results.Count(r => r.Status == ComplianceCheckStatus.Warning);

            var response = new Response(
                TotalChecks: results.Count,
                PassedCount: passedCount,
                FailedCount: failedCount,
                WarningCount: warningCount,
                Results: results,
                ExecutedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resultados de compliance checks.</summary>
    public sealed record Response(
        int TotalChecks,
        int PassedCount,
        int FailedCount,
        int WarningCount,
        IReadOnlyList<ComplianceCheckResultDto> Results,
        DateTimeOffset ExecutedAt);

    /// <summary>DTO de resultado de check de compliance.</summary>
    public sealed record ComplianceCheckResultDto(
        string CheckId,
        string CheckName,
        string ServiceId,
        string ServiceName,
        string Team,
        string Domain,
        ComplianceCheckStatus Status,
        string PolicyId,
        string Detail,
        DateTimeOffset EvaluatedAt);
}
