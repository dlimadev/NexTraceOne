using NexTraceOne.Notifications.Domain.Entities;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de agrupamento e correlação de notificações.
/// Gera chaves de correlação e decide agrupamento com base no contexto.
/// </summary>
public interface INotificationGroupingService
{
    /// <summary>
    /// Gera a chave de correlação para uma notificação com base no seu contexto.
    /// Notificações com a mesma chave são agrupáveis.
    /// </summary>
    string GenerateCorrelationKey(
        Guid tenantId,
        string eventType,
        string sourceModule,
        string? sourceEntityType,
        string? sourceEntityId);

    /// <summary>
    /// Tenta encontrar ou criar um grupo para a notificação dada.
    /// Retorna o GroupId se agrupamento for aplicável.
    /// </summary>
    Task<Guid?> ResolveGroupAsync(
        Guid tenantId,
        string correlationKey,
        int windowMinutes = 60,
        CancellationToken cancellationToken = default);
}
