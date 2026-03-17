namespace NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Audit.
/// Outros módulos que precisarem registar eventos de auditoria devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IAuditModule
{
    /// <summary>Registra um evento de auditoria na trilha.</summary>
    Task RecordEventAsync(string sourceModule, string actionType, string resourceId, string resourceType, string performedBy, Guid tenantId, string? payload, CancellationToken cancellationToken);

    /// <summary>Verifica a integridade da cadeia de hash.</summary>
    Task<bool> VerifyChainIntegrityAsync(CancellationToken cancellationToken);
}
