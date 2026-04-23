using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IApprovalWorkflowReader"/>.
/// Retorna lista vazia quando o bridge com dados de aprovação não está configurado.
///
/// Wave AP.1 — GetApprovalWorkflowReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullApprovalWorkflowReader : IApprovalWorkflowReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry>>([]);
}
