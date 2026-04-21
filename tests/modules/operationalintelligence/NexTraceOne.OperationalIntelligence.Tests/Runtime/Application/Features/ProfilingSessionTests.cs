using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetProfilingAnalysis;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetProfilingHistory;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestProfilingSession;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para as features de Continuous Profiling (Wave D).
/// Cobrem: domínio da entidade ProfilingSession, handlers de comando e query, validação.
/// </summary>
public sealed class ProfilingSessionTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowStart = new(2026, 4, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 4, 21, 10, 30, 0, TimeSpan.Zero);

    private readonly IProfilingSessionRepository _repository = Substitute.For<IProfilingSessionRepository>();
    private readonly IRuntimeIntelligenceUnitOfWork _unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ProfilingSessionTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Domain: ProfilingSession entity ──────────────────────────────────

    [Fact]
    public void ProfilingSession_Start_Creates_Valid_Session()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "payment-svc", "Production",
            ProfilingFrameType.DotNetTrace,
            WindowStart, WindowEnd, 5000, 512.5m, 32, FixedNow);

        session.Should().NotBeNull();
        session.ServiceName.Should().Be("payment-svc");
        session.Environment.Should().Be("Production");
        session.FrameType.Should().Be(ProfilingFrameType.DotNetTrace);
        session.TenantId.Should().Be("tenant-1");
        session.TotalCpuSamples.Should().Be(5000);
        session.PeakMemoryMb.Should().Be(512.5m);
        session.PeakThreadCount.Should().Be(32);
        session.HasAnomalies.Should().BeFalse();
        session.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ProfilingSession_Start_Calculates_DurationSeconds_Correctly()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "svc", "prod",
            ProfilingFrameType.Pprof,
            WindowStart, WindowEnd, 0, 0m, 0, FixedNow);

        session.DurationSeconds.Should().Be(1800); // 30 minutes
    }

    [Fact]
    public void ProfilingSession_Start_Throws_When_WindowEnd_Before_WindowStart()
    {
        var act = () => ProfilingSession.Start(
            "tenant-1", "svc", "prod",
            ProfilingFrameType.Pprof,
            WindowStart, WindowStart.AddSeconds(-1), 0, 0m, 0, FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ProfilingSession_AttachTopFrames_Sets_TopFramesJson()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "svc", "prod",
            ProfilingFrameType.DotNetTrace,
            WindowStart, WindowEnd, 100, 256m, 8, FixedNow);

        session.AttachTopFrames("[{\"method\":\"Foo\",\"sampleCount\":100}]");

        session.TopFramesJson.Should().NotBeNullOrWhiteSpace();
        session.TopFramesJson.Should().Contain("Foo");
    }

    [Fact]
    public void ProfilingSession_AttachRawDataReference_Sets_Uri_And_Hash()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "svc", "prod",
            ProfilingFrameType.AsyncProfiler,
            WindowStart, WindowEnd, 0, 0m, 0, FixedNow);

        session.AttachRawDataReference("s3://bucket/key.nettrace", "abc123hash");

        session.RawDataUri.Should().Be("s3://bucket/key.nettrace");
        session.RawDataHash.Should().Be("abc123hash");
    }

    [Fact]
    public void ProfilingSession_LinkToRelease_Sets_ReleaseVersion_And_CommitSha()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "svc", "staging",
            ProfilingFrameType.Pprof,
            WindowStart, WindowEnd, 0, 0m, 0, FixedNow);

        session.LinkToRelease("v2.5.1", "deadbeef");

        session.ReleaseVersion.Should().Be("v2.5.1");
        session.CommitSha.Should().Be("deadbeef");
    }

    [Fact]
    public void ProfilingSession_MarkAsHavingAnomalies_Sets_HasAnomalies_True()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "svc", "prod",
            ProfilingFrameType.GenericStackSamples,
            WindowStart, WindowEnd, 0, 0m, 0, FixedNow);

        session.MarkAsHavingAnomalies();

        session.HasAnomalies.Should().BeTrue();
    }

    [Fact]
    public void ProfilingSession_Start_Clamps_ServiceName_To_200_Chars()
    {
        var longName = new string('x', 250);

        var session = ProfilingSession.Start(
            "tenant-1", longName, "prod",
            ProfilingFrameType.DotNetTrace,
            WindowStart, WindowEnd, 0, 0m, 0, FixedNow);

        session.ServiceName.Should().HaveLength(200);
    }

    [Fact]
    public void ProfilingFrameType_Has_Expected_Values()
    {
        ((int)ProfilingFrameType.DotNetTrace).Should().Be(0);
        ((int)ProfilingFrameType.Pprof).Should().Be(1);
        ((int)ProfilingFrameType.AsyncProfiler).Should().Be(2);
        ((int)ProfilingFrameType.GenericStackSamples).Should().Be(3);
    }

    // ── Application: IngestProfilingSession handler ───────────────────────

    [Fact]
    public async Task IngestProfilingSession_Handler_Creates_Session_And_Saves()
    {
        var handler = new IngestProfilingSession.Handler(_repository, _unitOfWork, _clock);
        var command = BuildIngestCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("payment-svc");
        result.Value.FrameType.Should().Be("DotNetTrace");
        result.Value.DurationSeconds.Should().Be(1800);
        result.Value.SessionId.Should().NotBe(Guid.Empty);
        _repository.Received(1).Add(Arg.Any<ProfilingSession>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestProfilingSession_Handler_Attaches_TopFrames_When_Provided()
    {
        var handler = new IngestProfilingSession.Handler(_repository, _unitOfWork, _clock);
        var command = BuildIngestCommand(topFramesJson: "[{\"method\":\"Hotspot\"}]");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasTopFrames.Should().BeTrue();
    }

    [Fact]
    public async Task IngestProfilingSession_Handler_Attaches_RawData_When_Provided()
    {
        var handler = new IngestProfilingSession.Handler(_repository, _unitOfWork, _clock);
        var command = BuildIngestCommand(rawDataUri: "s3://bucket/trace.nettrace");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasRawData.Should().BeTrue();
    }

    [Fact]
    public async Task IngestProfilingSession_Handler_Links_Release_When_Provided()
    {
        var handler = new IngestProfilingSession.Handler(_repository, _unitOfWork, _clock);
        var command = BuildIngestCommand(releaseVersion: "v3.0.0", commitSha: "cafebabe");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Add(Arg.Is<ProfilingSession>(s =>
            s.ReleaseVersion == "v3.0.0" && s.CommitSha == "cafebabe"));
    }

    [Fact]
    public async Task IngestProfilingSession_Handler_Marks_Anomalies_When_HasAnomalies_True()
    {
        var handler = new IngestProfilingSession.Handler(_repository, _unitOfWork, _clock);
        var command = BuildIngestCommand(hasAnomalies: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAnomalies.Should().BeTrue();
        _repository.Received(1).Add(Arg.Is<ProfilingSession>(s => s.HasAnomalies));
    }

    // ── Application: GetProfilingHistory handler ──────────────────────────

    [Fact]
    public async Task GetProfilingHistory_Handler_Returns_Session_List()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "payment-svc", "Production",
            ProfilingFrameType.DotNetTrace,
            WindowStart, WindowEnd, 5000, 512m, 16, FixedNow);
        _repository.ListByServiceAsync("payment-svc", "Production", 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ProfilingSession>)[session]);

        var handler = new GetProfilingHistory.Handler(_repository);
        var query = new GetProfilingHistory.Query("payment-svc", "Production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ServiceName.Should().Be("payment-svc");
    }

    [Fact]
    public async Task GetProfilingHistory_Handler_Returns_Empty_When_No_Sessions()
    {
        _repository.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ProfilingSession>)[]);

        var handler = new GetProfilingHistory.Handler(_repository);
        var query = new GetProfilingHistory.Query("unknown-svc", "Production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── Application: GetProfilingAnalysis handler ─────────────────────────

    [Fact]
    public async Task GetProfilingAnalysis_Handler_Returns_NotFound_For_Unknown_Session()
    {
        _repository.GetByIdAsync(Arg.Any<ProfilingSessionId>(), Arg.Any<CancellationToken>())
            .Returns((ProfilingSession?)null);

        var handler = new GetProfilingAnalysis.Handler(_repository);
        var query = new GetProfilingAnalysis.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetProfilingAnalysis_Handler_Returns_Full_Session_Detail()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "order-svc", "Staging",
            ProfilingFrameType.Pprof,
            WindowStart, WindowEnd, 2000, 256m, 8, FixedNow);
        _repository.GetByIdAsync(Arg.Any<ProfilingSessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetProfilingAnalysis.Handler(_repository);
        var query = new GetProfilingAnalysis.Query(session.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("order-svc");
        result.Value.Environment.Should().Be("Staging");
        result.Value.FrameType.Should().Be("Pprof");
        result.Value.TotalCpuSamples.Should().Be(2000);
        result.Value.PeakMemoryMb.Should().Be(256m);
        result.Value.PeakThreadCount.Should().Be(8);
    }

    [Fact]
    public async Task GetProfilingAnalysis_Handler_Returns_TopFramesJson_When_Present()
    {
        var session = ProfilingSession.Start(
            "tenant-1", "catalog-svc", "Production",
            ProfilingFrameType.AsyncProfiler,
            WindowStart, WindowEnd, 1000, 128m, 4, FixedNow);
        session.AttachTopFrames("[{\"method\":\"HotMethod\",\"percentage\":45.5}]");
        _repository.GetByIdAsync(Arg.Any<ProfilingSessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetProfilingAnalysis.Handler(_repository);
        var query = new GetProfilingAnalysis.Query(session.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopFramesJson.Should().NotBeNullOrWhiteSpace();
        result.Value.TopFramesJson.Should().Contain("HotMethod");
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task IngestProfilingSession_Validator_Rejects_WindowEnd_Before_WindowStart()
    {
        var validator = new IngestProfilingSession.Validator();
        var command = BuildIngestCommand() with { WindowEnd = WindowStart.AddMinutes(-1) };

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "WindowEnd");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static IngestProfilingSession.Command BuildIngestCommand(
        string? topFramesJson = null,
        string? rawDataUri = null,
        string? rawDataHash = null,
        string? releaseVersion = null,
        string? commitSha = null,
        bool hasAnomalies = false) =>
        new(
            TenantId: "tenant-1",
            ServiceName: "payment-svc",
            Environment: "Production",
            FrameType: ProfilingFrameType.DotNetTrace,
            WindowStart: WindowStart,
            WindowEnd: WindowEnd,
            TotalCpuSamples: 5000,
            PeakMemoryMb: 512.5m,
            PeakThreadCount: 32,
            TopFramesJson: topFramesJson,
            RawDataUri: rawDataUri,
            RawDataHash: rawDataHash,
            ReleaseVersion: releaseVersion,
            CommitSha: commitSha,
            HasAnomalies: hasAnomalies);
}
