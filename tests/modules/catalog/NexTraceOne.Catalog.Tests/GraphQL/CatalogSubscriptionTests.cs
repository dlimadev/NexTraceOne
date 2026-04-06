using HotChocolate.Subscriptions;
using NexTraceOne.Catalog.API.GraphQL;
using NexTraceOne.Catalog.API.GraphQL.Publishers;

namespace NexTraceOne.Catalog.Tests.GraphQL;

/// <summary>
/// Testes unitários para GraphQL Subscriptions real-time — Phase 5.3.
/// Cobrem: CatalogSubscription types, GraphQLTopics constants e GraphQLEventPublisher.
/// </summary>
public sealed class CatalogSubscriptionTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 14, 0, 0, TimeSpan.Zero);

    // ─────────────────────────────────────────────────────────────────────
    // ChangeEventNotification
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeEventNotification_ShouldHoldAllFields()
    {
        var changeId = Guid.NewGuid();
        var notification = new ChangeEventNotification
        {
            ChangeId = changeId,
            ServiceName = "order-service",
            Environment = "Production",
            EventType = "deployed",
            TeamName = "Team Alpha",
            OccurredAt = FixedNow,
            Version = "1.2.3"
        };

        notification.ChangeId.Should().Be(changeId);
        notification.ServiceName.Should().Be("order-service");
        notification.Environment.Should().Be("Production");
        notification.EventType.Should().Be("deployed");
        notification.Version.Should().Be("1.2.3");
        notification.OccurredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void ChangeEventNotification_Version_ShouldBeOptional()
    {
        var notification = new ChangeEventNotification
        {
            ChangeId = Guid.NewGuid(),
            ServiceName = "my-service",
            Environment = "Development",
            EventType = "rolled-back",
            TeamName = "Team Beta",
            OccurredAt = FixedNow
        };

        notification.Version.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // IncidentEventNotification
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IncidentEventNotification_ShouldHoldAllFields()
    {
        var incidentId = Guid.NewGuid();
        var notification = new IncidentEventNotification
        {
            IncidentId = incidentId,
            Title = "High latency in Order API",
            Severity = "High",
            Status = "Investigating",
            ServiceName = "order-service",
            TeamName = "Team Alpha",
            OpenedAt = FixedNow
        };

        notification.IncidentId.Should().Be(incidentId);
        notification.Title.Should().Be("High latency in Order API");
        notification.Severity.Should().Be("High");
        notification.Status.Should().Be("Investigating");
        notification.ServiceName.Should().Be("order-service");
        notification.OpenedAt.Should().Be(FixedNow);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GraphQLTopics
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GraphQLTopics_ChangeEvents_ShouldBeNonEmpty()
    {
        GraphQLTopics.ChangeEvents.Should().NotBeNullOrEmpty();
        GraphQLTopics.ChangeEvents.Should().Be("onChangeDeployed");
    }

    [Fact]
    public void GraphQLTopics_IncidentEvents_ShouldBeNonEmpty()
    {
        GraphQLTopics.IncidentEvents.Should().NotBeNullOrEmpty();
        GraphQLTopics.IncidentEvents.Should().Be("onIncidentUpdated");
    }

    // ─────────────────────────────────────────────────────────────────────
    // CatalogSubscription resolver passthrough
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void CatalogSubscription_OnChangeDeployed_ReturnsEventMessage()
    {
        var notification = new ChangeEventNotification
        {
            ChangeId = Guid.NewGuid(),
            ServiceName = "payment-service",
            Environment = "Production",
            EventType = "promoted",
            TeamName = "Payments Team",
            OccurredAt = FixedNow
        };

        var sub = new CatalogSubscription();
        var result = sub.OnChangeDeployed(notification);

        result.Should().BeSameAs(notification);
    }

    [Fact]
    public void CatalogSubscription_OnIncidentUpdated_ReturnsEventMessage()
    {
        var notification = new IncidentEventNotification
        {
            IncidentId = Guid.NewGuid(),
            Title = "DB connection failure",
            Severity = "Critical",
            Status = "Open",
            ServiceName = "catalog-service",
            TeamName = "Platform Team",
            OpenedAt = FixedNow
        };

        var sub = new CatalogSubscription();
        var result = sub.OnIncidentUpdated(notification);

        result.Should().BeSameAs(notification);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GraphQLEventPublisher
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GraphQLEventPublisher_PublishChangeEvent_CallsSender()
    {
        var sender = Substitute.For<ITopicEventSender>();
        var publisher = new GraphQLEventPublisher(sender);

        var notification = new ChangeEventNotification
        {
            ChangeId = Guid.NewGuid(),
            ServiceName = "svc",
            Environment = "Production",
            EventType = "deployed",
            TeamName = "Team",
            OccurredAt = FixedNow
        };

        await publisher.PublishChangeEventAsync(notification);

        await sender.Received(1).SendAsync(
            GraphQLTopics.ChangeEvents,
            notification,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GraphQLEventPublisher_PublishIncidentEvent_CallsSender()
    {
        var sender = Substitute.For<ITopicEventSender>();
        var publisher = new GraphQLEventPublisher(sender);

        var notification = new IncidentEventNotification
        {
            IncidentId = Guid.NewGuid(),
            Title = "Latency spike",
            Severity = "Medium",
            Status = "Open",
            ServiceName = "svc",
            TeamName = "Team",
            OpenedAt = FixedNow
        };

        await publisher.PublishIncidentEventAsync(notification);

        await sender.Received(1).SendAsync(
            GraphQLTopics.IncidentEvents,
            notification,
            Arg.Any<CancellationToken>());
    }
}
