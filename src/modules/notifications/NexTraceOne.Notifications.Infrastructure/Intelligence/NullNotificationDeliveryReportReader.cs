using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação null (honest-null) de INotificationDeliveryReportReader.
/// Retorna dados vazios — sem bridge real configurado.
/// Wave AK.2 — GetNotificationDeliveryReport.
/// </summary>
public sealed class NullNotificationDeliveryReportReader : INotificationDeliveryReportReader
{
    public Task<INotificationDeliveryReportReader.DeliveryReportData> GetDeliveryDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new INotificationDeliveryReportReader.DeliveryReportData([], [], []));
    }
}
