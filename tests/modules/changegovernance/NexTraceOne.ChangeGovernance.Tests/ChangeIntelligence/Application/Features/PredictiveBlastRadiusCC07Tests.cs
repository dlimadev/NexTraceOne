using System.Linq;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPredictiveBlastRadius;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ActivateSpectralPackage;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetSpectralMarketplace;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para CC-07 (Predictive Blast Radius v2) e CC-08 (Spectral Marketplace).
/// </summary>
public sealed class PredictiveBlastRadiusCC07Tests
{
    private readonly IBlastRadiusRepository _blastRepo = Substitute.For<IBlastRadiusRepository>();
    private readonly IRulesetRepository _rulesetRepo = Substitute.For<IRulesetRepository>();
    private readonly IRulesetGovernanceUnitOfWork _uow = Substitute.For<IRulesetGovernanceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTimeOffset _now = new(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

    public PredictiveBlastRadiusCC07Tests() => _clock.UtcNow.Returns(_now);

    // ── CC-07: GetPredictiveBlastRadius ──────────────────────────────────────

    [Fact]
    public async Task GetPredictiveBlastRadius_ReportNotFound_ReturnsFailure()
    {
        _blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);

        var handler = new GetPredictiveBlastRadius.Handler(_blastRepo);

        var result = await handler.Handle(
            new GetPredictiveBlastRadius.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetPredictiveBlastRadius_WithConsumers_ReturnsProbabilities()
    {
        var releaseId = ReleaseId.From(Guid.NewGuid());
        var report = BlastRadiusReport.Calculate(
            releaseId,
            Guid.NewGuid(),
            new[] { "svc-payments", "svc-orders" },
            new[] { "svc-reporting" },
            _now);

        _blastRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(report);

        var handler = new GetPredictiveBlastRadius.Handler(_blastRepo);

        var result = await handler.Handle(
            new GetPredictiveBlastRadius.Query(releaseId.Value, 90, 10.0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAffectedConsumers.Should().Be(3);
        result.Value.ProbabilityOfRegressionByConsumer.Should().ContainKey("svc-payments");
        result.Value.ProbabilityOfRegressionByConsumer.Should().ContainKey("svc-orders");
        result.Value.ProbabilityOfRegressionByConsumer.Should().ContainKey("svc-reporting");
    }

    [Fact]
    public async Task GetPredictiveBlastRadius_DirectConsumers_HaveHigherProbability()
    {
        var releaseId = ReleaseId.From(Guid.NewGuid());
        var direct = new[] { "svc-direct" };
        var transitive = new[] { "svc-transitive" };
        var report = BlastRadiusReport.Calculate(releaseId, Guid.NewGuid(), direct, transitive, _now);

        _blastRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(report);

        var handler = new GetPredictiveBlastRadius.Handler(_blastRepo);
        var result = await handler.Handle(
            new GetPredictiveBlastRadius.Query(releaseId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var directProb = result.Value.ProbabilityOfRegressionByConsumer["svc-direct"];
        var transitiveProb = result.Value.ProbabilityOfRegressionByConsumer["svc-transitive"];
        directProb.Should().BeGreaterThan(transitiveProb);
    }

    [Fact]
    public async Task GetPredictiveBlastRadius_NoConsumers_ReturnsNoneRisk()
    {
        var releaseId = ReleaseId.From(Guid.NewGuid());
        var report = BlastRadiusReport.Calculate(releaseId, Guid.NewGuid(), [], [], _now);

        _blastRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(report);

        var handler = new GetPredictiveBlastRadius.Handler(_blastRepo);
        var result = await handler.Handle(
            new GetPredictiveBlastRadius.Query(releaseId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallRegressionRisk.Should().Be("None");
        result.Value.ProbabilityOfRegressionByConsumer.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPredictiveBlastRadius_ReturnsConfiguredLookbackDays()
    {
        var releaseId = ReleaseId.From(Guid.NewGuid());
        var report = BlastRadiusReport.Calculate(releaseId, Guid.NewGuid(), ["svc-a"], [], _now);
        _blastRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new GetPredictiveBlastRadius.Handler(_blastRepo);
        var result = await handler.Handle(
            new GetPredictiveBlastRadius.Query(releaseId.Value, 180, 5.0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HistoricalLookbackDays.Should().Be(180);
        result.Value.MinCallFrequency.Should().Be(5.0);
    }

    // ── CC-08: GetSpectralMarketplace ─────────────────────────────────────────

    [Fact]
    public async Task GetSpectralMarketplace_ReturnsAllFourPackages()
    {
        var handler = new GetSpectralMarketplace.Handler();
        var result = await handler.Handle(new GetSpectralMarketplace.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Packages.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetSpectralMarketplace_ContainsExpectedPackageIds()
    {
        var handler = new GetSpectralMarketplace.Handler();
        var result = await handler.Handle(new GetSpectralMarketplace.Query(), CancellationToken.None);

        var ids = result.Value.Packages.Select(p => p.Id).ToList();
        ids.Should().Contain("enterprise");
        ids.Should().Contain("security");
        ids.Should().Contain("accessibility");
        ids.Should().Contain("internal-platform");
    }

    [Fact]
    public async Task GetSpectralMarketplace_EachPackageHasRules()
    {
        var handler = new GetSpectralMarketplace.Handler();
        var result = await handler.Handle(new GetSpectralMarketplace.Query(), CancellationToken.None);

        result.Value.Packages.Should().AllSatisfy(p => p.RuleCount.Should().BeGreaterThan(0));
    }

    // ── CC-08: ActivateSpectralPackage ────────────────────────────────────────

    [Fact]
    public async Task ActivateSpectralPackage_ValidPackage_CreatesRuleset()
    {
        _rulesetRepo.FindByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Ruleset?)null);

        var handler = new ActivateSpectralPackage.Handler(_rulesetRepo, _uow, _clock);
        var result = await handler.Handle(
            new ActivateSpectralPackage.Command("enterprise"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Activated.Should().BeTrue();
        result.Value.Reactivated.Should().BeFalse();
        _rulesetRepo.Received(1).Add(Arg.Any<Ruleset>());
    }

    [Fact]
    public async Task ActivateSpectralPackage_AlreadyExists_ReactivatesExisting()
    {
        var existingRuleset = Ruleset.Create(
            "spectral-marketplace/security",
            "Security Pack", "{}", RulesetType.Default, _now);
        existingRuleset.Archive();
        _rulesetRepo.FindByNameAsync("spectral-marketplace/security", Arg.Any<CancellationToken>())
            .Returns(existingRuleset);

        var handler = new ActivateSpectralPackage.Handler(_rulesetRepo, _uow, _clock);
        var result = await handler.Handle(
            new ActivateSpectralPackage.Command("security"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reactivated.Should().BeTrue();
        result.Value.Activated.Should().BeFalse();
        existingRuleset.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateSpectralPackage_InvalidPackageId_ReturnsValidationError()
    {
        var handler = new ActivateSpectralPackage.Handler(_rulesetRepo, _uow, _clock);
        var result = await handler.Handle(
            new ActivateSpectralPackage.Command("unknown-package"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
