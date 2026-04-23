using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiGovernanceComplianceReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação null (honest-null) de IAiGovernanceComplianceReader.
/// Retorna sempre listas vazias — serve como bridge sem infra real.
/// Wave AT.3 — GetAiGovernanceComplianceReport.
/// </summary>
public sealed class NullAiGovernanceComplianceReader : IAiGovernanceComplianceReader
{
    public Task<IReadOnlyList<GetAiGovernanceComplianceReport.ModelComplianceRow>> GetComplianceRowsAsync(
        string tenantId,
        int auditTrailLookbackDays,
        int modelReviewDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetAiGovernanceComplianceReport.ModelComplianceRow>>([]);

    public Task<IReadOnlyList<GetAiGovernanceComplianceReport.PolicyViolation>> GetPolicyViolationsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetAiGovernanceComplianceReport.PolicyViolation>>([]);
}
