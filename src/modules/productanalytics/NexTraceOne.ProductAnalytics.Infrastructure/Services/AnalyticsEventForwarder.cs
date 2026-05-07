using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IAnalyticsEventForwarder"/> que delega para
/// <see cref="IAnalyticsWriter"/> do building block de observabilidade.
///
/// Segue o mesmo padrão de TraceCorrelationAnalyticsWriter (ChangeGovernance).
/// Quando Analytics:Enabled = false, o IAnalyticsWriter resolvido é o NullAnalyticsWriter,
/// garantindo graceful degradation sem alteração neste adapter.
/// </summary>
internal sealed class AnalyticsEventForwarder(IAnalyticsWriter analyticsWriter) : IAnalyticsEventForwarder
{
    public Task ForwardAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        var record = new ProductAnalyticsRecord(
            Id: analyticsEvent.Id.Value,
            TenantId: analyticsEvent.TenantId,
            UserId: Guid.TryParse(analyticsEvent.UserId, out var uid) ? uid : Guid.Empty,
            Persona: analyticsEvent.Persona ?? string.Empty,
            Module: analyticsEvent.Module.ToString(),
            EventType: (byte)analyticsEvent.EventType,
            Feature: analyticsEvent.Feature ?? string.Empty,
            EntityType: analyticsEvent.EntityType ?? string.Empty,
            Outcome: analyticsEvent.Outcome ?? string.Empty,
            Route: analyticsEvent.Route,
            TeamId: Guid.TryParse(analyticsEvent.TeamId, out var tid) ? tid : null,
            DomainId: Guid.TryParse(analyticsEvent.DomainId, out var did) ? did : null,
            SessionId: analyticsEvent.SessionId ?? string.Empty,
            ClientType: analyticsEvent.ClientType ?? string.Empty,
            MetadataJson: analyticsEvent.MetadataJson ?? "{}",
            OccurredAt: analyticsEvent.OccurredAt,
            EnvironmentId: null,
            DurationMs: null,
            ParentEventId: null,
            Source: "platform");

        return analyticsWriter.WriteProductEventAsync(record, cancellationToken);
    }
}
