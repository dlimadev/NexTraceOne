using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de quiet hours.
/// Verifica se o utilizador está em período de silêncio e se a entrega deve ser diferida.
/// Notificações obrigatórias (Critical, BreakGlass, Compliance) ignoram quiet hours.
///
/// Configuração por utilizador (scope=User) via <see cref="IConfigurationResolutionService"/>:
///   - <c>notifications.quiet_hours.enabled</c> (bool, default: true no sistema, false por utilizador)
///   - <c>notifications.quiet_hours.start</c>    (string HH:mm, default: "22:00")
///   - <c>notifications.quiet_hours.end</c>      (string HH:mm, default: "08:00")
///   - <c>notifications.quiet_hours.timezone</c> (string IANA tz, default: "UTC")
///
/// Se a configuração não estiver disponível, usa os valores default.
/// </summary>
internal sealed class QuietHoursService(
    IConfigurationResolutionService configResolution) : IQuietHoursService
{
    private const string DefaultStart = "22:00";
    private const string DefaultEnd = "08:00";
    private const string DefaultTimezone = "UTC";

    /// <inheritdoc/>
    public async Task<bool> ShouldDeferAsync(
        Guid recipientUserId,
        bool isMandatory,
        CancellationToken cancellationToken = default)
    {
        // Notificações obrigatórias nunca são diferidas
        if (isMandatory)
            return false;

        var userId = recipientUserId.ToString();

        // Verificar se quiet hours estão habilitadas para este utilizador
        var enabledDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.quiet_hours.enabled",
            ConfigurationScope.User,
            userId,
            cancellationToken);

        // Se o utilizador não configurou e o sistema tem "true" como default, activar quiet hours
        var quietHoursEnabled = enabledDto is null
            || !bool.TryParse(enabledDto.EffectiveValue, out var enabled)
            || enabled;

        if (!quietHoursEnabled)
            return false;

        // Ler horário e timezone configurados
        var startDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.quiet_hours.start", ConfigurationScope.User, userId, cancellationToken);
        var endDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.quiet_hours.end", ConfigurationScope.User, userId, cancellationToken);
        var tzDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.quiet_hours.timezone", ConfigurationScope.User, userId, cancellationToken);

        var startStr = startDto?.EffectiveValue ?? DefaultStart;
        var endStr = endDto?.EffectiveValue ?? DefaultEnd;
        var tzId = tzDto?.EffectiveValue ?? DefaultTimezone;

        if (!TryParseHHmm(startStr, out var startHour, out var startMinute)
            || !TryParseHHmm(endStr, out var endHour, out var endMinute))
            return false;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var nowMinutes = localNow.Hour * 60 + localNow.Minute;
        var startMinutes = startHour * 60 + startMinute;
        var endMinutes = endHour * 60 + endMinute;

        // Suporta janela que cruza meia-noite (ex: 22:00–08:00)
        bool isQuietHours;
        if (startMinutes > endMinutes)
            isQuietHours = nowMinutes >= startMinutes || nowMinutes < endMinutes;
        else
            isQuietHours = nowMinutes >= startMinutes && nowMinutes < endMinutes;

        return isQuietHours;
    }

    private static bool TryParseHHmm(string value, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':');
        if (parts.Length == 2
            && int.TryParse(parts[0], out hour)
            && int.TryParse(parts[1], out minute)
            && hour is >= 0 and <= 23
            && minute is >= 0 and <= 59)
            return true;

        // Fallback para formato apenas inteiro (hora sem minutos)
        if (int.TryParse(value, out hour) && hour is >= 0 and <= 23)
        {
            minute = 0;
            return true;
        }

        return false;
    }
}
