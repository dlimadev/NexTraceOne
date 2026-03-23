using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Features.GetPreferences;

/// <summary>
/// Feature: GetPreferences — obtém as preferências de notificação do utilizador autenticado.
/// Para cada combinação de categoria e canal, retorna a preferência explícita ou o default da plataforma,
/// e indica se a combinação é obrigatória (não pode ser desativada pelo utilizador).
/// </summary>
public static class GetPreferences
{
    /// <summary>Query para obter preferências do utilizador autenticado.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que constrói a lista completa de preferências.</summary>
    public sealed class Handler(
        INotificationPreferenceService preferenceService,
        IMandatoryNotificationPolicy mandatoryPolicy,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Guid.TryParse(currentUser.Id, out var userId))
                return Error.Unauthorized(
                    "Notification.InvalidUserId",
                    "Current user identifier is not a valid GUID.");

            var explicitPrefs = await preferenceService.GetPreferencesAsync(userId, cancellationToken);
            var prefsLookup = explicitPrefs.ToDictionary(
                p => (p.Category, p.Channel),
                p => p);

            var preferences = new List<PreferenceDto>();
            var categories = Enum.GetValues<NotificationCategory>();
            var channels = Enum.GetValues<DeliveryChannel>();

            foreach (var category in categories)
            {
                // Obter canais obrigatórios para esta categoria (usa Critical como referência máxima)
                var mandatoryChannels = mandatoryPolicy.GetMandatoryChannels(
                    string.Empty, category, NotificationSeverity.Critical);

                foreach (var channel in channels)
                {
                    var isMandatory = mandatoryChannels.Contains(channel);

                    bool enabled;
                    DateTimeOffset? updatedAt = null;

                    if (prefsLookup.TryGetValue((category, channel), out var pref))
                    {
                        enabled = pref.Enabled;
                        updatedAt = pref.UpdatedAt;
                    }
                    else
                    {
                        enabled = await preferenceService.IsChannelEnabledAsync(
                            userId, category, channel, cancellationToken);
                    }

                    preferences.Add(new PreferenceDto(
                        category.ToString(),
                        channel.ToString(),
                        enabled,
                        isMandatory,
                        updatedAt));
                }
            }

            return new Response(preferences);
        }
    }

    /// <summary>Resposta com a lista completa de preferências.</summary>
    public sealed record Response(IReadOnlyList<PreferenceDto> Preferences);

    /// <summary>DTO de projeção de uma preferência para a API.</summary>
    public sealed record PreferenceDto(
        string Category,
        string Channel,
        bool Enabled,
        bool IsMandatory,
        DateTimeOffset? UpdatedAt);
}
