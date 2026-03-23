using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Application.Engine;

/// <summary>
/// Implementação do contrato público INotificationModule.
/// Fachada que permite a outros módulos submeter notificações à engine central
/// sem acoplamento direto à implementação do orquestrador.
/// </summary>
public sealed class NotificationModuleService(
    INotificationOrchestrator orchestrator,
    INotificationStore store) : INotificationModule
{
    /// <inheritdoc/>
    public Task<NotificationResult> SubmitAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default) =>
        orchestrator.ProcessAsync(request, cancellationToken);

    /// <inheritdoc/>
    public Task<int> GetUnreadCountAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        store.CountUnreadAsync(recipientUserId, cancellationToken);
}
