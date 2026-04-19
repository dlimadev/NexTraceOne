using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório de eventos de auditoria do módulo Audit.
/// </summary>
public interface IAuditEventRepository
{
    /// <summary>Obtém um evento de auditoria pelo identificador.</summary>
    Task<AuditEvent?> GetByIdAsync(AuditEventId id, CancellationToken cancellationToken);

    /// <summary>Pesquisa eventos de auditoria por módulo, tipo de ação, correlação ou período.</summary>
    Task<IReadOnlyList<AuditEvent>> SearchAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Obtém a trilha de auditoria de um recurso específico.</summary>
    Task<IReadOnlyList<AuditEvent>> GetTrailByResourceAsync(string resourceType, string resourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Pesquisa eventos com filtros adicionais de recurso (resourceType + resourceId).
    /// P7.4 — suporta correlação lookup: dado um resourceId, encontrar todos os eventos relacionados.
    /// </summary>
    Task<IReadOnlyList<AuditEvent>> SearchWithResourceAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        string? resourceType, string? resourceId,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conta o total de eventos correspondentes aos filtros (sem paginação).
    /// Usado para calcular TotalCount e TotalPages na resposta paginada.
    /// </summary>
    Task<int> CountAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conta o total de eventos com filtros de recurso (sem paginação).
    /// </summary>
    Task<int> CountWithResourceAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        string? resourceType,
        string? resourceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Remove eventos auditáveis anteriores ao cutoff (para aplicar política de retenção).
    /// P7.4 — operação de retenção real sobre aud_audit_events.
    /// Retorna o número de eventos eliminados.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo evento de auditoria.</summary>
    void Add(AuditEvent auditEvent);
}
