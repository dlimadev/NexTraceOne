using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using CalculateBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius.CalculateBlastRadius;
using ClassifyChangeLevelFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel.ClassifyChangeLevel;
using ComputeChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore.ComputeChangeScore;
using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using GetTraceCorrelationsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTraceCorrelations.GetTraceCorrelations;
using NotifyDeploymentFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment.NotifyDeployment;
using RecordTraceCorrelationFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordTraceCorrelation.RecordTraceCorrelation;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes de handlers da camada Application do módulo ChangeIntelligence.</summary>
public sealed class ChangeIntelligenceApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ── NotifyDeployment ──────────────────────────────────────────────────

    [Fact]
    public async Task NotifyDeployment_Should_CreateRelease_AndReturnResponse()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var markerRepository = Substitute.For<IExternalMarkerRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var scoreCalculator = new ChangeScoreCalculator();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new NotifyDeploymentFeature.Handler(repository, changeEventRepository, markerRepository, scoreRepository, scoreCalculator, unitOfWork, dateTimeProvider);

        repository.GetByServiceNameVersionEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new NotifyDeploymentFeature.Command(
            Guid.NewGuid(), "MyService", "2.0.0", "prod", "https://ci/pipeline/42", "deadbeef1234");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be(command.ServiceName);
        result.Value.Version.Should().Be(command.Version);
        result.Value.Environment.Should().Be(command.Environment);
        result.Value.IsNewRelease.Should().BeTrue();
        result.Value.AutoScore.Should().BeInRange(0m, 1m);
        repository.Received(1).Add(Arg.Any<Release>());
        changeEventRepository.Received(1).Add(Arg.Any<ChangeEvent>());
        markerRepository.Received(1).Add(Arg.Any<ExternalMarker>());
        scoreRepository.Received(1).Add(Arg.Any<ChangeIntelligenceScore>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyDeployment_Should_EnrichExistingRelease_WhenSameServiceVersionEnvironment()
    {
        var existingRelease = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var markerRepository = Substitute.For<IExternalMarkerRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var scoreCalculator = new ChangeScoreCalculator();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new NotifyDeploymentFeature.Handler(repository, changeEventRepository, markerRepository, scoreRepository, scoreCalculator, unitOfWork, dateTimeProvider);

        repository.GetByServiceNameVersionEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingRelease);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new NotifyDeploymentFeature.Command(
            null, existingRelease.ServiceName, existingRelease.Version, existingRelease.Environment,
            "https://ci/pipeline/99", "aabbccdd1234");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(existingRelease.Id.Value);
        result.Value.IsNewRelease.Should().BeFalse();
        repository.DidNotReceive().Add(Arg.Any<Release>());
        changeEventRepository.Received(1).Add(Arg.Any<ChangeEvent>());
        markerRepository.Received(1).Add(Arg.Any<ExternalMarker>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyDeployment_Should_RecordExternalMarker_WithDeploymentStartedType()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var markerRepository = Substitute.For<IExternalMarkerRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var scoreCalculator = new ChangeScoreCalculator();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new NotifyDeploymentFeature.Handler(repository, changeEventRepository, markerRepository, scoreRepository, scoreCalculator, unitOfWork, dateTimeProvider);

        repository.GetByServiceNameVersionEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new NotifyDeploymentFeature.Command(
            null, "PaymentService", "3.1.0", "production", "github-actions", "cafebabe5678",
            ExternalDeploymentId: "run-12345");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalMarkerId.Should().NotBe(Guid.Empty);
        markerRepository.Received(1).Add(Arg.Is<ExternalMarker>(m =>
            m.MarkerType == MarkerType.DeploymentStarted
            && m.SourceSystem == command.PipelineSource
            && m.ExternalId == "run-12345"));
    }

    // ── ClassifyChangeLevel ───────────────────────────────────────────────

    [Fact]
    public async Task ClassifyChangeLevel_Should_UpdateChangeLevel_WhenReleaseExists()
    {
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new ClassifyChangeLevelFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var result = await sut.Handle(
            new ClassifyChangeLevelFeature.Command(release.Id.Value, ChangeLevel.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyChangeLevel_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new ClassifyChangeLevelFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await sut.Handle(
            new ClassifyChangeLevelFeature.Command(Guid.NewGuid(), ChangeLevel.Breaking),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }

    // ── CalculateBlastRadius ──────────────────────────────────────────────

    [Fact]
    public async Task CalculateBlastRadius_Should_CreateReport_AndReturnResponse()
    {
        var release = CreateRelease();
        var releaseRepository = Substitute.For<IReleaseRepository>();
        var blastRadiusRepository = Substitute.For<IBlastRadiusRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var scoreCalculator = new ChangeScoreCalculator();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CalculateBlastRadiusFeature.Handler(releaseRepository, blastRadiusRepository, scoreRepository, scoreCalculator, unitOfWork, dateTimeProvider);

        releaseRepository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var direct = new List<string> { "ServiceA", "ServiceB" };
        var transitive = new List<string> { "ServiceC" };

        var result = await sut.Handle(
            new CalculateBlastRadiusFeature.Command(release.Id.Value, direct, transitive),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAffectedConsumers.Should().Be(direct.Count + transitive.Count);
        result.Value.UpdatedScore.Should().BeInRange(0m, 1m);
        blastRadiusRepository.Received(1).Add(Arg.Any<BlastRadiusReport>());
        scoreRepository.Received(1).Add(Arg.Any<ChangeIntelligenceScore>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ComputeChangeScore ────────────────────────────────────────────────

    [Fact]
    public async Task ComputeChangeScore_Should_Succeed_WithValidWeights()
    {
        var release = CreateRelease();
        var releaseRepository = Substitute.For<IReleaseRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ComputeChangeScoreFeature.Handler(releaseRepository, scoreRepository, unitOfWork, dateTimeProvider);

        releaseRepository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeChangeScoreFeature.Command(release.Id.Value, 0.6m, 0.4m, 0.2m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().BeInRange(0m, 1m);
        scoreRepository.Received(1).Add(Arg.Any<ChangeIntelligenceScore>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GetRelease ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRelease_Should_ReturnResponse_WhenReleaseExists()
    {
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var sut = new GetReleaseFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var result = await sut.Handle(new GetReleaseFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.ServiceName.Should().Be(release.ServiceName);
        result.Value.Version.Should().Be(release.Version);
        result.Value.Environment.Should().Be(release.Environment);
    }

    [Fact]
    public async Task GetRelease_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var sut = new GetReleaseFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await sut.Handle(new GetReleaseFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }

    // ── RecordTraceCorrelation ─────────────────────────────────────────────────

    [Fact]
    public async Task RecordTraceCorrelation_Should_CreateChangeEvent_AndWriteToAnalytics()
    {
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var traceWriter = Substitute.For<ITraceCorrelationWriter>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new RecordTraceCorrelationFeature.Handler(
            repository, changeEventRepository, traceWriter, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var command = new RecordTraceCorrelationFeature.Command(
            ReleaseId: release.Id.Value,
            TraceId: "4bf92f3577b34da6a3ce929d0e0e4736",
            ServiceName: "TestService",
            Environment: "staging",
            CorrelationSource: "deployment_event");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TraceId.Should().Be(command.TraceId);
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.CorrelationSource.Should().Be("deployment_event");
        changeEventRepository.Received(1).Add(Arg.Is<ChangeEvent>(e =>
            e.EventType == "trace_correlated" && e.Source == command.TraceId));
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await traceWriter.Received(1).WriteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            command.TraceId, command.ServiceName, Arg.Any<Guid?>(),
            command.Environment, Arg.Any<Guid?>(), command.CorrelationSource,
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            FixedNow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordTraceCorrelation_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var traceWriter = Substitute.For<ITraceCorrelationWriter>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var sut = new RecordTraceCorrelationFeature.Handler(
            repository, changeEventRepository, traceWriter, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var command = new RecordTraceCorrelationFeature.Command(
            ReleaseId: Guid.NewGuid(),
            TraceId: "4bf92f3577b34da6a3ce929d0e0e4736",
            ServiceName: "TestService",
            Environment: "staging");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
        changeEventRepository.DidNotReceive().Add(Arg.Any<ChangeEvent>());
        await traceWriter.DidNotReceive().WriteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // ── GetTraceCorrelations ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTraceCorrelations_Should_ReturnCorrelations_ForRelease()
    {
        var release = CreateRelease();
        var traceEvent = ChangeEvent.Create(
            release.Id,
            "trace_correlated",
            "Trace '4bf92f3577b34da6a3ce929d0e0e4736' correlated",
            "4bf92f3577b34da6a3ce929d0e0e4736",
            FixedNow);

        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();

        var sut = new GetTraceCorrelationsFeature.Handler(repository, changeEventRepository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        changeEventRepository.ListByReleaseIdAndEventTypeAsync(
            Arg.Any<ReleaseId>(), "trace_correlated", Arg.Any<CancellationToken>())
            .Returns(new List<ChangeEvent> { traceEvent });

        var result = await sut.Handle(new GetTraceCorrelationsFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.Correlations.Should().HaveCount(1);
        result.Value.Correlations[0].TraceId.Should().Be("4bf92f3577b34da6a3ce929d0e0e4736");
    }

    [Fact]
    public async Task GetTraceCorrelations_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var changeEventRepository = Substitute.For<IChangeEventRepository>();
        var sut = new GetTraceCorrelationsFeature.Handler(repository, changeEventRepository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await sut.Handle(new GetTraceCorrelationsFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }
}
