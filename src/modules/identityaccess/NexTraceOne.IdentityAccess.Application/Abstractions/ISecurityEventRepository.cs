using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de eventos de segurança e anomalias detectadas.
/// </summary>
public interface ISecurityEventRepository
{
    /// <summary>Lista eventos de segurança de um tenant com filtro opcional por tipo.</summary>
    Task<IReadOnlyList<SecurityEvent>> ListByTenantAsync(
        TenantId tenantId,
        string? eventType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Conta eventos não revisados em um tenant.</summary>
    Task<int> CountUnreviewedByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo evento de segurança para persistência.</summary>
    void Add(SecurityEvent securityEvent);
}
