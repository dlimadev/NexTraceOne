using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Domain.Notifications;

/// <summary>
/// Entidade central que representa uma requisição de notificação na plataforma.
/// Contém todas as informações necessárias para o orquestrador resolver template,
/// determinar canais, identificar destinatários e despachar a notificação.
/// Usa chaves i18n para subject e body, permitindo localização no frontend.
/// Parâmetros dinâmicos são armazenados como JSON para interpolação no template.
/// </summary>
public sealed class NotificationRequest : AuditableEntity<NotificationId>
{
    /// <summary>Identificador do tenant ao qual esta notificação pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Categoria de negócio da notificação para roteamento e filtragem.</summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>Severidade da notificação para priorização e decisão de canal.</summary>
    public NotificationSeverity Severity { get; private set; }

    /// <summary>
    /// Código do template de notificação.
    /// Usado para resolver subject e body via <see cref="NotificationTemplate"/>.
    /// </summary>
    public string TemplateCode { get; private set; } = string.Empty;

    /// <summary>Chave i18n do assunto da notificação para localização no frontend.</summary>
    public string SubjectKey { get; private set; } = string.Empty;

    /// <summary>Chave i18n do corpo da notificação para localização no frontend.</summary>
    public string BodyKey { get; private set; } = string.Empty;

    /// <summary>
    /// Parâmetros dinâmicos em formato JSON para interpolação no template.
    /// Exemplo: {"userName": "João", "releaseName": "v2.1.0"}
    /// </summary>
    public string Parameters { get; private set; } = "{}";

    /// <summary>URL de deep link para navegação direta ao contexto da notificação.</summary>
    public string? DeepLinkUrl { get; private set; }

    /// <summary>
    /// Indica se o destinatário deve confirmar ciência explícita da notificação.
    /// Usado em alertas de segurança, aprovações e break glass.
    /// </summary>
    public bool RequiresAcknowledgement { get; private set; }

    /// <summary>Data/hora UTC de expiração. Após essa data, a notificação não é mais relevante.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Módulo de origem que gerou a notificação.
    /// Permite rastreabilidade cross-module e filtragem por origem.
    /// </summary>
    public string SourceModule { get; private set; } = string.Empty;

    /// <summary>
    /// Id de correlação para rastreabilidade distribuída.
    /// Conecta a notificação ao fluxo que a originou (workflow, release, etc.).
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Id do usuário que originou a ação que disparou a notificação.</summary>
    public Guid? CreatedByUserId { get; private set; }

    private NotificationRequest() { }

    /// <summary>
    /// Factory method para criação de uma requisição de notificação.
    /// Centraliza a construção e garante que invariantes sejam respeitadas.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="category">Categoria de negócio da notificação.</param>
    /// <param name="severity">Severidade para priorização e roteamento.</param>
    /// <param name="templateCode">Código do template para resolução de conteúdo.</param>
    /// <param name="subjectKey">Chave i18n do assunto.</param>
    /// <param name="bodyKey">Chave i18n do corpo.</param>
    /// <param name="sourceModule">Módulo de origem que gerou a notificação.</param>
    /// <param name="parameters">Parâmetros JSON para interpolação no template.</param>
    /// <param name="deepLinkUrl">URL de navegação direta ao contexto.</param>
    /// <param name="requiresAcknowledgement">Se exige confirmação explícita.</param>
    /// <param name="expiresAt">Data/hora UTC de expiração.</param>
    /// <param name="correlationId">Id de correlação para rastreabilidade.</param>
    /// <param name="createdByUserId">Id do usuário que originou a ação.</param>
    /// <returns>Instância validada de <see cref="NotificationRequest"/>.</returns>
    public static NotificationRequest Create(
        Guid tenantId,
        NotificationCategory category,
        NotificationSeverity severity,
        string templateCode,
        string subjectKey,
        string bodyKey,
        string sourceModule,
        string? parameters = null,
        string? deepLinkUrl = null,
        bool requiresAcknowledgement = false,
        DateTimeOffset? expiresAt = null,
        string? correlationId = null,
        Guid? createdByUserId = null)
    {
        Guard.Against.NullOrWhiteSpace(templateCode);
        Guard.Against.NullOrWhiteSpace(subjectKey);
        Guard.Against.NullOrWhiteSpace(bodyKey);
        Guard.Against.NullOrWhiteSpace(sourceModule);

        return new NotificationRequest
        {
            Id = new NotificationId(Guid.NewGuid()),
            TenantId = tenantId,
            Category = category,
            Severity = severity,
            TemplateCode = templateCode,
            SubjectKey = subjectKey,
            BodyKey = bodyKey,
            Parameters = parameters ?? "{}",
            DeepLinkUrl = deepLinkUrl,
            RequiresAcknowledgement = requiresAcknowledgement,
            ExpiresAt = expiresAt,
            SourceModule = sourceModule,
            CorrelationId = correlationId,
            CreatedByUserId = createdByUserId
        };
    }
}
