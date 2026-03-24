using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Governance.Contracts;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para os handlers de governance expandidos na Fase 5:
/// PolicyViolated, EvidenceExpiring, BudgetThresholdReached.
/// </summary>
public sealed class GovernancePhase5HandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];

    public GovernancePhase5HandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
    }

    // ── PolicyViolated ──

    [Fact]
    public async Task HandleAsync_PolicyViolated_SubmitsNotification()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var ownerUserId = Guid.NewGuid();
        var @event = new IntegrationEvents.PolicyViolatedIntegrationEvent(
            PolicyName: "SecurityHeaders",
            ServiceName: "api-gateway",
            ViolationDescription: "Missing Content-Security-Policy header",
            OwnerUserId: ownerUserId) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.PolicyViolated);
        r.Category.Should().Be(nameof(NotificationCategory.Compliance));
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceModule.Should().Be("Governance");
        r.SourceEntityType.Should().Be("Policy");
        r.RequiresAction.Should().BeTrue();
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.Title.Should().Contain("SecurityHeaders");
        r.Message.Should().Contain("api-gateway");
        r.Message.Should().Contain("Missing Content-Security-Policy");
    }

    [Fact]
    public async Task HandleAsync_PolicyViolated_MissingOwner_Skips()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.PolicyViolatedIntegrationEvent(
            PolicyName: "Test",
            ServiceName: "svc",
            ViolationDescription: "desc",
            OwnerUserId: null) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── EvidenceExpiring ──

    [Fact]
    public async Task HandleAsync_EvidenceExpiring_SubmitsNotification()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var ownerUserId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(5);
        var @event = new IntegrationEvents.EvidenceExpiringIntegrationEvent(
            EvidenceId: evidenceId,
            EvidenceName: "SOC2 Audit Report",
            ServiceName: "billing-service",
            ExpiresAt: expiresAt,
            OwnerUserId: ownerUserId) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.EvidenceExpiring);
        r.Severity.Should().Be(nameof(NotificationSeverity.ActionRequired));
        r.SourceEntityType.Should().Be("Evidence");
        r.SourceEntityId.Should().Be(evidenceId.ToString());
        r.ActionUrl.Should().Be($"/governance/evidence/{evidenceId}");
        r.RequiresAction.Should().BeTrue();
        r.Title.Should().Contain("SOC2 Audit Report");
        r.Message.Should().Contain("billing-service");
    }

    [Fact]
    public async Task HandleAsync_EvidenceExpiring_MissingTenant_Skips()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.EvidenceExpiringIntegrationEvent(
            EvidenceId: Guid.NewGuid(),
            EvidenceName: "Report",
            ServiceName: "svc",
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(1),
            OwnerUserId: Guid.NewGuid()) { TenantId = null };

        await handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── BudgetThresholdReached ──

    [Fact]
    public async Task HandleAsync_BudgetThresholdReached_80Percent_SubmitsActionRequired()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var ownerUserId = Guid.NewGuid();
        var @event = new IntegrationEvents.BudgetThresholdReachedIntegrationEvent(
            ServiceName: "data-pipeline",
            ThresholdPercent: 80,
            CurrentSpend: 800.00m,
            BudgetLimit: 1000.00m,
            OwnerUserId: ownerUserId) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.BudgetThresholdReached);
        r.Category.Should().Be(nameof(NotificationCategory.FinOps));
        r.Severity.Should().Be(nameof(NotificationSeverity.ActionRequired));
        r.RequiresAction.Should().BeFalse();
        r.Title.Should().Contain("80%");
        r.Title.Should().Contain("data-pipeline");
        r.Message.Should().Contain("$800");
        r.Message.Should().Contain("$1,000");
    }

    [Fact]
    public async Task HandleAsync_BudgetThresholdReached_90Percent_SubmitsWarning()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.BudgetThresholdReachedIntegrationEvent(
            ServiceName: "analytics",
            ThresholdPercent: 90,
            CurrentSpend: 900.00m,
            BudgetLimit: 1000.00m,
            OwnerUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].Severity.Should().Be(nameof(NotificationSeverity.Warning));
        _captured[0].RequiresAction.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_BudgetThresholdReached_100Percent_SubmitsCritical()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.BudgetThresholdReachedIntegrationEvent(
            ServiceName: "ml-pipeline",
            ThresholdPercent: 100,
            CurrentSpend: 1200.00m,
            BudgetLimit: 1000.00m,
            OwnerUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].Severity.Should().Be(nameof(NotificationSeverity.Critical));
        _captured[0].RequiresAction.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_BudgetThresholdReached_MissingOwner_Skips()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.BudgetThresholdReachedIntegrationEvent(
            ServiceName: "svc",
            ThresholdPercent: 80,
            CurrentSpend: 80m,
            BudgetLimit: 100m,
            OwnerUserId: null) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
