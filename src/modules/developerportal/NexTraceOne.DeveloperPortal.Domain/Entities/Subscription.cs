using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.DeveloperPortal.Domain.Enums;
using NexTraceOne.DeveloperPortal.Domain.Errors;

namespace NexTraceOne.DeveloperPortal.Domain.Entities;

/// <summary>
/// Aggregate Root que representa a subscrição de um desenvolvedor a uma API do catálogo.
/// Permite que consumidores recebam notificações de alterações (breaking changes, depreciações,
/// alertas de segurança) por e-mail ou webhook. Cada subscrição vincula um utilizador e
/// o serviço consumidor a uma API específica, com nível e canal de notificação configuráveis.
/// </summary>
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    private Subscription() { }

    /// <summary>Identificador do ativo de API subscrito no módulo EngineeringGraph.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome legível da API subscrita.</summary>
    public string ApiName { get; private set; } = string.Empty;

    /// <summary>Identificador do utilizador que criou a subscrição.</summary>
    public Guid SubscriberId { get; private set; }

    /// <summary>E-mail do subscritor para notificações por e-mail.</summary>
    public string SubscriberEmail { get; private set; } = string.Empty;

    /// <summary>Nome do serviço consumidor que depende desta API.</summary>
    public string ConsumerServiceName { get; private set; } = string.Empty;

    /// <summary>Versão do serviço consumidor no momento da subscrição.</summary>
    public string ConsumerServiceVersion { get; private set; } = string.Empty;

    /// <summary>Nível de notificação desejado (apenas breaking, todas as mudanças, etc.).</summary>
    public SubscriptionLevel Level { get; private set; }

    /// <summary>Canal de entrega das notificações (e-mail ou webhook).</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>URL do webhook para entrega de notificações, obrigatório quando o canal é Webhook.</summary>
    public string? WebhookUrl { get; private set; }

    /// <summary>Indica se a subscrição está ativa e a receber notificações.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação da subscrição.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última notificação enviada, ou null se nunca notificado.</summary>
    public DateTimeOffset? LastNotifiedAt { get; private set; }

    /// <summary>
    /// Cria uma nova subscrição de notificações para uma API.
    /// Valida que o canal Webhook possui URL válida e que os dados obrigatórios estão presentes.
    /// </summary>
    public static Result<Subscription> Create(
        Guid apiAssetId,
        string apiName,
        Guid subscriberId,
        string subscriberEmail,
        string consumerServiceName,
        string consumerServiceVersion,
        SubscriptionLevel level,
        NotificationChannel channel,
        string? webhookUrl,
        DateTimeOffset createdAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(apiName);
        Guard.Against.Default(subscriberId);
        Guard.Against.NullOrWhiteSpace(subscriberEmail);
        Guard.Against.NullOrWhiteSpace(consumerServiceName);
        Guard.Against.NullOrWhiteSpace(consumerServiceVersion);

        if (channel == NotificationChannel.Webhook && string.IsNullOrWhiteSpace(webhookUrl))
            return DeveloperPortalErrors.InvalidWebhookUrl();

        return Result<Subscription>.Success(new Subscription
        {
            Id = SubscriptionId.New(),
            ApiAssetId = apiAssetId,
            ApiName = apiName,
            SubscriberId = subscriberId,
            SubscriberEmail = subscriberEmail,
            ConsumerServiceName = consumerServiceName,
            ConsumerServiceVersion = consumerServiceVersion,
            Level = level,
            Channel = channel,
            WebhookUrl = webhookUrl,
            IsActive = true,
            CreatedAt = createdAt
        });
    }

    /// <summary>
    /// Desativa a subscrição, interrompendo o envio de notificações.
    /// Retorna falha se a subscrição já estiver inativa.
    /// </summary>
    public Result<MediatR.Unit> Deactivate()
    {
        if (!IsActive)
            return DeveloperPortalErrors.SubscriptionAlreadyInactive(Id.Value.ToString());

        IsActive = false;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Reativa uma subscrição previamente desativada.
    /// Retorna falha se a subscrição já estiver ativa.
    /// </summary>
    public Result<MediatR.Unit> Reactivate()
    {
        if (IsActive)
            return DeveloperPortalErrors.SubscriptionAlreadyActive(Id.Value.ToString());

        IsActive = true;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Atualiza as preferências de notificação (nível, canal e URL de webhook).
    /// Valida que o canal Webhook possui URL válida.
    /// </summary>
    public Result<MediatR.Unit> UpdatePreferences(
        SubscriptionLevel level,
        NotificationChannel channel,
        string? webhookUrl)
    {
        if (channel == NotificationChannel.Webhook && string.IsNullOrWhiteSpace(webhookUrl))
            return DeveloperPortalErrors.InvalidWebhookUrl();

        Level = level;
        Channel = channel;
        WebhookUrl = webhookUrl;
        return MediatR.Unit.Value;
    }

    /// <summary>Regista a data/hora da última notificação enviada com sucesso.</summary>
    public void MarkNotified(DateTimeOffset timestamp)
    {
        LastNotifiedAt = timestamp;
    }
}

/// <summary>Identificador fortemente tipado de Subscription.</summary>
public sealed record SubscriptionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SubscriptionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SubscriptionId From(Guid id) => new(id);
}
