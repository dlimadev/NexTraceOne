namespace NexTraceOne.AuditCompliance.Domain.Audit.Ports;

/// <summary>
/// Porta de verificação de integridade da trilha de auditoria.
/// Garante que eventos de auditoria não foram adulterados.
/// Preparada para futura extração como Audit Ledger independente.
/// </summary>
public interface IAuditIntegrityPort
{
    /// <summary>
    /// Verifica a integridade de um checkpoint de auditoria.
    /// </summary>
    Task<bool> VerifyCheckpointIntegrityAsync(Guid checkpointId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um novo checkpoint de integridade para o período especificado.
    /// </summary>
    Task<Guid> CreateIntegrityCheckpointAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
