using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Abstração para persistência e consulta de configurações de canais de entrega.
/// Implementação fornecida na camada Infrastructure (EF Core / PostgreSQL).
/// </summary>
public interface IDeliveryChannelConfigurationStore
{
    /// <summary>Persiste uma nova configuração de canal.</summary>
    Task AddAsync(DeliveryChannelConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>Obtém uma configuração de canal por Id.</summary>
    Task<DeliveryChannelConfiguration?> GetByIdAsync(
        DeliveryChannelConfigurationId id,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém a configuração ativa de um tipo de canal para um tenant.</summary>
    Task<DeliveryChannelConfiguration?> GetByChannelTypeAsync(
        Guid tenantId,
        DeliveryChannel channelType,
        CancellationToken cancellationToken = default);

    /// <summary>Lista todas as configurações de canal de um tenant.</summary>
    Task<IReadOnlyList<DeliveryChannelConfiguration>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas na entidade.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
