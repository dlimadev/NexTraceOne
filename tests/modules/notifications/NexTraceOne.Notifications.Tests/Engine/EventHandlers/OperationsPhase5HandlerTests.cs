using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para os handlers de operações expandidos na Fase 5:
/// IncidentResolved, AnomalyDetected, HealthDegradation.
/// </summary>
public sealed class OperationsPhase5HandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly IncidentNotificationHandler _handler;

    public OperationsPhase5HandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new IncidentNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<IncidentNotificationHandler>());
    }

    // ── IncidentResolved ──

    [Fact]
    public async Task HandleAsync_IncidentResolved_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var @event = new IncidentResolvedIntegrationEvent(
            IncidentId: incidentId,
            ServiceName: "payments-api",
            ResolvedBy: "john.doe",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.IncidentResolved);
        r.Category.Should().Be(nameof(NotificationCategory.Incident));
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceModule.Should().Be("OperationalIntelligence");
        r.SourceEntityType.Should().Be("Incident");
        r.SourceEntityId.Should().Be(incidentId.ToString());
        r.TenantId.Should().Be(tenantId);
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.RequiresAction.Should().BeFalse();
        r.ActionUrl.Should().Be($"/incidents/{incidentId}");
        r.Title.Should().Contain("payments-api");
        r.Message.Should().Contain("john.doe");
    }

    [Fact]
    public async Task HandleAsync_IncidentResolved_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new IncidentResolvedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "api",
            ResolvedBy: "admin",
            OwnerUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_IncidentResolved_MissingTenant_Skips()
    {
        var @event = new IncidentResolvedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "api",
            ResolvedBy: "admin",
            OwnerUserId: Guid.NewGuid(),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── AnomalyDetected ──

    [Fact]
    public async Task HandleAsync_AnomalyDetected_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var anomalyId = Guid.NewGuid();
        var @event = new AnomalyDetectedIntegrationEvent(
            AnomalyId: anomalyId,
            ServiceName: "auth-service",
            AnomalyType: "Runtime",
            Description: "Latency spike detected",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.AnomalyDetected);
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceEntityType.Should().Be("Anomaly");
        r.SourceEntityId.Should().Be(anomalyId.ToString());
        r.ActionUrl.Should().Be($"/operations/anomalies/{anomalyId}");
        r.RequiresAction.Should().BeTrue();
        r.Title.Should().Contain("auth-service");
        r.Message.Should().Contain("runtime");
        r.Message.Should().Contain("Latency spike detected");
    }

    [Fact]
    public async Task HandleAsync_AnomalyDetected_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new AnomalyDetectedIntegrationEvent(
            AnomalyId: Guid.NewGuid(),
            ServiceName: "api",
            AnomalyType: "Drift",
            Description: "Config drift",
            OwnerUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── HealthDegradation ──

    [Fact]
    public async Task HandleAsync_HealthDegradation_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var @event = new HealthDegradationIntegrationEvent(
            ServiceId: serviceId,
            ServiceName: "billing-api",
            PreviousStatus: "Healthy",
            CurrentStatus: "Degraded",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.HealthDegradation);
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceEntityType.Should().Be("Service");
        r.SourceEntityId.Should().Be(serviceId.ToString());
        r.ActionUrl.Should().Be($"/services/{serviceId}/health");
        r.RequiresAction.Should().BeTrue();
        r.Title.Should().Contain("billing-api");
        r.Message.Should().Contain("Healthy");
        r.Message.Should().Contain("Degraded");
    }

    [Fact]
    public async Task HandleAsync_HealthDegradation_MissingTenant_Skips()
    {
        var @event = new HealthDegradationIntegrationEvent(
            ServiceId: Guid.NewGuid(),
            ServiceName: "api",
            PreviousStatus: "Healthy",
            CurrentStatus: "Degraded",
            OwnerUserId: Guid.NewGuid(),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
