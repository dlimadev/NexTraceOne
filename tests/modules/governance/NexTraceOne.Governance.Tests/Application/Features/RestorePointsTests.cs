using System.Linq;
using FluentAssertions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetRestorePoints;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Integrations.Domain;
using NSubstitute;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes para GetRestorePoints.
/// Verifica integração com IBackupProvider, IRecoveryJobRepository e persistência de jobs.
/// </summary>
public sealed class RestorePointsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 14, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static BackupRestorePoint SamplePoint(string id = "rp-001") =>
        new(id, FixedNow.AddHours(-2), 128, "Available", "pg_dump");

    // ── GetRestorePoints.Handler (Query) ──────────────────────────────────────────

    [Fact]
    public async Task Handler_WhenProviderNotConfigured_ShouldReturnEmptyWithNote()
    {
        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(false);
        provider.ListRestorePointsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BackupRestorePoint>());

        var handler = new GetRestorePoints.Handler(provider);
        var result = await handler.Handle(new GetRestorePoints.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RestorePoints.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
        result.Value.Oldest.Should().BeNull();
        result.Value.Latest.Should().BeNull();
        result.Value.SimulatedNote.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handler_WhenProviderConfiguredWithPoints_ShouldReturnMappedDtos()
    {
        var points = new[]
        {
            SamplePoint("rp-001"),
            SamplePoint("rp-002") with { CreatedAt = FixedNow.AddHours(-5) }
        };

        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(true);
        provider.ListRestorePointsAsync(Arg.Any<CancellationToken>()).Returns(points);

        var handler = new GetRestorePoints.Handler(provider);
        var result = await handler.Handle(new GetRestorePoints.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RestorePoints.Should().HaveCount(2);
        result.Value.Total.Should().Be(2);
        result.Value.Oldest.Should().Be(points.Min(p => p.CreatedAt));
        result.Value.Latest.Should().Be(points.Max(p => p.CreatedAt));
        result.Value.SimulatedNote.Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_WhenProviderConfiguredWithNoPoints_ShouldReturnEmptyWithoutNote()
    {
        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(true);
        provider.ListRestorePointsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BackupRestorePoint>());

        var handler = new GetRestorePoints.Handler(provider);
        var result = await handler.Handle(new GetRestorePoints.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RestorePoints.Should().BeEmpty();
        result.Value.SimulatedNote.Should().BeEmpty();
    }

    // ── GetRestorePoints.InitiateHandler ─────────────────────────────────────────

    [Fact]
    public async Task InitiateHandler_DryRun_ShouldPersistJobWithDryRunStatus()
    {
        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(false);

        var jobRepo = Substitute.For<IRecoveryJobRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = CreateClock();

        var handler = new GetRestorePoints.InitiateHandler(provider, jobRepo, unitOfWork, clock);
        var command = new GetRestorePoints.InitiateRecovery(
            RestorePointId: "rp-001",
            Scope: "full",
            Schemas: null,
            DryRun: true,
            InitiatedBy: "admin@company.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DryRun.Should().BeTrue();
        result.Value.Status.Should().Be("DryRunCompleted");
        result.Value.JobId.Should().NotBeNullOrWhiteSpace();

        await jobRepo.Received(1).AddAsync(
            Arg.Is<RecoveryJob>(j =>
                j.RestorePointId == "rp-001" &&
                j.DryRun == true &&
                j.InitiatedBy == "admin@company.com"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateHandler_RealRun_ShouldPersistJobWithInitiatedStatus()
    {
        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(false);

        var jobRepo = Substitute.For<IRecoveryJobRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = CreateClock();

        var handler = new GetRestorePoints.InitiateHandler(provider, jobRepo, unitOfWork, clock);
        var command = new GetRestorePoints.InitiateRecovery(
            RestorePointId: "rp-002",
            Scope: "partial",
            Schemas: ["public", "audit"],
            DryRun: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DryRun.Should().BeFalse();
        result.Value.Status.Should().Be("Initiated");

        await jobRepo.Received(1).AddAsync(
            Arg.Is<RecoveryJob>(j =>
                j.RestorePointId == "rp-002" &&
                j.Scope == "partial" &&
                j.DryRun == false &&
                j.SchemasJson != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateHandler_WhenProviderConfiguredAndPointNotFound_ShouldReturnNotFoundError()
    {
        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetRestorePointAsync("rp-missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackupRestorePoint?>(null));

        var jobRepo = Substitute.For<IRecoveryJobRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = CreateClock();

        var handler = new GetRestorePoints.InitiateHandler(provider, jobRepo, unitOfWork, clock);
        var command = new GetRestorePoints.InitiateRecovery(
            RestorePointId: "rp-missing",
            Scope: "full",
            Schemas: null,
            DryRun: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);

        await jobRepo.DidNotReceive().AddAsync(Arg.Any<RecoveryJob>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateHandler_WhenProviderConfiguredAndPointExists_ShouldCreateJob()
    {
        var point = SamplePoint("rp-003");

        var provider = Substitute.For<IBackupProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetRestorePointAsync("rp-003", Arg.Any<CancellationToken>()).Returns(point);

        var jobRepo = Substitute.For<IRecoveryJobRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = CreateClock();

        var handler = new GetRestorePoints.InitiateHandler(provider, jobRepo, unitOfWork, clock);
        var command = new GetRestorePoints.InitiateRecovery(
            RestorePointId: "rp-003",
            Scope: "schema",
            Schemas: ["public"],
            DryRun: false,
            InitiatedBy: "ops@company.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.JobId.Should().NotBeNullOrWhiteSpace();
        await jobRepo.Received(1).AddAsync(Arg.Any<RecoveryJob>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── RecoveryJob domain entity ────────────────────────────────────────────────

    [Fact]
    public void RecoveryJob_Create_RealRun_ShouldHaveInitiatedStatus()
    {
        var job = RecoveryJob.Create("rp-001", "full", null, dryRun: false, "admin", FixedNow);

        job.Status.Should().Be("Initiated");
        job.DryRun.Should().BeFalse();
        job.RestorePointId.Should().Be("rp-001");
        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void RecoveryJob_Create_DryRun_ShouldHaveDryRunCompletedStatus()
    {
        var job = RecoveryJob.Create("rp-001", "partial", "[\"public\"]", dryRun: true, null, FixedNow);

        job.Status.Should().Be("DryRunCompleted");
        job.DryRun.Should().BeTrue();
        job.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void RecoveryJob_Complete_ShouldUpdateStatusAndMessage()
    {
        var job = RecoveryJob.Create("rp-001", "full", null, dryRun: false, "admin", FixedNow);

        job.Complete("Recovery completed successfully.", FixedNow.AddMinutes(5));

        job.Status.Should().Be("Completed");
        job.Message.Should().Contain("completed successfully");
        job.CompletedAt.Should().Be(FixedNow.AddMinutes(5));
    }

    [Fact]
    public void RecoveryJob_Fail_ShouldUpdateStatusAndReason()
    {
        var job = RecoveryJob.Create("rp-001", "full", null, dryRun: false, "admin", FixedNow);

        job.Fail("Connection refused by backup server.", FixedNow.AddSeconds(30));

        job.Status.Should().Be("Failed");
        job.Message.Should().Contain("Connection refused");
        job.CompletedAt.Should().Be(FixedNow.AddSeconds(30));
    }
}
