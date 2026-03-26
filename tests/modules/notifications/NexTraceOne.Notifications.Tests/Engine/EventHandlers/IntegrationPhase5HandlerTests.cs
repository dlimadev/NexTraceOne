using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;
using IntegrationsContracts = NexTraceOne.Integrations.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para os handlers de integração expandidos na Fase 5:
/// SyncFailed, ConnectorAuthFailed.
/// P2.5: SyncFailedIntegrationEvent e ConnectorAuthFailedIntegrationEvent agora usam
///       NexTraceOne.Integrations.Contracts (ownership correto: módulo Integrations).
/// </summary>
public sealed class IntegrationPhase5HandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly IntegrationFailureNotificationHandler _handler;

    public IntegrationPhase5HandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new IntegrationFailureNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<IntegrationFailureNotificationHandler>());
    }

    // ── SyncFailed ──

    [Fact]
    public async Task HandleAsync_SyncFailed_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();
        var @event = new IntegrationsContracts.SyncFailedIntegrationEvent(
            IntegrationId: integrationId,
            IntegrationName: "Jira Sync",
            ErrorMessage: "Remote API returned 429 Too Many Requests",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.SyncFailed);
        r.Category.Should().Be(nameof(NotificationCategory.Integration));
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceModule.Should().Be("Integrations");
        r.SourceEntityType.Should().Be("Integration");
        r.SourceEntityId.Should().Be(integrationId.ToString());
        r.ActionUrl.Should().Be($"/integrations/{integrationId}/sync");
        r.RequiresAction.Should().BeTrue();
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.Title.Should().Contain("Jira Sync");
        r.Message.Should().Contain("429 Too Many Requests");
    }

    [Fact]
    public async Task HandleAsync_SyncFailed_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new IntegrationsContracts.SyncFailedIntegrationEvent(
            IntegrationId: Guid.NewGuid(),
            IntegrationName: "Sync",
            ErrorMessage: "Error",
            OwnerUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SyncFailed_MissingTenant_Skips()
    {
        var @event = new IntegrationsContracts.SyncFailedIntegrationEvent(
            IntegrationId: Guid.NewGuid(),
            IntegrationName: "Sync",
            ErrorMessage: "Error",
            OwnerUserId: Guid.NewGuid(),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── ConnectorAuthFailed ──

    [Fact]
    public async Task HandleAsync_ConnectorAuthFailed_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var @event = new IntegrationsContracts.ConnectorAuthFailedIntegrationEvent(
            ConnectorId: connectorId,
            ConnectorName: "Azure DevOps Connector",
            ErrorMessage: "OAuth token expired and refresh failed",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.ConnectorAuthFailed);
        r.Severity.Should().Be(nameof(NotificationSeverity.Critical));
        r.SourceEntityType.Should().Be("Connector");
        r.SourceEntityId.Should().Be(connectorId.ToString());
        r.ActionUrl.Should().Be($"/integrations/connectors/{connectorId}");
        r.RequiresAction.Should().BeTrue();
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.Title.Should().Contain("Azure DevOps Connector");
        r.Message.Should().Contain("OAuth token expired");
    }

    [Fact]
    public async Task HandleAsync_ConnectorAuthFailed_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new IntegrationsContracts.ConnectorAuthFailedIntegrationEvent(
            ConnectorId: Guid.NewGuid(),
            ConnectorName: "Connector",
            ErrorMessage: "Error",
            OwnerUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
