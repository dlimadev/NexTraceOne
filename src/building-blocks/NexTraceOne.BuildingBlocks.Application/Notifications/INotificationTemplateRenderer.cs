using NexTraceOne.BuildingBlocks.Domain.Notifications;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Renderizador de templates de notificação.
/// Responsável por resolver placeholders nos templates de subject e body
/// usando os parâmetros dinâmicos fornecidos na requisição.
/// Suporta i18n para localização em múltiplos idiomas.
/// </summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Renderiza o subject e body de um template usando os parâmetros fornecidos.
    /// </summary>
    /// <param name="template">Template de notificação com placeholders.</param>
    /// <param name="parameters">Parâmetros dinâmicos para interpolação (chave-valor).</param>
    /// <param name="locale">Código do idioma para localização (ex: "pt-BR", "en").</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Tupla com subject e body renderizados, ou erro se o template for inválido.</returns>
    Task<Result<RenderedNotification>> RenderAsync(
        NotificationTemplate template,
        Dictionary<string, string>? parameters = null,
        string locale = "pt-BR",
        CancellationToken ct = default);
}

/// <summary>
/// Resultado da renderização de um template de notificação.
/// Contém subject e body já interpolados com os parâmetros dinâmicos.
/// </summary>
/// <param name="Subject">Assunto renderizado da notificação.</param>
/// <param name="Body">Corpo renderizado da notificação.</param>
public sealed record RenderedNotification(string Subject, string Body);
