using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Infrastructure.Routing;

/// <summary>
/// Resolve destinatários a partir de um pedido de notificação.
/// Utiliza IDs explícitos quando disponíveis; resolução de roles e equipas
/// está preparada (scaffolded) para implementação futura.
/// </summary>
internal sealed class NotificationRecipientResolver(
    ILogger<NotificationRecipientResolver> logger) : INotificationRecipientResolver
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(
        NotificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recipients = new HashSet<Guid>();

        // Destinatários explícitos (prioritário)
        if (request.RecipientUserIds is { Count: > 0 })
        {
            foreach (var userId in request.RecipientUserIds)
            {
                if (userId != Guid.Empty)
                    recipients.Add(userId);
            }
        }

        // Resolução de roles — scaffolded para implementação futura
        if (request.RecipientRoles is { Count: > 0 } && recipients.Count == 0)
        {
            logger.LogWarning(
                "Role-based recipient resolution is not yet implemented. Roles specified: [{Roles}]. Event: {EventType}",
                string.Join(", ", request.RecipientRoles), request.EventType);
        }

        // Resolução de equipas — scaffolded para implementação futura
        if (request.RecipientTeamIds is { Count: > 0 } && recipients.Count == 0)
        {
            logger.LogWarning(
                "Team-based recipient resolution is not yet implemented. Teams specified: [{Teams}]. Event: {EventType}",
                string.Join(", ", request.RecipientTeamIds), request.EventType);
        }

        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "No recipients resolved for notification event {EventType}. No explicit user IDs, roles, or teams could be resolved.",
                request.EventType);
        }

        IReadOnlyList<Guid> result = [.. recipients];
        return Task.FromResult(result);
    }
}
