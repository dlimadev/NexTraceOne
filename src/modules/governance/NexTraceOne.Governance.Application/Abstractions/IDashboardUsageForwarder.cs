using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Port for forwarding DashboardUsageEvent to the analytics store (Elastic/ClickHouse).
/// Implemented in Infrastructure — Application layer stays free of analytics dependencies.
/// Fire-and-forget: failures are suppressed so the domain is never blocked.
/// </summary>
public interface IDashboardUsageForwarder
{
    Task ForwardAsync(DashboardUsageEvent usageEvent, CancellationToken cancellationToken = default);
}
