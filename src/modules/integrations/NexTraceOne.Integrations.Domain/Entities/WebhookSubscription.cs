using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para WebhookSubscription.
/// </summary>
public sealed record WebhookSubscriptionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado que representa uma subscrição de webhook outbound.
/// Permite que tenants configurem endpoints externos para receber notificações
/// quando eventos relevantes ocorrem no NexTraceOne (incidents, changes, contracts, services, alerts).
///
/// Owner: módulo Integrations.
/// </summary>
public sealed class WebhookSubscription : Entity<WebhookSubscriptionId>
{
    /// <summary>Identificador do tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo da subscrição (ex: "Incident Alerts — PagerDuty").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL de destino do webhook (HTTPS obrigatório).</summary>
    public string TargetUrl { get; private set; } = string.Empty;

    /// <summary>Tipos de evento subscritos (ex: "incident.created", "change.deployed").</summary>
    public IReadOnlyList<string> EventTypes { get; private set; } = [];

    /// <summary>Hash do segredo HMAC para assinatura de payloads (null se sem assinatura).</summary>
    public string? SecretHash { get; private set; }

    /// <summary>Descrição opcional da subscrição.</summary>
    public string? Description { get; private set; }

    /// <summary>Indica se a subscrição está activa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Total de entregas realizadas.</summary>
    public long DeliveryCount { get; private set; }

    /// <summary>Total de entregas com sucesso.</summary>
    public long SuccessCount { get; private set; }

    /// <summary>Total de entregas com falha.</summary>
    public long FailureCount { get; private set; }

    /// <summary>Data/hora UTC do último trigger.</summary>
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    private WebhookSubscription() { }

    /// <summary>
    /// Cria uma nova subscrição de webhook outbound.
    /// </summary>
    public static WebhookSubscription Create(
        string tenantId,
        string name,
        string targetUrl,
        IReadOnlyList<string> eventTypes,
        string? secretHash,
        string? description,
        bool isActive,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(targetUrl, nameof(targetUrl));
        Guard.Against.StringTooLong(targetUrl, 500, nameof(targetUrl));
        Guard.Against.Null(eventTypes, nameof(eventTypes));

        return new WebhookSubscription
        {
            Id = new WebhookSubscriptionId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name.Trim(),
            TargetUrl = targetUrl.Trim(),
            EventTypes = eventTypes,
            SecretHash = secretHash,
            Description = description?.Trim(),
            IsActive = isActive,
            DeliveryCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            CreatedAt = utcNow
        };
    }

    /// <summary>Activa a subscrição.</summary>
    public void Activate(DateTimeOffset utcNow)
    {
        IsActive = true;
        UpdatedAt = utcNow;
    }

    /// <summary>Desactiva a subscrição.</summary>
    public void Deactivate(DateTimeOffset utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }

    /// <summary>Regista uma entrega bem-sucedida.</summary>
    public void RecordDeliverySuccess(DateTimeOffset utcNow)
    {
        DeliveryCount++;
        SuccessCount++;
        LastTriggeredAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Regista uma falha de entrega.</summary>
    public void RecordDeliveryFailure(DateTimeOffset utcNow)
    {
        DeliveryCount++;
        FailureCount++;
        LastTriggeredAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza a configuração da subscrição.</summary>
    public void Update(
        string name,
        string targetUrl,
        IReadOnlyList<string> eventTypes,
        string? secretHash,
        string? description,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(targetUrl, nameof(targetUrl));
        Guard.Against.StringTooLong(targetUrl, 500, nameof(targetUrl));

        Name = name.Trim();
        TargetUrl = targetUrl.Trim();
        EventTypes = eventTypes;
        SecretHash = secretHash;
        Description = description?.Trim();
        UpdatedAt = utcNow;
    }
}
