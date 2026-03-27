using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Template persistido de conteúdo de notificação.
/// Permite que administradores definam conteúdo personalizado por tipo de evento e canal,
/// substituindo os templates internos em memória quando presente.
/// </summary>
public sealed class NotificationTemplate : Entity<NotificationTemplateId>
{
    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(
        NotificationTemplateId id,
        Guid tenantId,
        string eventType,
        string name,
        string subjectTemplate,
        string bodyTemplate,
        string? plainTextTemplate,
        DeliveryChannel? channel,
        string locale,
        bool isBuiltIn)
    {
        Id = id;
        TenantId = tenantId;
        EventType = eventType;
        Name = name;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        PlainTextTemplate = plainTextTemplate;
        Channel = channel;
        Locale = locale;
        IsBuiltIn = isBuiltIn;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Tenant ao qual o template pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Tipo de evento que este template resolve (ex.: "IncidentCreated", "ApprovalPending").
    /// Corresponde aos valores em NotificationType.
    /// </summary>
    public string EventType { get; private set; } = default!;

    /// <summary>Nome legível do template para exibição na UI de configuração.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Template do assunto (para email) ou título da notificação.
    /// Suporta variáveis no formato {{VariableName}}.
    /// </summary>
    public string SubjectTemplate { get; private set; } = default!;

    /// <summary>
    /// Template do corpo da notificação.
    /// Para email, pode conter HTML. Para outros canais, usa markup simples.
    /// Suporta variáveis no formato {{VariableName}}.
    /// </summary>
    public string BodyTemplate { get; private set; } = default!;

    /// <summary>
    /// Template do corpo em texto simples (alternativa ao HTML em email).
    /// Opcional. Quando presente, é enviado como vista alternativa plain-text.
    /// </summary>
    public string? PlainTextTemplate { get; private set; }

    /// <summary>
    /// Canal de entrega para o qual este template se aplica.
    /// Quando null, aplica-se a todos os canais.
    /// </summary>
    public DeliveryChannel? Channel { get; private set; }

    /// <summary>
    /// Código de locale deste template (ex.: "en", "pt").
    /// Permite templates multilíngues para o mesmo tipo de evento.
    /// </summary>
    public string Locale { get; private set; } = default!;

    /// <summary>Indica se o template está ativo e deve ser utilizado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Indica se é um template de sistema (built-in).
    /// Templates built-in não podem ser removidos, apenas desativados.
    /// </summary>
    public bool IsBuiltIn { get; private set; }

    /// <summary>Data/hora UTC de criação do template.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização do template.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cria um novo template de notificação personalizado.
    /// </summary>
    public static NotificationTemplate Create(
        Guid tenantId,
        string eventType,
        string name,
        string subjectTemplate,
        string bodyTemplate,
        string? plainTextTemplate = null,
        DeliveryChannel? channel = null,
        string locale = "en")
    {
        return new NotificationTemplate(
            new NotificationTemplateId(Guid.NewGuid()),
            tenantId,
            eventType,
            name,
            subjectTemplate,
            bodyTemplate,
            plainTextTemplate,
            channel,
            locale,
            isBuiltIn: false);
    }

    /// <summary>
    /// Cria um template de sistema (built-in) para seeding inicial.
    /// </summary>
    public static NotificationTemplate CreateBuiltIn(
        Guid tenantId,
        string eventType,
        string name,
        string subjectTemplate,
        string bodyTemplate,
        string? plainTextTemplate = null,
        DeliveryChannel? channel = null,
        string locale = "en")
    {
        return new NotificationTemplate(
            new NotificationTemplateId(Guid.NewGuid()),
            tenantId,
            eventType,
            name,
            subjectTemplate,
            bodyTemplate,
            plainTextTemplate,
            channel,
            locale,
            isBuiltIn: true);
    }

    /// <summary>Atualiza o conteúdo do template.</summary>
    public void Update(
        string name,
        string subjectTemplate,
        string bodyTemplate,
        string? plainTextTemplate)
    {
        Name = name;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        PlainTextTemplate = plainTextTemplate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Ativa o template.</summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Desativa o template (não o remove).</summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
