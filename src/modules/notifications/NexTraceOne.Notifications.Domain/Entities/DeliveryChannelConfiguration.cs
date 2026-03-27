using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Configuração persistida de um canal de entrega de notificações.
/// Substitui a configuração estática de appsettings para configuração gerida em runtime,
/// permitindo alterações de canal sem redeploy.
/// Cada tenant pode ter a sua própria configuração por canal.
/// </summary>
public sealed class DeliveryChannelConfiguration : Entity<DeliveryChannelConfigurationId>
{
    private DeliveryChannelConfiguration() { } // EF Core

    private DeliveryChannelConfiguration(
        DeliveryChannelConfigurationId id,
        Guid tenantId,
        DeliveryChannel channelType,
        string displayName,
        bool isEnabled,
        string? configurationJson)
    {
        Id = id;
        TenantId = tenantId;
        ChannelType = channelType;
        DisplayName = displayName;
        IsEnabled = isEnabled;
        ConfigurationJson = configurationJson;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Tenant ao qual a configuração pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Tipo de canal de entrega configurado.</summary>
    public DeliveryChannel ChannelType { get; private set; }

    /// <summary>Nome de exibição do canal para a UI de configuração.</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>Indica se o canal está habilitado para uso.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Configuração específica do canal em formato JSON.
    /// Para Email: host, porta, SSL, credenciais.
    /// Para Teams: webhook URL.
    /// Para Webhook genérico: endpoint URL, headers, autenticação.
    /// </summary>
    public string? ConfigurationJson { get; private set; }

    /// <summary>Data/hora UTC de criação da configuração.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cria uma nova configuração de canal de entrega.
    /// </summary>
    public static DeliveryChannelConfiguration Create(
        Guid tenantId,
        DeliveryChannel channelType,
        string displayName,
        bool isEnabled = false,
        string? configurationJson = null)
    {
        return new DeliveryChannelConfiguration(
            new DeliveryChannelConfigurationId(Guid.NewGuid()),
            tenantId,
            channelType,
            displayName,
            isEnabled,
            configurationJson);
    }

    /// <summary>Atualiza a configuração do canal.</summary>
    public void Update(
        string displayName,
        bool isEnabled,
        string? configurationJson)
    {
        DisplayName = displayName;
        IsEnabled = isEnabled;
        ConfigurationJson = configurationJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Habilita o canal.</summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Desabilita o canal.</summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
