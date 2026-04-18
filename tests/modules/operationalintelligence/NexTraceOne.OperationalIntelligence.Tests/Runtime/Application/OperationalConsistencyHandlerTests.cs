using FluentAssertions;
using NSubstitute;
using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareEnvironments;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.EstablishRuntimeBaseline;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários dos handlers introduzidos no P6.5:
/// EstablishRuntimeBaseline e CompareEnvironments.
/// </summary>
public sealed class OperationalConsistencyHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private static RuntimeSnapshot MakeSnapshot(
        string serviceName,
        string env,
        decimal avgMs = 100m,
        decimal p99Ms = 200m,
        decimal errorRate = 0.01m,
        decimal rps = 50m)
        => RuntimeSnapshot.Create(serviceName, env, avgMs, p99Ms, errorRate, rps, 30m, 512m, 2, FixedNow, "Prometheus");

    // ── EstablishRuntimeBaseline ──────────────────────────────────────────────

    [Fact]
    public async Task EstablishRuntimeBaseline_NewBaseline_CreatesAndPersists()
    {
        var repo = Substitute.For<IRuntimeBaselineRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        repo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns((RuntimeBaseline?)null);

        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new EstablishRuntimeBaseline.Handler(repo, uow, dt);
        var command = new EstablishRuntimeBaseline.Command("svc-api", "production", 100m, 200m, 0.01m, 50m, 30, 0.8m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("svc-api");
        result.Value.Environment.Should().Be("production");
        result.Value.IsUpdate.Should().BeFalse();
        result.Value.IsConfident.Should().BeTrue();
        repo.Received(1).Add(Arg.Any<RuntimeBaseline>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstablishRuntimeBaseline_ExistingBaseline_RefreshesInPlace()
    {
        var existing = RuntimeBaseline.Establish("svc-api", "production", 100m, 200m, 0.01m, 50m, FixedNow.AddDays(-7), 10, 0.4m);

        var repo = Substitute.For<IRuntimeBaselineRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        repo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(existing);

        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new EstablishRuntimeBaseline.Handler(repo, uow, dt);
        var command = new EstablishRuntimeBaseline.Command("svc-api", "production", 110m, 220m, 0.008m, 55m, 30, 0.85m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsUpdate.Should().BeTrue();
        result.Value.ExpectedAvgLatencyMs.Should().Be(110m);
        result.Value.DataPointCount.Should().Be(30);
        result.Value.IsConfident.Should().BeTrue();
        repo.DidNotReceive().Add(Arg.Any<RuntimeBaseline>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstablishRuntimeBaseline_LowConfidence_ReturnedInResponse()
    {
        var repo = Substitute.For<IRuntimeBaselineRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        repo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RuntimeBaseline?)null);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new EstablishRuntimeBaseline.Handler(repo, uow, dt);
        var command = new EstablishRuntimeBaseline.Command("svc-api", "staging", 80m, 160m, 0.005m, 40m, 3, 0.3m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfident.Should().BeFalse();
        result.Value.ConfidenceScore.Should().Be(0.3m);
    }

    // ── CompareEnvironments ───────────────────────────────────────────────────

    [Fact]
    public async Task CompareEnvironments_BothSnapshotsMatch_NoDriftFindings()
    {
        var stagingSnap = MakeSnapshot("svc-api", "staging");
        var prodSnap = MakeSnapshot("svc-api", "production");

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>()).Returns(prodSnap);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeFalse();
        result.Value.Deviations.Should().HaveCount(4); // 4 metrics compared
        result.Value.Deviations.All(d => d.FindingId == null).Should().BeTrue();
        driftRepo.DidNotReceive().Add(Arg.Any<DriftFinding>());
    }

    [Fact]
    public async Task CompareEnvironments_HighLatencyInProd_CreatesDriftFinding()
    {
        var stagingSnap = MakeSnapshot("svc-api", "staging", avgMs: 100m, p99Ms: 200m);
        // production has 3x higher latency — far outside 20% tolerance
        var prodSnap = MakeSnapshot("svc-api", "production", avgMs: 300m, p99Ms: 600m);

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>()).Returns(prodSnap);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeTrue();
        result.Value.Deviations.Where(d => d.FindingId.HasValue).Should().HaveCountGreaterThanOrEqualTo(2);
        driftRepo.Received().Add(Arg.Any<DriftFinding>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompareEnvironments_SourceSnapshotMissing_ReturnsNotFound()
    {
        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>())
            .Returns((RuntimeSnapshot?)null);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompareEnvironments_TargetSnapshotMissing_ReturnsNotFound()
    {
        var stagingSnap = MakeSnapshot("svc-api", "staging");

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns((RuntimeSnapshot?)null);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompareEnvironments_WithReleaseId_DriftFindingsCorrelatedToRelease()
    {
        var releaseId = Guid.NewGuid();
        var stagingSnap = MakeSnapshot("svc-api", "staging", avgMs: 100m);
        var prodSnap = MakeSnapshot("svc-api", "production", avgMs: 500m); // 400% deviation

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>()).Returns(prodSnap);

        DriftFinding? capturedFinding = null;
        var driftRepo = Substitute.For<IDriftFindingRepository>();
        driftRepo.When(r => r.Add(Arg.Any<DriftFinding>()))
            .Do(ci => capturedFinding = ci.Arg<DriftFinding>());

        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m, releaseId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedFinding.Should().NotBeNull();
        capturedFinding!.ReleaseId.Should().Be(releaseId);
        capturedFinding.Environment.Should().Be("production");
    }

    [Fact]
    public async Task CompareEnvironments_HealthStatusDifferent_ReflectedInResponse()
    {
        var stagingSnap = MakeSnapshot("svc-api", "staging", errorRate: 0.02m); // Healthy
        var prodSnap = MakeSnapshot("svc-api", "production", errorRate: 0.12m); // Unhealthy

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>()).Returns(prodSnap);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceHealthStatus.Should().Be(HealthStatus.Healthy.ToString());
        result.Value.TargetHealthStatus.Should().Be(HealthStatus.Unhealthy.ToString());
        result.Value.HasDrift.Should().BeTrue();
    }

    [Fact]
    public async Task CompareEnvironments_EnvironmentNamesArePersisted()
    {
        var stagingSnap = MakeSnapshot("svc-api", "staging");
        var prodSnap = MakeSnapshot("svc-api", "production");

        var snapRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapRepo.GetLatestByServiceAsync("svc-api", "staging", Arg.Any<CancellationToken>()).Returns(stagingSnap);
        snapRepo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>()).Returns(prodSnap);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        var uow = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CompareEnvironments.Handler(snapRepo, driftRepo, uow, dt);
        var command = new CompareEnvironments.Command("svc-api", "staging", "production", 20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceEnvironment.Should().Be("staging");
        result.Value.TargetEnvironment.Should().Be("production");
        result.Value.ServiceName.Should().Be("svc-api");
    }
}
