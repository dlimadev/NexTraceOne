using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Motor de roteamento de canais de notificação com suporte a preferências e obrigatoriedade.
/// Determina quais canais devem ser utilizados com base na severidade, tipo de evento,
/// preferências do utilizador e regras obrigatórias da plataforma.
///
/// Fluxo de decisão (Fase 4):
/// 1. Verificar se é obrigatório → canais obrigatórios sempre incluídos
/// 2. Aplicar regras de severidade para canais base
/// 3. Consultar preferências do utilizador para canais não obrigatórios
/// 4. Filtrar por disponibilidade de infraestrutura (Email.Enabled, Teams.Enabled)
/// 5. InApp sempre incluído
/// </summary>
internal sealed class NotificationRoutingEngine(
    IOptions<NotificationChannelOptions> channelOptions,
    INotificationPreferenceService preferenceService,
    IMandatoryNotificationPolicy mandatoryPolicy,
    ILogger<NotificationRoutingEngine> logger) : INotificationRoutingEngine
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(
        Guid recipientUserId,
        NotificationCategory category,
        NotificationSeverity severity,
        CancellationToken cancellationToken = default)
    {
        var channels = new HashSet<DeliveryChannel> { DeliveryChannel.InApp };
        var options = channelOptions.Value;

        // 1. Canais obrigatórios (o utilizador não pode desativar)
        // Nota: o event type não está disponível nesta interface — a política avalia apenas
        // por categoria e severidade. Regras específicas por event type (ex: BreakGlassActivated)
        // são aplicadas a montante pelo orquestrador.
        var mandatoryChannels = mandatoryPolicy.GetMandatoryChannels(string.Empty, category, severity);
        foreach (var mandatory in mandatoryChannels)
        {
            if (IsChannelAvailable(mandatory, options))
                channels.Add(mandatory);
        }

        // 2. Canais baseados na severidade (regras de roteamento padrão)
        var severityChannels = ResolveSeverityChannels(severity, options);

        // 3. Para canais não obrigatórios, consultar preferências do utilizador
        foreach (var channel in severityChannels)
        {
            if (channels.Contains(channel))
                continue;

            var isEnabled = await preferenceService.IsChannelEnabledAsync(
                recipientUserId, category, channel, cancellationToken);

            if (isEnabled)
                channels.Add(channel);
        }

        logger.LogDebug(
            "Routing resolved for user {UserId}, severity {Severity}, category {Category}: [{Channels}] (mandatory: [{MandatoryChannels}])",
            recipientUserId, severity, category,
            string.Join(", ", channels),
            string.Join(", ", mandatoryChannels));

        return [.. channels];
    }

    /// <summary>
    /// Resolve canais elegíveis com base na severidade (regras padrão da plataforma).
    /// </summary>
    private static List<DeliveryChannel> ResolveSeverityChannels(
        NotificationSeverity severity,
        NotificationChannelOptions options)
    {
        var channels = new List<DeliveryChannel>();

        switch (severity)
        {
            case NotificationSeverity.Critical:
            case NotificationSeverity.Warning:
                if (options.Email.Enabled) channels.Add(DeliveryChannel.Email);
                if (options.Teams.Enabled) channels.Add(DeliveryChannel.MicrosoftTeams);
                break;

            case NotificationSeverity.ActionRequired:
                if (options.Email.Enabled) channels.Add(DeliveryChannel.Email);
                break;

            case NotificationSeverity.Info:
            default:
                break;
        }

        return channels;
    }

    /// <summary>Verifica se o canal está disponível na infraestrutura.</summary>
    private static bool IsChannelAvailable(DeliveryChannel channel, NotificationChannelOptions options) => channel switch
    {
        DeliveryChannel.InApp => true,
        DeliveryChannel.Email => options.Email.Enabled,
        DeliveryChannel.MicrosoftTeams => options.Teams.Enabled,
        _ => false
    };
}
