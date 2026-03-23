using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

public sealed class IncidentNotificationHandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly IncidentNotificationHandler _handler;

    public IncidentNotificationHandlerTests()
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

    [Fact]
    public async Task HandleAsync_IncidentCreated_SubmitsNotification()
    {
        var ownerUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var @event = new IncidentCreatedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "payments-api",
            IncidentSeverity: "Critical",
            Description: "Service is down",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.IncidentCreated);
        r.SourceModule.Should().Be("OperationalIntelligence");
        r.SourceEntityType.Should().Be("Incident");
        r.SourceEntityId.Should().Be(@event.IncidentId.ToString());
        r.TenantId.Should().Be(tenantId);
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_IncidentCreated_MissingOwner_Skips()
    {
        var skipTenantId = Guid.NewGuid();
        var @event = new IncidentCreatedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "api",
            IncidentSeverity: "Warning",
            Description: "Test",
            OwnerUserId: null,
            TenantId: skipTenantId) { TenantId = skipTenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_IncidentCreated_MissingTenant_Skips()
    {
        var @event = new IncidentCreatedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "api",
            IncidentSeverity: "Warning",
            Description: "Test",
            OwnerUserId: Guid.NewGuid(),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_IncidentEscalated_SubmitsNotification()
    {
        var escalatedTenantId = Guid.NewGuid();
        var @event = new IncidentEscalatedIntegrationEvent(
            IncidentId: Guid.NewGuid(),
            ServiceName: "auth-service",
            PreviousSeverity: "Warning",
            NewSeverity: "Critical",
            OwnerUserId: Guid.NewGuid(),
            TenantId: escalatedTenantId) { TenantId = escalatedTenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.IncidentEscalated);
        _captured[0].SourceEntityId.Should().Be(@event.IncidentId.ToString());
    }

    [Fact]
    public async Task HandleAsync_IncidentCreated_IncludesDeepLink()
    {
        var incidentId = Guid.NewGuid();
        var deepLinkTenantId = Guid.NewGuid();
        var @event = new IncidentCreatedIntegrationEvent(
            IncidentId: incidentId,
            ServiceName: "api",
            IncidentSeverity: "Critical",
            Description: "Down",
            OwnerUserId: Guid.NewGuid(),
            TenantId: deepLinkTenantId) { TenantId = deepLinkTenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].ActionUrl.Should().Be($"/incidents/{incidentId}");
    }
}
