using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Observability;

/// <summary>
/// Forwards DashboardUsageEvent to ClickHouse (tabela dedicada gov_dashboard_usage).
/// Fase 4: usa WriteDashboardUsageEventAsync em vez de WriteProductEventAsync.
/// Failures são suprimidos — o commit de domínio nunca é bloqueado por writes analíticos.
/// </summary>
internal sealed class DashboardUsageForwarder(
    IAnalyticsWriter analyticsWriter,
    ILogger<DashboardUsageForwarder> logger) : IDashboardUsageForwarder
{
    public async Task ForwardAsync(DashboardUsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = new DashboardUsageEventRecord(
                Id: usageEvent.Id.Value,
                TenantId: Guid.TryParse(usageEvent.TenantId, out var tid) ? tid : Guid.Empty,
                DashboardId: usageEvent.DashboardId,
                UserId: usageEvent.UserId,
                Persona: usageEvent.Persona,
                EventType: usageEvent.EventType,
                DurationSeconds: usageEvent.DurationSeconds,
                OccurredAt: usageEvent.OccurredAt);

            await analyticsWriter.WriteDashboardUsageEventAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to forward DashboardUsageEvent {EventId} to analytics store — suppressed",
                usageEvent.Id.Value);
        }
    }
}
