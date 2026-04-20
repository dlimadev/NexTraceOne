using System.Linq;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

using ComputeFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeConfidenceBreakdown.ComputeChangeConfidenceBreakdown;
using GetFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeConfidenceBreakdown.GetChangeConfidenceBreakdown;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para ComputeChangeConfidenceBreakdown e GetChangeConfidenceBreakdown.
/// Valida cálculo de sub-scores, média ponderada, qualidade de dados e casos de falha.
/// </summary>
public sealed class ChangeConfidenceBreakdownTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IChangeConfidenceBreakdownRepository _breakdownRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public ChangeConfidenceBreakdownTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto?)null);
    }

    private ComputeFeature.Handler CreateComputeHandler() =>
        new(_breakdownRepo, _releaseRepo, _uow, _clock, _configService);

    private GetFeature.Handler CreateGetHandler() => new(_breakdownRepo);

    private static Release MakeRelease()
        => Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.NewGuid(),
            serviceName: "svc-test",
            version: "1.0.0",
            environment: "Production",
            pipelineSource: "ci",
            commitSha: "abc123",
            createdAt: new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

    private static ComputeFeature.Command DefaultCommand(Guid releaseId) => new(
        ReleaseId: releaseId,
        Environment: "Production",
        BlastSurfaceConsumers: 2,
        CanaryAvailable: true,
        CanaryErrorRate: 0.02m,
        PreProdBaselineAvailable: true,
        PreProdDeltaPercent: 0.05m,
        TestCoveragePercent: 80m,
        ContractBreakingChanges: 0,
        HistoricalRegressionCount: 1);

    // ── ComputeChangeConfidenceBreakdown Tests ───────────────────────────────

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_ValidInput_ReturnsBreakdownWithSevenSubScores()
    {
        var release = MakeRelease();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateComputeHandler().Handle(DefaultCommand(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SubScores.Should().HaveCount(7);
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.AggregatedScore.Should().BeInRange(0m, 100m);
        result.Value.ComputedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_CanaryUnavailable_SubScoreIsLowConfidenceWithSimulatedNote()
    {
        var release = MakeRelease();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var command = DefaultCommand(release.Id.Value) with { CanaryAvailable = false };
        var result = await CreateComputeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var canarySubScore = result.Value.SubScores.First(s => s.SubScoreType == "CanarySignal");
        canarySubScore.Confidence.Should().Be("Low");
        canarySubScore.SimulatedNote.Should().NotBeNullOrEmpty();
        canarySubScore.Value.Should().Be(50m);
    }

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_PreProdUnavailable_SubScoreIsLowConfidenceWithSimulatedNote()
    {
        var release = MakeRelease();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var command = DefaultCommand(release.Id.Value) with { PreProdBaselineAvailable = false };
        var result = await CreateComputeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var preProdSubScore = result.Value.SubScores.First(s => s.SubScoreType == "PreProdDelta");
        preProdSubScore.Confidence.Should().Be("Low");
        preProdSubScore.SimulatedNote.Should().NotBeNullOrEmpty();
        preProdSubScore.Value.Should().Be(50m);
    }

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_HighTestCoverage_SubScoreIsHigh()
    {
        var release = MakeRelease();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var command = DefaultCommand(release.Id.Value) with { TestCoveragePercent = 95m };
        var result = await CreateComputeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var testSubScore = result.Value.SubScores.First(s => s.SubScoreType == "TestCoverage");
        testSubScore.Value.Should().Be(95m);
        testSubScore.Confidence.Should().Be("High");
    }

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_BreakingContracts_ReducesContractStabilityScore()
    {
        var release = MakeRelease();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var command = DefaultCommand(release.Id.Value) with { ContractBreakingChanges = 2 };
        var result = await CreateComputeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var contractSubScore = result.Value.SubScores.First(s => s.SubScoreType == "ContractStability");
        contractSubScore.Value.Should().Be(50m); // 100 - 2 * 25 = 50
    }

    [Fact]
    public async Task ComputeChangeConfidenceBreakdown_ReleaseNotFound_ReturnsFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await CreateComputeHandler().Handle(
            DefaultCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    // ── GetChangeConfidenceBreakdown Tests ────────────────────────────────────

    [Fact]
    public async Task GetChangeConfidenceBreakdown_NotFound_ReturnsFailure()
    {
        _breakdownRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeConfidenceBreakdown?)null);

        var result = await CreateGetHandler().Handle(
            new GetFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChangeConfidenceBreakdown_Found_ReturnsMappedResponse()
    {
        var releaseId = ReleaseId.New();
        var subScores = new[]
        {
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.TestCoverage, 80m, 0.15m, ConfidenceDataQuality.High, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.ContractStability, 100m, 0.20m, ConfidenceDataQuality.High, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.HistoricalRegression, 80m, 0.15m, ConfidenceDataQuality.Medium, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.BlastSurface, 90m, 0.15m, ConfidenceDataQuality.High, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.DependencyHealth, 75m, 0.10m, ConfidenceDataQuality.Low, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.CanarySignal, 98m, 0.10m, ConfidenceDataQuality.High, "reason", ["citation://test"]),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.PreProdDelta, 95m, 0.15m, ConfidenceDataQuality.High, "reason", ["citation://test"]),
        };
        var breakdown = ChangeConfidenceBreakdown.Create(releaseId, subScores, FixedNow);

        _breakdownRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(breakdown);

        var result = await CreateGetHandler().Handle(
            new GetFeature.Query(releaseId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(releaseId.Value);
        result.Value.SubScores.Should().HaveCount(7);
        result.Value.ScoreVersion.Should().Be("2.0");
        result.Value.AggregatedScore.Should().BeInRange(0m, 100m);
        result.Value.ComputedAt.Should().Be(FixedNow);
    }

    // ── Weighted Average Tests ────────────────────────────────────────────────

    [Fact]
    public void ChangeConfidenceBreakdown_AggregatedScore_IsWeightedAverage()
    {
        var releaseId = ReleaseId.New();
        var subScores = new[]
        {
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.TestCoverage, 80m, 1m, ConfidenceDataQuality.High, "reason", []),
            ChangeConfidenceSubScore.Create(ConfidenceSubScoreType.ContractStability, 60m, 3m, ConfidenceDataQuality.High, "reason", []),
        };

        var breakdown = ChangeConfidenceBreakdown.Create(releaseId, subScores, FixedNow);

        // (80*1 + 60*3) / (1+3) = (80 + 180) / 4 = 65
        breakdown.AggregatedScore.Should().Be(65m);
    }

    // ── Validator Tests ───────────────────────────────────────────────────────

    [Fact]
    public void ComputeValidator_EmptyReleaseId_ReturnsError()
    {
        var validator = new ComputeFeature.Validator();
        var command = DefaultCommand(Guid.Empty);
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void ComputeValidator_NegativeBlastSurface_ReturnsError()
    {
        var validator = new ComputeFeature.Validator();
        var command = DefaultCommand(Guid.NewGuid()) with { BlastSurfaceConsumers = -1 };
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BlastSurfaceConsumers");
    }

    [Fact]
    public void ComputeValidator_ValidCommand_Passes()
    {
        var validator = new ComputeFeature.Validator();
        var result = validator.Validate(DefaultCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetValidator_EmptyReleaseId_ReturnsError()
    {
        var validator = new GetFeature.Validator();
        var result = validator.Validate(new GetFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void GetValidator_ValidReleaseId_Passes()
    {
        var validator = new GetFeature.Validator();
        var result = validator.Validate(new GetFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
