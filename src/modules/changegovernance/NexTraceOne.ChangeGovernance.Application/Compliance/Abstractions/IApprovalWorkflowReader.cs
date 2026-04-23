namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de eficiência de workflows de aprovação.
/// Por omissão satisfeita por <c>NullApprovalWorkflowReader</c> (honest-null).
/// Wave AP.1 — GetApprovalWorkflowReport.
/// </summary>
public interface IApprovalWorkflowReader
{
    Task<IReadOnlyList<ApprovalEnvironmentEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record ApprovalEnvironmentEntry(
        string Environment,
        string ApprovalType,
        int TotalApprovals,
        decimal AvgApprovalTimeHours,
        decimal SlaComplianceRate,
        decimal AutoApprovalRate,
        decimal RejectionRate,
        int PendingCount,
        IReadOnlyList<ApproverBacklog> ApproverBacklogs);

    public sealed record ApproverBacklog(string ApproverId, string ApproverName, int PendingCount);
}
