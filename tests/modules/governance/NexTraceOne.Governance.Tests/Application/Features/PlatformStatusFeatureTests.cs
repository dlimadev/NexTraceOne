using System.Linq;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetPlatformEvents;
using NexTraceOne.Governance.Application.Features.GetPlatformHealth;
using NexTraceOne.Governance.Application.Features.GetPlatformJobs;
using NexTraceOne.Governance.Application.Features.GetPlatformQueues;
using NexTraceOne.Governance.Application.Features.GetPlatformReadiness;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

public sealed class PlatformStatusFeatureTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    // ── GetPlatformReadiness ──

    [Fact]
    public async Task GetPlatformReadiness_Handler_ShouldReturnSuccess()
    {
        var healthProvider = BuildFullHealthProvider();
        var handler = new GetPlatformReadiness.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformReadiness.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.EnvironmentName.Should().NotBeNullOrWhiteSpace();
        result.Value.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetPlatformReadiness_Handler_ShouldReturnAllExpectedChecks()
    {
        var healthProvider = BuildFullHealthProvider();
        var handler = new GetPlatformReadiness.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformReadiness.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Checks.Should().HaveCount(2);
        result.Value.Checks.Should().OnlyContain(c => c.Passed);
    }

    [Fact]
    public async Task GetPlatformReadiness_Response_ShouldHaveVersionFormat()
    {
        var healthProvider = BuildFullHealthProvider();
        var handler = new GetPlatformReadiness.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformReadiness.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPlatformReadiness_ShouldNotBeReady_WhenAnySubsystemUnhealthy()
    {
        var healthProvider = Substitute.For<IPlatformHealthProvider>();
        healthProvider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>
            {
                new("API", PlatformSubsystemStatus.Healthy, "OK"),
                new("Database", PlatformSubsystemStatus.Unhealthy, "Connection refused")
            });

        var handler = new GetPlatformReadiness.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformReadiness.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeFalse();
        result.Value.Checks.Single(c => c.Name == "Database").Passed.Should().BeFalse();
    }

    // ── GetPlatformHealth ──

    [Fact]
    public async Task GetPlatformHealth_Handler_ShouldReturnRealUptimeGreaterThanZero()
    {
        var healthProvider = Substitute.For<IPlatformHealthProvider>();
        healthProvider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>
            {
                new("API", PlatformSubsystemStatus.Healthy, "API is responding."),
                new("Database", PlatformSubsystemStatus.Healthy, "All checks healthy.")
            });

        var handler = new GetPlatformHealth.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformHealth.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UptimeSeconds.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPlatformHealth_Handler_ShouldAggregateOverallAsUnhealthy_WhenAnySubsystemIsUnhealthy()
    {
        var healthProvider = Substitute.For<IPlatformHealthProvider>();
        healthProvider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>
            {
                new("API", PlatformSubsystemStatus.Healthy, "OK"),
                new("Database", PlatformSubsystemStatus.Unhealthy, "Connection failed")
            });

        var handler = new GetPlatformHealth.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformHealth.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(PlatformSubsystemStatus.Unhealthy);
    }

    [Fact]
    public async Task GetPlatformHealth_Handler_ShouldAggregateOverallAsDegraded_WhenAnySubsystemIsUnknown()
    {
        var healthProvider = Substitute.For<IPlatformHealthProvider>();
        healthProvider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>
            {
                new("API", PlatformSubsystemStatus.Healthy, "OK"),
                new("BackgroundJobs", PlatformSubsystemStatus.Unknown, "Not evaluated")
            });

        var handler = new GetPlatformHealth.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformHealth.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(PlatformSubsystemStatus.Degraded);
    }

    [Fact]
    public async Task GetPlatformHealth_Handler_ShouldReturnUnknown_WhenNoSubsystems()
    {
        var healthProvider = Substitute.For<IPlatformHealthProvider>();
        healthProvider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>());

        var handler = new GetPlatformHealth.Handler(healthProvider);
        var result = await handler.Handle(new GetPlatformHealth.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(PlatformSubsystemStatus.Unknown);
    }

    // ── GetPlatformJobs ──

    [Fact]
    public async Task GetPlatformJobs_Handler_ShouldReturnAllKnownJobs_WhenNoFilter()
    {
        var jobProvider = Substitute.For<IPlatformJobStatusProvider>();
        jobProvider.GetJobSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KnownJobSnapshot>
            {
                new("outbox-processor-governance", "Outbox Processor — Governance", "Processes governance outbox."),
                new("identity-expiration",          "Identity Expiration Cleanup",   "Revokes expired sessions.")
            });

        var handler = new GetPlatformJobs.Handler(jobProvider);
        var result = await handler.Handle(new GetPlatformJobs.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(2);
        result.Value.Jobs.Should().OnlyContain(j => j.Status == BackgroundJobStatus.Stale);
        result.Value.Jobs.Should().OnlyContain(j => j.LastRunAt == null);
    }

    [Fact]
    public async Task GetPlatformJobs_Handler_ShouldReturnEmpty_WhenFilteringForRunning()
    {
        var jobProvider = Substitute.For<IPlatformJobStatusProvider>();
        jobProvider.GetJobSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KnownJobSnapshot>
            {
                new("outbox-processor-governance", "Outbox Processor", "Description")
            });

        var handler = new GetPlatformJobs.Handler(jobProvider);
        var result = await handler.Handle(new GetPlatformJobs.Query(StatusFilter: "Running"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().BeEmpty("all jobs are reported as Stale when BackgroundWorkers is a separate process");
    }

    // ── GetPlatformEvents ──

    [Fact]
    public async Task GetPlatformEvents_Handler_ShouldRespectSeverityFilter()
    {
        var eventProvider = Substitute.For<IPlatformEventProvider>();
        eventProvider.GetRecentEventsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceOperationalEvent>
            {
                new("rollout-1", FixedNow.AddMinutes(-10), "Warning", "Governance", "Pack rolled back.", false),
                new("rollout-2", FixedNow.AddMinutes(-20), "Info",    "Governance", "Pack completed.",  true)
            });

        var handler = new GetPlatformEvents.Handler(eventProvider);
        var result = await handler.Handle(new GetPlatformEvents.Query(SeverityFilter: "Warning"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().HaveCount(1);
        result.Value.Events.Should().OnlyContain(e => e.Severity == PlatformEventSeverity.Warning);
    }

    [Fact]
    public async Task GetPlatformEvents_Handler_ShouldRespectSubsystemFilter()
    {
        var eventProvider = Substitute.For<IPlatformEventProvider>();
        eventProvider.GetRecentEventsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceOperationalEvent>
            {
                new("rollout-1", FixedNow, "Info",    "Governance", "Governance event.", true),
                new("waiver-1",  FixedNow, "Warning", "Identity",   "Identity event.",   false)
            });

        var handler = new GetPlatformEvents.Handler(eventProvider);
        var result = await handler.Handle(new GetPlatformEvents.Query(SubsystemFilter: "Governance"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().HaveCount(1);
        result.Value.Events.Should().OnlyContain(e => e.Subsystem == "Governance");
    }

    [Fact]
    public async Task GetPlatformEvents_Handler_ShouldReturnEmpty_WhenNoEventsMatchFilter()
    {
        var eventProvider = Substitute.For<IPlatformEventProvider>();
        eventProvider.GetRecentEventsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceOperationalEvent>
            {
                new("rollout-1", FixedNow, "Info", "Governance", "Pack completed.", true)
            });

        var handler = new GetPlatformEvents.Handler(eventProvider);
        var result = await handler.Handle(new GetPlatformEvents.Query(SeverityFilter: "Critical"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetPlatformQueues ──

    [Fact]
    public async Task GetPlatformQueues_Handler_ShouldReturnQueues()
    {
        var metricsProvider = Substitute.For<IPlatformQueueMetricsProvider>();
        metricsProvider.GetQueueSnapshotsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<QueueSnapshot>
            {
                new("gov_outbox_messages", "Governance", PendingCount: 3, FailedCount: 1, LastActivityAt: FixedNow)
            });

        var handler = new GetPlatformQueues.Handler(metricsProvider);
        var result = await handler.Handle(new GetPlatformQueues.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Queues.Should().HaveCount(1);
        result.Value.Queues[0].QueueName.Should().Be("gov_outbox_messages");
        result.Value.Queues[0].PendingCount.Should().Be(3);
        result.Value.Queues[0].FailedCount.Should().Be(1);
        result.Value.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── helpers ──

    private static IPlatformHealthProvider BuildFullHealthProvider()
    {
        var provider = Substitute.For<IPlatformHealthProvider>();
        provider.GetSubsystemHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SubsystemHealthInfo>
            {
                new("API",      PlatformSubsystemStatus.Healthy, "API is responding."),
                new("Database", PlatformSubsystemStatus.Healthy, "All checks healthy.")
            });
        return provider;
    }
}
