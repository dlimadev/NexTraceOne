using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração para persistência e consulta da configuração SMTP.
/// Implementação fornecida na camada Infrastructure (EF Core / PostgreSQL).
/// </summary>
public interface ISmtpConfigurationStore
{
    /// <summary>Persiste uma nova configuração SMTP.</summary>
    Task AddAsync(SmtpConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>Obtém a configuração SMTP de um tenant.</summary>
    Task<SmtpConfiguration?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém uma configuração SMTP por Id.</summary>
    Task<SmtpConfiguration?> GetByIdAsync(
        SmtpConfigurationId id,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas na entidade.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
