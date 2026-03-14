using NexTraceOne.BuildingBlocks.Core.Notifications;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Orquestrador central de notificações da plataforma.
/// Responsável por receber requisições de notificação, resolver templates,
/// determinar canais por preferência do usuário/tenant, e despachar
/// para os channel adapters apropriados com suporte a fallback e retry.
/// Implementação na camada Infrastructure de cada módulo ou no BuildingBlocks.Infrastructure.
/// </summary>
public interface INotificationOrchestrator
{
    /// <summary>
    /// Envia uma notificação completa a partir de uma requisição já montada.
    /// O orquestrador resolve template, determina canais por preferência e despacha.
    /// </summary>
    /// <param name="request">Requisição de notificação com todos os dados necessários.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Id da notificação criada, ou erro de validação/processamento.</returns>
    Task<Result<NotificationId>> SendAsync(
        NotificationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Envia uma notificação para um único usuário usando template code.
    /// Atalho para cenários simples onde há apenas um destinatário.
    /// </summary>
    /// <param name="userId">Id do usuário destinatário.</param>
    /// <param name="templateCode">Código do template de notificação.</param>
    /// <param name="parameters">Parâmetros dinâmicos para interpolação no template.</param>
    /// <param name="severity">Severidade da notificação (padrão: Info).</param>
    /// <param name="deepLinkUrl">URL de navegação direta ao contexto (opcional).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Id da notificação criada, ou erro de validação/processamento.</returns>
    Task<Result<NotificationId>> SendToUserAsync(
        Guid userId,
        string templateCode,
        Dictionary<string, string>? parameters = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? deepLinkUrl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Envia uma notificação para múltiplos usuários usando template code.
    /// Usado em cenários de broadcast: aprovadores de um workflow, membros de um tenant, etc.
    /// </summary>
    /// <param name="userIds">Lista de IDs dos usuários destinatários.</param>
    /// <param name="templateCode">Código do template de notificação.</param>
    /// <param name="parameters">Parâmetros dinâmicos para interpolação no template.</param>
    /// <param name="severity">Severidade da notificação (padrão: Info).</param>
    /// <param name="deepLinkUrl">URL de navegação direta ao contexto (opcional).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de IDs das notificações criadas, ou erro de validação/processamento.</returns>
    Task<Result<IReadOnlyList<NotificationId>>> SendToUsersAsync(
        IReadOnlyList<Guid> userIds,
        string templateCode,
        Dictionary<string, string>? parameters = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? deepLinkUrl = null,
        CancellationToken ct = default);
}
