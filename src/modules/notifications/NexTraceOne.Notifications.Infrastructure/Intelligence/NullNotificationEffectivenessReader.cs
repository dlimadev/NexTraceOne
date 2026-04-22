using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação null (honest-null) de INotificationEffectivenessReader.
/// Retorna dados vazios — sem bridge real configurado.
/// Wave AK.3 — GetNotificationEffectivenessReport.
/// </summary>
public sealed class NullNotificationEffectivenessReader : INotificationEffectivenessReader
{
    public Task<IReadOnlyList<INotificationEffectivenessReader.EventTypeEffectivenessData>> GetEffectivenessDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        int actionWindowHours,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<INotificationEffectivenessReader.EventTypeEffectivenessData>>([]);
    }
}
