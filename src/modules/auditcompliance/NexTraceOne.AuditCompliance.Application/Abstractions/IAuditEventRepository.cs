using NexTraceOne.Audit.Domain.Entities;

namespace NexTraceOne.Audit.Application.Abstractions;

/// <summary>
/// Repositório de eventos de auditoria do módulo Audit.
/// </summary>
public interface IAuditEventRepository
{
    /// <summary>Obtém um evento de auditoria pelo identificador.</summary>
    Task<AuditEvent?> GetByIdAsync(AuditEventId id, CancellationToken cancellationToken);

    /// <summary>Pesquisa eventos de auditoria por módulo, tipo de ação ou período.</summary>
    Task<IReadOnlyList<AuditEvent>> SearchAsync(string? sourceModule, string? actionType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Obtém a trilha de auditoria de um recurso específico.</summary>
    Task<IReadOnlyList<AuditEvent>> GetTrailByResourceAsync(string resourceType, string resourceId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo evento de auditoria.</summary>
    void Add(AuditEvent auditEvent);
}
