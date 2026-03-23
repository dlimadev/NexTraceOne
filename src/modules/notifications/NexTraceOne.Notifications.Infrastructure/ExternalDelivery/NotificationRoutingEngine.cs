using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Motor de roteamento inicial para canais de notificação.
/// Determina quais canais devem ser utilizados com base na severidade e no tipo de evento.
/// Respeita a habilitação/desabilitação de canais por configuração.
///
/// Regras iniciais (Fase 3):
/// - Info → InApp only
/// - ActionRequired → InApp + Email
/// - Warning → InApp + Email + Teams
/// - Critical → InApp + Email + Teams
/// </summary>
internal sealed class NotificationRoutingEngine(
    IOptions<NotificationChannelOptions> channelOptions,
    ILogger<NotificationRoutingEngine> logger) : INotificationRoutingEngine
{
    private static readonly IReadOnlyList<DeliveryChannel> InAppOnly = [DeliveryChannel.InApp];

    /// <inheritdoc/>
    public Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(
        Guid recipientUserId,
        NotificationCategory category,
        NotificationSeverity severity,
        CancellationToken cancellationToken = default)
    {
        var channels = new List<DeliveryChannel> { DeliveryChannel.InApp };
        var options = channelOptions.Value;

        switch (severity)
        {
            case NotificationSeverity.Critical:
            case NotificationSeverity.Warning:
                // Critical + Warning → InApp + Email + Teams
                if (options.Email.Enabled)
                    channels.Add(DeliveryChannel.Email);
                if (options.Teams.Enabled)
                    channels.Add(DeliveryChannel.MicrosoftTeams);
                break;

            case NotificationSeverity.ActionRequired:
                // ActionRequired → InApp + Email
                if (options.Email.Enabled)
                    channels.Add(DeliveryChannel.Email);
                break;

            case NotificationSeverity.Info:
            default:
                // Info → InApp only
                break;
        }

        logger.LogDebug(
            "Routing resolved for user {UserId}, severity {Severity}, category {Category}: [{Channels}]",
            recipientUserId, severity, category, string.Join(", ", channels));

        return Task.FromResult<IReadOnlyList<DeliveryChannel>>(channels);
    }
}
