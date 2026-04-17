using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para o ChangeIntelligenceNotificationHandler —
/// cobre os 7 tipos de evento de Change Intelligence:
/// PromotionCompleted, PromotionBlocked, RollbackTriggered, DeploymentCompleted,
/// ChangeConfidenceScored, BlastRadiusHigh, PostChangeVerificationFailed.
/// TenantId é propriedade init-only da classe base IntegrationEventBase e deve
/// ser definida via object initializer { TenantId = tenantId }.
/// </summary>
public sealed class ChangeIntelligenceNotificationHandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly ChangeIntelligenceNotificationHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _changeId = Guid.NewGuid();

    public ChangeIntelligenceNotificationHandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });

        _handler = new ChangeIntelligenceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ChangeIntelligenceNotificationHandler>());
    }

    // ── PromotionCompleted ────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PromotionCompleted_SubmitsInfoNotification()
    {
        var promotionId = Guid.NewGuid();
        var @event = new PromotionCompletedIntegrationEvent(
            PromotionId: promotionId,
            ServiceName: "order-api",
            TargetEnvironment: "production",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.PromotionCompleted);
        req.Severity.Should().Be(nameof(NotificationSeverity.Info));
        req.RequiresAction.Should().BeFalse();
        req.TenantId.Should().Be(_tenantId);
        req.RecipientUserIds.Should().Contain(_ownerUserId);
    }

    [Fact]
    public async Task HandleAsync_PromotionCompleted_MissingOwner_Skips()
    {
        var @event = new PromotionCompletedIntegrationEvent(
            PromotionId: _changeId,
            ServiceName: "order-api",
            TargetEnvironment: "production",
            OwnerUserId: null) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
        await _module.DidNotReceive().SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PromotionCompleted_MissingTenant_Skips()
    {
        // TenantId omitted — base class defaults to null
        var @event = new PromotionCompletedIntegrationEvent(
            PromotionId: _changeId,
            ServiceName: "order-api",
            TargetEnvironment: "production",
            OwnerUserId: _ownerUserId);

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── PromotionBlocked ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PromotionBlocked_SubmitsWarningAndRequiresAction()
    {
        var promotionId = Guid.NewGuid();
        var @event = new PromotionBlockedIntegrationEvent(
            PromotionId: promotionId,
            ServiceName: "payment-service",
            TargetEnvironment: "production",
            Reason: "Failed quality gate: coverage < 80%",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.PromotionBlocked);
        req.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        req.RequiresAction.Should().BeTrue();
        req.Message.Should().Contain("Failed quality gate");
    }

    [Fact]
    public async Task HandleAsync_PromotionBlocked_MissingOwner_Skips()
    {
        var @event = new PromotionBlockedIntegrationEvent(
            PromotionId: _changeId,
            ServiceName: "payment-service",
            TargetEnvironment: "production",
            Reason: "Gate failure",
            OwnerUserId: null) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── RollbackTriggered ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_RollbackTriggered_SubmitsCriticalAndRequiresAction()
    {
        var @event = new RollbackTriggeredIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "auth-service",
            EnvironmentName: "production",
            Reason: "Error rate > 5% after deploy",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.RollbackTriggered);
        req.Severity.Should().Be(nameof(NotificationSeverity.Critical));
        req.RequiresAction.Should().BeTrue();
        req.SourceEntityId.Should().Be(_changeId.ToString());
    }

    [Fact]
    public async Task HandleAsync_RollbackTriggered_MissingTenant_Skips()
    {
        var @event = new RollbackTriggeredIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "auth-service",
            EnvironmentName: "production",
            Reason: "Error rate spike",
            OwnerUserId: _ownerUserId);

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── DeploymentCompleted ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DeploymentCompleted_Success_SubmitsInfoAndNoAction()
    {
        var @event = new DeploymentCompletedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "catalog-api",
            EnvironmentName: "staging",
            IsSuccess: true,
            FailureReason: null,
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.DeploymentCompleted);
        req.Severity.Should().Be(nameof(NotificationSeverity.Info));
        req.RequiresAction.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_DeploymentCompleted_Failure_SubmitsWarningAndRequiresAction()
    {
        var @event = new DeploymentCompletedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "catalog-api",
            EnvironmentName: "staging",
            IsSuccess: false,
            FailureReason: "Health check failed",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        req.RequiresAction.Should().BeTrue();
        req.Message.Should().Contain("Health check failed");
    }

    [Fact]
    public async Task HandleAsync_DeploymentCompleted_MissingOwner_Skips()
    {
        var @event = new DeploymentCompletedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "catalog-api",
            EnvironmentName: "staging",
            IsSuccess: true,
            FailureReason: null,
            OwnerUserId: null) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── ChangeConfidenceScored ────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ChangeConfidenceScored_SubmitsWarningAndRequiresAction()
    {
        var @event = new ChangeConfidenceScoredIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "reporting-service",
            ConfidenceScore: 0.42m,
            EnvironmentName: "pre-production",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.ChangeConfidenceScored);
        req.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        req.RequiresAction.Should().BeTrue();
        req.Message.Should().Contain("42%");
    }

    [Fact]
    public async Task HandleAsync_ChangeConfidenceScored_MissingTenant_Skips()
    {
        var @event = new ChangeConfidenceScoredIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "reporting-service",
            ConfidenceScore: 0.42m,
            EnvironmentName: "pre-production",
            OwnerUserId: _ownerUserId);

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── BlastRadiusHigh ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_BlastRadiusHigh_SubmitsWarningAndRequiresAction()
    {
        var @event = new BlastRadiusHighIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "core-service",
            AffectedServiceCount: 12,
            EnvironmentName: "production",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.BlastRadiusHigh);
        req.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        req.RequiresAction.Should().BeTrue();
        req.Message.Should().Contain("12");
    }

    [Fact]
    public async Task HandleAsync_BlastRadiusHigh_MissingOwner_Skips()
    {
        var @event = new BlastRadiusHighIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "core-service",
            AffectedServiceCount: 12,
            EnvironmentName: "production",
            OwnerUserId: null) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── PostChangeVerificationFailed ──────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PostChangeVerificationFailed_SubmitsCriticalAndRequiresAction()
    {
        var @event = new PostChangeVerificationFailedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "inventory-api",
            EnvironmentName: "production",
            FailureReason: "SLO error rate exceeded",
            OwnerUserId: _ownerUserId) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var req = _captured[0];
        req.EventType.Should().Be(NotificationType.PostChangeVerificationFailed);
        req.Severity.Should().Be(nameof(NotificationSeverity.Critical));
        req.RequiresAction.Should().BeTrue();
        req.Message.Should().Contain("SLO error rate exceeded");
    }

    [Fact]
    public async Task HandleAsync_PostChangeVerificationFailed_MissingOwner_Skips()
    {
        var @event = new PostChangeVerificationFailedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "inventory-api",
            EnvironmentName: "production",
            FailureReason: "SLO error rate exceeded",
            OwnerUserId: null) { TenantId = _tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PostChangeVerificationFailed_MissingTenant_Skips()
    {
        var @event = new PostChangeVerificationFailedIntegrationEvent(
            ChangeId: _changeId,
            ServiceName: "inventory-api",
            EnvironmentName: "production",
            FailureReason: "SLO error rate exceeded",
            OwnerUserId: _ownerUserId);

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
