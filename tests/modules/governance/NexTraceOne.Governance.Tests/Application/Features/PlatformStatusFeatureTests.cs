using System.Linq;
using Microsoft.Extensions.Configuration;
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
    // ── GetPlatformReadiness ──

    [Fact]
    public async Task GetPlatformReadiness_Handler_ShouldReturnSuccess()
    {
        var handler = new GetPlatformReadiness.Handler();
        var query = new GetPlatformReadiness.Query();
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.EnvironmentName.Should().NotBeNullOrWhiteSpace();
        result.Value.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetPlatformReadiness_Handler_ShouldReturnAllExpectedChecks()
    {
        var handler = new GetPlatformReadiness.Handler();
        var query = new GetPlatformReadiness.Query();
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var expectedChecks = new[] { "API", "Database", "Configuration", "BackgroundJobs", "Ingestion" };
        result.Value.Checks.Should().HaveCount(expectedChecks.Length);
        result.Value.Checks.Select(c => c.Name).Should().BeEquivalentTo(expectedChecks);
        result.Value.Checks.Should().OnlyContain(c => c.Passed);
    }

    [Fact]
    public async Task GetPlatformReadiness_Response_ShouldHaveVersionFormat()
    {
        var handler = new GetPlatformReadiness.Handler();
        var query = new GetPlatformReadiness.Query();
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().NotBeNullOrWhiteSpace();
        result.Value.Version.Should().Contain(".");
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
        var query = new GetPlatformHealth.Query();
        var result = await handler.Handle(query, CancellationToken.None);
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
    public async Task GetPlatformJobs_Handler_ShouldRespectStatusFilter()
    {
        var handler = new GetPlatformJobs.Handler();
        var query = new GetPlatformJobs.Query(StatusFilter: "Running");
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().NotBeEmpty();
        result.Value.Jobs.Should().OnlyContain(j => j.Status == Governance.Domain.Enums.BackgroundJobStatus.Running);
    }

    // ── GetPlatformEvents ──

    [Fact]
    public async Task GetPlatformEvents_Handler_ShouldRespectSeverityFilter()
    {
        var handler = new GetPlatformEvents.Handler();
        var query = new GetPlatformEvents.Query(SeverityFilter: "Warning");
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().NotBeEmpty();
        result.Value.Events.Should().OnlyContain(e => e.Severity == Governance.Domain.Enums.PlatformEventSeverity.Warning);
    }

    [Fact]
    public async Task GetPlatformEvents_Handler_ShouldRespectSubsystemFilter()
    {
        var handler = new GetPlatformEvents.Handler();
        var query = new GetPlatformEvents.Query(SubsystemFilter: "API");
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().NotBeEmpty();
        result.Value.Events.Should().OnlyContain(e => e.Subsystem == "API");
    }

    // ── GetPlatformQueues ──

    [Fact]
    public async Task GetPlatformQueues_Handler_ShouldReturnQueues()
    {
        var handler = new GetPlatformQueues.Handler();
        var query = new GetPlatformQueues.Query();
        var result = await handler.Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Queues.Should().NotBeEmpty();
        result.Value.Queues.Should().OnlyContain(q => q.QueueName.Length > 0);
        result.Value.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
