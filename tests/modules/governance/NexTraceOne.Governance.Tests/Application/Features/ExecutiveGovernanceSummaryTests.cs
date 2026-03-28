using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ApplyGovernancePack;
using NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary;
using NexTraceOne.Governance.Application.Features.GetPackApplicability;
using NexTraceOne.Governance.Application.Features.GetPackCoverage;
using NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para features de resumo executivo de governança.
/// Handlers sem dependências que retornam dados estáticos/demonstrativos.
/// </summary>
public sealed class ExecutiveGovernanceSummaryTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IGovernanceDomainRepository _domainRepository = Substitute.For<IGovernanceDomainRepository>();
    private readonly ICatalogGraphModule _catalogGraph = Substitute.For<ICatalogGraphModule>();

    // ── GetDomainGovernanceSummary ──

    [Fact]
    public async Task GetDomainGovernanceSummary_ShouldReturnDimensions()
    {
        // Arrange
        var domain = GovernanceDomain.Create("domain-commerce", "Commerce");
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>()).Returns(domain);
        _catalogGraph.CountServicesByDomainAsync(domain.Name, Arg.Any<CancellationToken>()).Returns(4);

        var handler = new GetDomainGovernanceSummary.Handler(_domainRepository, _catalogGraph);
        var query = new GetDomainGovernanceSummary.Query(domain.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be(domain.Id.Value.ToString());
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.OverallMaturity.Should().NotBeNullOrWhiteSpace();
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetDomainGovernanceSummary_DimensionsShouldHaveNonNegativeScoresAndTrends()
    {
        // Arrange
        var domain = GovernanceDomain.Create("test-domain", "Test Domain");
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>()).Returns(domain);
        _catalogGraph.CountServicesByDomainAsync(domain.Name, Arg.Any<CancellationToken>()).Returns(2);
        var handler = new GetDomainGovernanceSummary.Handler(_domainRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetDomainGovernanceSummary.Query(domain.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.Value.Dimensions.Should().AllSatisfy(d =>
        {
            d.Dimension.Should().NotBeNullOrWhiteSpace();
            d.Level.Should().NotBeNullOrWhiteSpace();
            d.Score.Should().BeGreaterThanOrEqualTo(0);
            d.Trend.Should().NotBeNullOrWhiteSpace();
            d.Trend.Should().BeOneOf("Stable", "Improving");
        });
    }

    // ── GetTeamGovernanceSummary ──

    [Fact]
    public async Task GetTeamGovernanceSummary_ShouldReturnDimensions()
    {
        // Arrange
        var team = Team.Create("team-commerce", "Commerce Team");
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>()).Returns(team);
        _catalogGraph.CountServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>()).Returns(3);
        _catalogGraph.ListContractsByTeamAsync(team.Name, Arg.Any<CancellationToken>()).Returns([]);
        _catalogGraph.ListCrossTeamDependenciesAsync(team.Name, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetTeamGovernanceSummary.Handler(_teamRepository, _catalogGraph);
        var query = new GetTeamGovernanceSummary.Query(team.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be(team.Id.Value.ToString());
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.OverallMaturity.Should().NotBeNullOrWhiteSpace();
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetTeamGovernanceSummary_ShouldReturnCoverageMetrics()
    {
        // Arrange
        var team = Team.Create("team-test", "Team Test");
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>()).Returns(team);
        _catalogGraph.CountServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>()).Returns(3);
        _catalogGraph.ListContractsByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns([new("ctr-1", "Orders API", "REST", "v1", "Published")]);
        _catalogGraph.ListCrossTeamDependenciesAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns([new("dep-1", "orders-api", "identity-api", Guid.NewGuid().ToString(), "Identity", "Synchronous")]);

        var handler = new GetTeamGovernanceSummary.Handler(_teamRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetTeamGovernanceSummary.Query(team.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.Value.OwnershipCoverage.Should().BeGreaterThan(0);
        result.Value.ContractCoverage.Should().BeGreaterThan(0);
        result.Value.DocumentationCoverage.Should().BeGreaterThan(0);
        result.Value.ReliabilityScore.Should().BeGreaterThan(0);
    }

    // ── ApplyGovernancePack ──

    [Fact]
    public async Task ApplyGovernancePack_WithValidPack_ShouldReturnRolloutId()
    {
        // Arrange
        var pack = GovernancePack.Create("test-pack", "Test Pack", "desc", GovernanceRuleCategory.Contracts);
        var version = GovernancePackVersion.Create(
            pack.Id, "1.0.0", Array.Empty<GovernanceRuleBinding>(),
            EnforcementMode.Advisory, "Initial version", "admin@test.com");

        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        versionRepo.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(version);

        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(
            pack.Id.Value.ToString(), "Domain", "payments", "Blocking", "admin@company.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RolloutId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Value.RolloutId, out _).Should().BeTrue();
        result.Value.PackId.Should().Be(pack.Id.Value.ToString());
        result.Value.Status.Should().Be("Completed");
        await rolloutRepo.Received(1).AddAsync(Arg.Any<GovernanceRolloutRecord>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GetPackApplicability ──

    [Fact]
    public async Task GetPackApplicability_ShouldReturnScopes()
    {
        // Arrange
        var handler = new GetPackApplicability.Handler();
        var query = new GetPackApplicability.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scopes.Should().NotBeEmpty();
        result.Value.Scopes.Should().AllSatisfy(s =>
        {
            s.AppliedBy.Should().NotBeNullOrWhiteSpace();
            s.ScopeValue.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── GetPackCoverage ──

    [Fact]
    public async Task GetPackCoverage_ShouldReturnCoverageItems()
    {
        // Arrange
        var handler = new GetPackCoverage.Handler();
        var query = new GetPackCoverage.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.OverallCoveragePercent.Should().BeGreaterThan(0);
        result.Value.TotalScopes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPackCoverage_ItemsShouldHaveConsistentCounts()
    {
        // Arrange
        var handler = new GetPackCoverage.Handler();

        // Act
        var result = await handler.Handle(new GetPackCoverage.Query("test"), CancellationToken.None);

        // Assert
        result.Value.Items.Should().AllSatisfy(item =>
        {
            (item.CompliantCount + item.NonCompliantCount).Should().Be(item.TotalRules);
            item.CoveragePercent.Should().BeGreaterThanOrEqualTo(0);
            item.CoveragePercent.Should().BeLessThanOrEqualTo(100);
        });
    }
}
