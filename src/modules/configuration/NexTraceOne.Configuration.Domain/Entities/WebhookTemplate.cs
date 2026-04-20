using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Template de payload personalizado para webhooks do tenant.
/// Permite definir o formato e os cabeçalhos enviados para destinos externos em eventos específicos.
///
/// Invariantes:
/// - Nome não pode exceder 100 caracteres.
/// - PayloadTemplate não pode estar vazio.
/// - EventType normalizado para valores reconhecidos.
/// </summary>
public sealed class WebhookTemplate : Entity<WebhookTemplateId>
{
    private const int MaxNameLength = 100;
    private static readonly string[] ValidEventTypes =
        ["change.created", "incident.opened", "contract.published", "approval.expired"];

    private WebhookTemplate() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo do template.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tipo de evento que activa o webhook: change.created, incident.opened, contract.published, approval.expired.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Template Handlebars do payload enviado.</summary>
    public string PayloadTemplate { get; private set; } = string.Empty;

    /// <summary>Cabeçalhos HTTP adicionais em JSON (chave/valor).</summary>
    public string? HeadersJson { get; private set; }

    /// <summary>Indica se o template está activo.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Cria um novo template de webhook.</summary>
    public static WebhookTemplate Create(
        string tenantId,
        string name,
        string eventType,
        string payloadTemplate,
        string? headersJson,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(payloadTemplate, nameof(payloadTemplate));

        return new WebhookTemplate
        {
            Id = new WebhookTemplateId(Guid.NewGuid()),
            TenantId = tenantId.Trim(),
            Name = name.Trim(),
            EventType = NormalizeEventType(eventType),
            PayloadTemplate = payloadTemplate.Trim(),
            HeadersJson = string.IsNullOrWhiteSpace(headersJson) ? null : headersJson.Trim(),
            IsEnabled = true,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Alterna o estado activo/inactivo do template.</summary>
    public void Toggle(bool enabled)
    {
        IsEnabled = enabled;
    }

    /// <summary>Actualiza os detalhes do template.</summary>
    public void UpdateDetails(string name, string eventType, string payloadTemplate, string? headersJson)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(payloadTemplate, nameof(payloadTemplate));

        Name = name.Trim();
        EventType = NormalizeEventType(eventType);
        PayloadTemplate = payloadTemplate.Trim();
        HeadersJson = string.IsNullOrWhiteSpace(headersJson) ? null : headersJson.Trim();
    }

    private static string NormalizeEventType(string? eventType) =>
        ValidEventTypes.Contains(eventType?.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            ? eventType!.Trim().ToLowerInvariant()
            : ValidEventTypes[0];
}
