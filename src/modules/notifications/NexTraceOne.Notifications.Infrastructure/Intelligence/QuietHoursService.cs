using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de quiet hours.
/// Verifica se o utilizador está em período de silêncio e se a entrega deve ser diferida.
/// Notificações obrigatórias (Critical, BreakGlass, Compliance) ignoram quiet hours.
///
/// Configuração por defeito: 22:00–08:00 UTC.
/// Futuro: permitir configuração por utilizador via preferências.
/// </summary>
internal sealed class QuietHoursService : IQuietHoursService
{
    // Período padrão de quiet hours (UTC)
    private const int QuietStartHour = 22;
    private const int QuietEndHour = 8;

    /// <inheritdoc/>
    public Task<bool> ShouldDeferAsync(
        Guid recipientUserId,
        bool isMandatory,
        CancellationToken cancellationToken = default)
    {
        // Notificações obrigatórias nunca são diferidas
        if (isMandatory)
            return Task.FromResult(false);

        var currentHour = DateTimeOffset.UtcNow.Hour;
        var isQuietHours = currentHour >= QuietStartHour || currentHour < QuietEndHour;

        return Task.FromResult(isQuietHours);
    }
}
