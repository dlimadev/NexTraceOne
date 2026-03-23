using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

public sealed class SecurityNotificationHandlerTests
{
    private readonly List<NotificationRequest> _captured = [];
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly SecurityNotificationHandler _handler;

    public SecurityNotificationHandlerTests()
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

    [Fact]
    public async Task HandleAsync_BreakGlass_SubmitsCriticalNotification()
    {
        var userId = Guid.NewGuid();
        var @event = new BreakGlassActivatedIntegrationEvent(
            UserId: userId,
            ActivatedBy: "admin.user",
            Resource: "production-db",
            Reason: "Emergency recovery") { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.BreakGlassActivated);
        _captured[0].SourceModule.Should().Be("Identity");
        _captured[0].RecipientUserIds.Should().Contain(userId);
        _captured[0].RequiresAction.Should().BeTrue();
        _captured[0].Title.Should().Contain("Break-glass");
        _captured[0].Message.Should().Contain("admin.user");
    }

    [Fact]
    public async Task HandleAsync_BreakGlass_MissingTenant_Skips()
    {
        var @event = new BreakGlassActivatedIntegrationEvent(
            UserId: Guid.NewGuid(),
            ActivatedBy: "user",
            Resource: "db",
            Reason: "reason") { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_BreakGlass_IncludesDeepLink()
    {
        var userId = Guid.NewGuid();
        var @event = new BreakGlassActivatedIntegrationEvent(
            UserId: userId,
            ActivatedBy: "admin",
            Resource: "prod-db",
            Reason: "Emergency") { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].ActionUrl.Should().Be($"/security/break-glass/{userId}");
    }
}
