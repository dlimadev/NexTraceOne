using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IPolicyEvaluationHistoryReader"/>.
/// Retorna lista vazia quando o bridge com o módulo IdentityAccess não está configurado.
///
/// Wave AJ.3 — GetPlatformPolicyComplianceReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullPolicyEvaluationHistoryReader : IPolicyEvaluationHistoryReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord>> ListEvaluationsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord>>([]);
}
