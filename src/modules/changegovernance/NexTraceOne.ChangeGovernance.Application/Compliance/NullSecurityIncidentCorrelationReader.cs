using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance;

/// <summary>
/// Implementação null (honest-null) de ISecurityIncidentCorrelationReader.
/// Retorna lista vazia — sem dados de correlação de incidentes de segurança disponíveis.
/// Wave AX.3 — GetSecurityIncidentCorrelationReport.
/// </summary>
public sealed class NullSecurityIncidentCorrelationReader : ISecurityIncidentCorrelationReader
{
    public Task<IReadOnlyList<ISecurityIncidentCorrelationReader.SecurityIncidentEntry>>
        ListSecurityIncidentsByTenantAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISecurityIncidentCorrelationReader.SecurityIncidentEntry>>([]);
}
