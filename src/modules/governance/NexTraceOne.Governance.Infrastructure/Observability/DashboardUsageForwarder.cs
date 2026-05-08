using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Observability;

/// <summary>
/// Forwards DashboardUsageEvent to the analytics store (Elastic or ClickHouse)
/// by mapping it to a ProductAnalyticsRecord.
/// Failures are suppressed — the domain commit is never blocked by analytics writes.
/// </summary>
internal sealed class DashboardUsageForwarder(
    IAnalyticsWriter analyticsWriter,
    ILogger<DashboardUsageForwarder> logger) : IDashboardUsageForwarder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task ForwardAsync(DashboardUsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                dashboardId = usageEvent.DashboardId,
                eventType = usageEvent.EventType,
                durationSeconds = usageEvent.DurationSeconds
            }, JsonOptions);

            var record = new ProductAnalyticsRecord(
                Id: usageEvent.Id.Value,
                TenantId: Guid.TryParse(usageEvent.TenantId, out var tid) ? tid : Guid.Empty,
                UserId: Guid.TryParse(usageEvent.UserId, out var uid) ? uid : Guid.Empty,
                Persona: usageEvent.Persona ?? string.Empty,
                Module: "Governance",
                EventType: MapEventTypeToByte(usageEvent.EventType),
                Feature: "Dashboard",
                EntityType: "Dashboard",
                Outcome: usageEvent.EventType,
                Route: $"/dashboards/{usageEvent.DashboardId}",
                TeamId: null,
                DomainId: null,
                SessionId: string.Empty,
                ClientType: string.Empty,
                MetadataJson: metadata,
                OccurredAt: usageEvent.OccurredAt,
                EnvironmentId: null,
                DurationMs: usageEvent.DurationSeconds.HasValue
                    ? (uint)(usageEvent.DurationSeconds.Value * 1000)
                    : null,
                ParentEventId: null,
                Source: "Governance.DashboardUsage");

            await analyticsWriter.WriteProductEventAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to forward DashboardUsageEvent {EventId} to analytics store — suppressed",
                usageEvent.Id.Value);
        }
    }

    private static byte MapEventTypeToByte(string eventType) => eventType switch
    {
        "view" => 1,
        "export" => 2,
        "embed" => 3,
        "share" => 4,
        "snapshot" => 5,
        _ => 0
    };
}
