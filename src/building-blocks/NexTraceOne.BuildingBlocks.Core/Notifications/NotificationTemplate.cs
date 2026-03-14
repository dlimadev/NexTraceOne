using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Template de notificação que define estrutura, conteúdo padrão e canais suportados.
/// Cada template possui um código único usado como referência pelo orquestrador.
/// Suporta i18n para localização de subject e body em múltiplos idiomas.
/// Canais suportados são armazenados como string separada por vírgula para simplicidade
/// no MVP1 (sem necessidade de tabela auxiliar).
/// </summary>
public sealed class NotificationTemplate : AuditableEntity<NotificationTemplateId>
{
    /// <summary>
    /// Código único do template, usado como referência estável pelo orquestrador.
    /// Exemplo: "workflow.approval_requested", "security.break_glass_activated".
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Categoria de negócio associada ao template.</summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Template do assunto com placeholders para interpolação.
    /// Exemplo: "Aprovação pendente: {releaseName}".
    /// </summary>
    public string SubjectTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// Template do corpo com placeholders para interpolação.
    /// Suporta Markdown para formatação em canais que suportam rich text.
    /// </summary>
    public string BodyTemplate { get; private set; } = string.Empty;

    /// <summary>Canal padrão para envio quando o usuário não tem preferência definida.</summary>
    public NotificationChannel DefaultChannel { get; private set; }

    /// <summary>Severidade padrão da notificação gerada por este template.</summary>
    public NotificationSeverity DefaultSeverity { get; private set; }

    /// <summary>Indica se o template está ativo e pode ser usado para gerar notificações.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Lista de canais suportados, separados por vírgula.
    /// Exemplo: "InApp,Email,MicrosoftTeams".
    /// O orquestrador usa esta lista para determinar fallback entre canais.
    /// </summary>
    public string SupportedChannels { get; private set; } = string.Empty;

    /// <summary>Indica se o template suporta localização i18n para múltiplos idiomas.</summary>
    public bool SupportsI18n { get; private set; }

    private NotificationTemplate() { }

    /// <summary>
    /// Factory method para criação de um template de notificação.
    /// Centraliza a construção e garante que invariantes sejam respeitadas.
    /// </summary>
    /// <param name="code">Código único do template.</param>
    /// <param name="category">Categoria de negócio.</param>
    /// <param name="subjectTemplate">Template do assunto com placeholders.</param>
    /// <param name="bodyTemplate">Template do corpo com placeholders.</param>
    /// <param name="defaultChannel">Canal padrão de envio.</param>
    /// <param name="defaultSeverity">Severidade padrão.</param>
    /// <param name="supportedChannels">Canais suportados (separados por vírgula).</param>
    /// <param name="supportsI18n">Se suporta localização i18n.</param>
    /// <returns>Instância validada de <see cref="NotificationTemplate"/>.</returns>
    public static NotificationTemplate Create(
        string code,
        NotificationCategory category,
        string subjectTemplate,
        string bodyTemplate,
        NotificationChannel defaultChannel,
        NotificationSeverity defaultSeverity,
        string supportedChannels,
        bool supportsI18n = true)
    {
        Guard.Against.NullOrWhiteSpace(code);
        Guard.Against.NullOrWhiteSpace(subjectTemplate);
        Guard.Against.NullOrWhiteSpace(bodyTemplate);
        Guard.Against.NullOrWhiteSpace(supportedChannels);

        return new NotificationTemplate
        {
            Id = new NotificationTemplateId(Guid.NewGuid()),
            Code = code,
            Category = category,
            SubjectTemplate = subjectTemplate,
            BodyTemplate = bodyTemplate,
            DefaultChannel = defaultChannel,
            DefaultSeverity = defaultSeverity,
            IsActive = true,
            SupportedChannels = supportedChannels,
            SupportsI18n = supportsI18n
        };
    }

    /// <summary>Desativa o template, impedindo que novas notificações o utilizem.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa o template para uso pelo orquestrador.</summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Atualiza o conteúdo do template (subject e body).
    /// Mantém o código e categoria inalterados.
    /// </summary>
    /// <param name="subjectTemplate">Novo template do assunto.</param>
    /// <param name="bodyTemplate">Novo template do corpo.</param>
    public void UpdateContent(string subjectTemplate, string bodyTemplate)
    {
        Guard.Against.NullOrWhiteSpace(subjectTemplate);
        Guard.Against.NullOrWhiteSpace(bodyTemplate);

        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
    }
}
