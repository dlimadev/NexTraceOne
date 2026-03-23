using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Governance.Contracts;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

public sealed class ComplianceAndBudgetHandlerTests
{
    private readonly List<NotificationRequest> _captured = [];
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();

    public ComplianceAndBudgetHandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
    }

    [Fact]
    public async Task ComplianceCheckFailed_SubmitsNotification()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.ComplianceCheckFailedIntegrationEvent(
            ReportId: "RPT-001",
            ServiceName: "billing-api",
            GapCount: 3,
            OwnerUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.ComplianceCheckFailed);
        _captured[0].SourceModule.Should().Be("Governance");
        _captured[0].SourceEntityId.Should().Be("RPT-001");
        _captured[0].Title.Should().Contain("billing-api");
        _captured[0].Message.Should().Contain("3");
    }

    [Fact]
    public async Task BudgetExceeded_SubmitsNotification()
    {
        var handler = new BudgetNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<BudgetNotificationHandler>());

        var @event = new BudgetExceededIntegrationEvent(
            AnomalyId: Guid.NewGuid(),
            ServiceName: "data-pipeline",
            ExpectedCost: 500.00m,
            ActualCost: 1200.00m,
            OwnerUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.BudgetExceeded);
        _captured[0].SourceModule.Should().Be("OperationalIntelligence");
        _captured[0].Title.Should().Contain("data-pipeline");
        _captured[0].Message.Should().Contain("$500");
        _captured[0].Message.Should().Contain("$1,200");
    }

    [Fact]
    public async Task IntegrationFailed_SubmitsNotification()
    {
        var handler = new IntegrationFailureNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<IntegrationFailureNotificationHandler>());

        var integrationId = Guid.NewGuid();
        var @event = new IntegrationFailedIntegrationEvent(
            IntegrationId: integrationId,
            IntegrationName: "Azure DevOps",
            ErrorMessage: "Connection timeout after 30s",
            OwnerUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.IntegrationFailed);
        _captured[0].SourceEntityId.Should().Be(integrationId.ToString());
        _captured[0].Title.Should().Contain("Azure DevOps");
        _captured[0].Message.Should().Contain("Connection timeout");
    }

    [Fact]
    public async Task ComplianceCheckFailed_MissingOwner_Skips()
    {
        var handler = new ComplianceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ComplianceNotificationHandler>());

        var @event = new IntegrationEvents.ComplianceCheckFailedIntegrationEvent(
            ReportId: "RPT",
            ServiceName: "svc",
            GapCount: 1,
            OwnerUserId: null) { TenantId = Guid.NewGuid() };

        await handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task BudgetExceeded_MissingTenant_Skips()
    {
        var handler = new BudgetNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<BudgetNotificationHandler>());

        var @event = new BudgetExceededIntegrationEvent(
            AnomalyId: Guid.NewGuid(),
            ServiceName: "svc",
            ExpectedCost: 100m,
            ActualCost: 200m,
            OwnerUserId: Guid.NewGuid()) { TenantId = null };

        await handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
