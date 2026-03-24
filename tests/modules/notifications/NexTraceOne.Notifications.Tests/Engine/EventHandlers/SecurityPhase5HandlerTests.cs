using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para os handlers de segurança expandidos na Fase 5:
/// UserRoleChanged, JitAccessGranted, AccessReviewPending.
/// </summary>
public sealed class SecurityPhase5HandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly SecurityNotificationHandler _handler;

    public SecurityPhase5HandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new SecurityNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<SecurityNotificationHandler>());
    }

    // ── UserRoleChanged ──

    [Fact]
    public async Task HandleAsync_UserRoleChanged_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var @event = new UserRoleChangedIntegrationEvent(
            UserId: userId,
            TenantId: tenantId,
            RoleName: "TechLead") { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.UserRoleChanged);
        r.Category.Should().Be(nameof(NotificationCategory.Security));
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceModule.Should().Be("Identity");
        r.SourceEntityType.Should().Be("UserRole");
        r.SourceEntityId.Should().Be(userId.ToString());
        r.ActionUrl.Should().Be("/settings/profile");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(userId);
        r.Title.Should().Contain("TechLead");
    }

    [Fact]
    public async Task HandleAsync_UserRoleChanged_MissingTenant_Skips()
    {
        var @event = new UserRoleChangedIntegrationEvent(
            UserId: Guid.NewGuid(),
            TenantId: null,
            RoleName: "Admin") { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── JitAccessGranted ──

    [Fact]
    public async Task HandleAsync_JitAccessGranted_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(4);
        var @event = new JitAccessGrantedIntegrationEvent(
            UserId: userId,
            Resource: "production-db",
            GrantedBy: "security-admin",
            ExpiresAt: expiresAt,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.JitAccessGranted);
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceEntityType.Should().Be("JitAccess");
        r.ActionUrl.Should().Be("/security/jit-access");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(userId);
        r.Title.Should().Contain("production-db");
        r.Message.Should().Contain("security-admin");
    }

    [Fact]
    public async Task HandleAsync_JitAccessGranted_MissingTenant_Skips()
    {
        var @event = new JitAccessGrantedIntegrationEvent(
            UserId: Guid.NewGuid(),
            Resource: "resource",
            GrantedBy: "admin",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── AccessReviewPending ──

    [Fact]
    public async Task HandleAsync_AccessReviewPending_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var assigneeUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var dueDate = DateTimeOffset.UtcNow.AddDays(7);
        var @event = new AccessReviewPendingIntegrationEvent(
            ReviewId: reviewId,
            ReviewScope: "Production access",
            DueDate: dueDate,
            AssigneeUserId: assigneeUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.AccessReviewPending);
        r.Severity.Should().Be(nameof(NotificationSeverity.ActionRequired));
        r.SourceEntityType.Should().Be("AccessReview");
        r.SourceEntityId.Should().Be(reviewId.ToString());
        r.ActionUrl.Should().Be($"/security/access-reviews/{reviewId}");
        r.RequiresAction.Should().BeTrue();
        r.RecipientUserIds.Should().Contain(assigneeUserId);
        r.Title.Should().Contain("Production access");
    }

    [Fact]
    public async Task HandleAsync_AccessReviewPending_MissingAssignee_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new AccessReviewPendingIntegrationEvent(
            ReviewId: Guid.NewGuid(),
            ReviewScope: "test",
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            AssigneeUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
