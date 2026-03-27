using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório de políticas de retenção de eventos de auditoria.
/// P7.4 — criado para tornar RetentionPolicy funcionalmente real (antes apenas entidade estrutural).
/// </summary>
public interface IRetentionPolicyRepository
{
    /// <summary>Lista todas as políticas de retenção ativas.</summary>
    Task<IReadOnlyList<RetentionPolicy>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Lista todas as políticas (activas e inactivas).</summary>
    Task<IReadOnlyList<RetentionPolicy>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Obtém uma política pelo identificador.</summary>
    Task<RetentionPolicy?> GetByIdAsync(RetentionPolicyId id, CancellationToken cancellationToken);

    /// <summary>Obtém a política activa com menor período de retenção (mais restritiva).</summary>
    Task<RetentionPolicy?> GetMostRestrictiveActiveAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova política de retenção.</summary>
    void Add(RetentionPolicy policy);
}
