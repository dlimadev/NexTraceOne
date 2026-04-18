using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using GetCanaryRolloutStatusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetCanaryRolloutStatus.GetCanaryRolloutStatus;
using GetChangeIntelligenceSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;
using GetHistoricalPatternInsightFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetHistoricalPatternInsight.GetHistoricalPatternInsight;
using ResolveReleaseByExternalKeyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ResolveReleaseByExternalKey.ResolveReleaseByExternalKey;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes do padrão "Natural Key → Intelligence" para os novos endpoints by-external da Ingestion API.
/// Verifica que os endpoints by-external/intelligence, by-external/canary e
/// by-external/historical-pattern resolvem a chave natural antes de consultar os dados,
/// nunca expondo GUIDs internos ao sistema externo.
/// </summary>
public sealed class NaturalKeyIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IChangeScoreRepository _scoreRepo = Substitute.For<IChangeScoreRepository>();
    private readonly IBlastRadiusRepository _blastRepo = Substitute.For<IBlastRadiusRepository>();
    private readonly IExternalMarkerRepository _markerRepo = Substitute.For<IExternalMarkerRepository>();
    private readonly IReleaseBaselineRepository _baselineRepo = Substitute.For<IReleaseBaselineRepository>();
    private readonly IPostReleaseReviewRepository _reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
    private readonly IRollbackAssessmentRepository _rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
    private readonly IChangeEventRepository _eventRepo = Substitute.For<IChangeEventRepository>();
    private readonly ICanaryRolloutRepository _canaryRepo = Substitute.For<ICanaryRolloutRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    // ── Helpers ────────────────────────────────────────────────────────────

    private static Release CreateReleaseWithExternalKey(
        string externalReleaseId = "jenkins-build-99",
        string externalSystem = "jenkins",
        string service = "svc-checkout")
        => Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.Empty,
            serviceName: service,
            version: "3.0.0",
            environment: "staging",
            pipelineSource: $"External:{externalSystem}",
            commitSha: "deadbeef",
            createdAt: FixedNow,
            externalReleaseId: externalReleaseId,
            externalSystem: externalSystem);

    private void SetupReleaseFound(Release release)
    {
        _releaseRepo.GetByExternalKeyAsync(
            release.ExternalReleaseId!, release.ExternalSystem!, Arg.Any<CancellationToken>())
            .Returns(release);
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
    }

    private void SetupReleaseNotFound(string externalReleaseId, string externalSystem)
    {
        _releaseRepo.GetByExternalKeyAsync(externalReleaseId, externalSystem, Arg.Any<CancellationToken>())
            .Returns((Release?)null);
    }

    private void SetupEmptyOptionalData()
    {
        _scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        _blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        _markerRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExternalMarker>());
        _baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseBaseline?)null);
        _reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((PostReleaseReview?)null);
        _rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);
        _eventRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeEvent>());
    }

    // ── ResolveReleaseByExternalKey (prerequisite of all three new routes) ──

    [Fact]
    public async Task ResolveHandler_WithValidExternalKey_ShouldReturnReleaseId()
    {
        var release = CreateReleaseWithExternalKey();
        _releaseRepo.GetByExternalKeyAsync("jenkins-build-99", "jenkins", Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var result = await sut.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-99", "jenkins"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.ExternalReleaseId.Should().Be("jenkins-build-99");
        result.Value.ExternalSystem.Should().Be("jenkins");
    }

    [Fact]
    public async Task ResolveHandler_WhenNotFound_ShouldReturnFailure()
    {
        SetupReleaseNotFound("unknown-build", "jenkins");

        var sut = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var result = await sut.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("unknown-build", "jenkins"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── GET by-external/.../intelligence — two-step verify ────────────────

    [Fact]
    public async Task ChangeIntelligenceSummary_ViaExternalKey_ShouldReturnReleaseData()
    {
        var release = CreateReleaseWithExternalKey();
        SetupReleaseFound(release);
        SetupEmptyOptionalData();

        // Step 1: resolve external key → internal GUID
        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-99", "jenkins"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeTrue();

        // Step 2: call intelligence handler with the resolved GUID
        var intelligenceHandler = new GetChangeIntelligenceSummaryFeature.Handler(
            _releaseRepo, _scoreRepo, _blastRepo, _markerRepo,
            _baselineRepo, _reviewRepo, _rollbackRepo, _eventRepo);

        var result = await intelligenceHandler.Handle(
            new GetChangeIntelligenceSummaryFeature.Query(resolveResult.Value.ReleaseId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Release.ServiceName.Should().Be("svc-checkout");
        result.Value.Release.Version.Should().Be("3.0.0");
        result.Value.Score.Should().BeNull();
        result.Value.BlastRadius.Should().BeNull();
    }

    [Fact]
    public async Task ChangeIntelligenceSummary_WhenExternalKeyNotFound_ShouldStopAtResolveStep()
    {
        SetupReleaseNotFound("missing-build", "gitlab");

        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("missing-build", "gitlab"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeFalse();
        // Intelligence handler should never be reached — no internal GUID exposed
        await _releaseRepo.DidNotReceive().GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>());
    }

    // ── GET by-external/.../canary — two-step verify ──────────────────────

    [Fact]
    public async Task CanaryRolloutStatus_ViaExternalKey_WhenNoCanaryData_ShouldReturnUnknown()
    {
        var release = CreateReleaseWithExternalKey();
        SetupReleaseFound(release);
        _canaryRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((CanaryRollout?)null);
        _canaryRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanaryRollout>());

        // Step 1: resolve
        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-99", "jenkins"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeTrue();

        // Step 2: canary status
        var canaryHandler = new GetCanaryRolloutStatusFeature.Handler(_releaseRepo, _canaryRepo);
        var result = await canaryHandler.Handle(
            new GetCanaryRolloutStatusFeature.Query(resolveResult.Value.ReleaseId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasData.Should().BeFalse();
        result.Value.ConfidenceBoost.Should().Be("Unknown");
    }

    [Fact]
    public async Task CanaryRolloutStatus_WhenExternalKeyNotFound_ShouldStopAtResolveStep()
    {
        SetupReleaseNotFound("unknown-canary", "spinnaker");

        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("unknown-canary", "spinnaker"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeFalse();
        await _canaryRepo.DidNotReceive().GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>());
    }

    // ── GET by-external/.../historical-pattern — two-step verify ──────────

    [Fact]
    public async Task HistoricalPatternInsight_ViaExternalKey_WhenNoHistory_ShouldReturnInsufficient()
    {
        var release = CreateReleaseWithExternalKey();
        SetupReleaseFound(release);
        _dateTimeProvider.UtcNow.Returns(FixedNow);

        _releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ChangeLevel>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        // Step 1: resolve
        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-99", "jenkins"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeTrue();

        // Step 2: historical pattern
        var patternHandler = new GetHistoricalPatternInsightFeature.Handler(_releaseRepo, _dateTimeProvider);
        var result = await patternHandler.Handle(
            new GetHistoricalPatternInsightFeature.Query(resolveResult.Value.ReleaseId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSamples.Should().Be(0);
        result.Value.PatternRisk.Should().Be("Insufficient");
    }

    [Fact]
    public async Task HistoricalPatternInsight_WhenExternalKeyNotFound_ShouldStopAtResolveStep()
    {
        SetupReleaseNotFound("unknown-pipeline", "azure-devops");

        var resolveHandler = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var resolveResult = await resolveHandler.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("unknown-pipeline", "azure-devops"),
            CancellationToken.None);

        resolveResult.IsSuccess.Should().BeFalse();
        await _releaseRepo.DidNotReceive().GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>());
    }

    // ── Validators ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "jenkins")]
    [InlineData("build-123", "")]
    public void ResolveValidator_WithInvalidInput_ShouldFail(string externalReleaseId, string externalSystem)
    {
        var validator = new ResolveReleaseByExternalKeyFeature.Validator();
        var result = validator.Validate(
            new ResolveReleaseByExternalKeyFeature.Query(externalReleaseId, externalSystem));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResolveValidator_WithValidInput_ShouldPass()
    {
        var validator = new ResolveReleaseByExternalKeyFeature.Validator();
        var result = validator.Validate(
            new ResolveReleaseByExternalKeyFeature.Query("build-42", "gitlab"));
        result.IsValid.Should().BeTrue();
    }
}
