using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ApplyGovernancePack;
using NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary;
using NexTraceOne.Governance.Application.Features.GetEvidencePackage;
using NexTraceOne.Governance.Application.Features.GetPackApplicability;
using NexTraceOne.Governance.Application.Features.GetPackCoverage;
using NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary;
using NexTraceOne.Governance.Application.Features.ListEvidencePackages;
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
    private readonly IGovernanceRolloutRecordRepository _rolloutRepository = Substitute.For<IGovernanceRolloutRecordRepository>();
    private readonly IGovernancePackVersionRepository _versionRepository = Substitute.For<IGovernancePackVersionRepository>();
    private readonly IComplianceGapRepository _gapRepository = Substitute.For<IComplianceGapRepository>();
    private readonly IEvidencePackageRepository _evidencePackageRepository = Substitute.For<IEvidencePackageRepository>();
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
        var packId = new GovernancePackId(Guid.NewGuid());
        var versionId = new GovernancePackVersionId(Guid.NewGuid());
        _rolloutRepository.ListByPackIdAsync(packId, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>
            {
                GovernanceRolloutRecord.Create(packId, versionId, "payments", GovernanceScopeType.Domain, EnforcementMode.Required, "admin@company.com")
            });

        var handler = new GetPackApplicability.Handler(_rolloutRepository);
        var query = new GetPackApplicability.Query(packId.Value.ToString());

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

    [Fact]
    public async Task GetPackApplicability_InvalidPackId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetPackApplicability.Handler(_rolloutRepository);

        // Act
        var result = await handler.Handle(new GetPackApplicability.Query("invalid"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task ListEvidencePackages_ShouldReturnRepositoryDataAndNotSimulated()
    {
        // Arrange
        var package = EvidencePackage.Create("Q1 Evidence", "Quarterly evidence", "quarterly-review", "auditor@company.com");
        var item = EvidenceItem.Create(package.Id, EvidenceType.Approval, "Approval", "Approved", "governance", "REF-1", "auditor@company.com", DateTimeOffset.UtcNow);
        package.AddItem(item);
        package.Seal();

        _evidencePackageRepository.ListAsync(Arg.Any<string?>(), Arg.Any<EvidencePackageStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<EvidencePackage> { package });

        var handler = new ListEvidencePackages.Handler(_evidencePackageRepository);

        // Act
        var result = await handler.Handle(new ListEvidencePackages.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPackages.Should().Be(1);
        result.Value.Packages[0].ItemCount.Should().Be(1);
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetEvidencePackage_ExistingPackage_ShouldReturnDetailAndNotSimulated()
    {
        // Arrange
        var package = EvidencePackage.Create("Q1 Evidence", "Quarterly evidence", "quarterly-review", "auditor@company.com");
        package.AddItem(EvidenceItem.Create(package.Id, EvidenceType.AuditReference, "Audit", "Audit trail", "audit", "AUD-1", "auditor@company.com", DateTimeOffset.UtcNow));
        package.Seal();

        _evidencePackageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>())
            .Returns(package);

        var handler = new GetEvidencePackage.Handler(_evidencePackageRepository);

        // Act
        var result = await handler.Handle(new GetEvidencePackage.Query(package.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Package.Name.Should().Be("Q1 Evidence");
        result.Value.Package.Items.Should().HaveCount(1);
        result.Value.IsSimulated.Should().BeFalse();
    }

    // ── GetPackCoverage ──

    [Fact]
    public async Task GetPackCoverage_ShouldReturnCoverageItems()
    {
        // Arrange
        var packId = Guid.NewGuid();
        var packIdTyped = new GovernancePackId(packId);

        var rollouts = new List<GovernanceRolloutRecord>
        {
            GovernanceRolloutRecord.Create(
                packIdTyped,
                new GovernancePackVersionId(Guid.NewGuid()),
                "payments",
                GovernanceScopeType.Domain,
                EnforcementMode.Required,
                "admin")
        };
        rollouts[0].MarkCompleted();

        _rolloutRepository.ListAsync(
            Arg.Is<GovernancePackId>(id => id.Value == packId),
            Arg.Any<GovernanceScopeType?>(),
            Arg.Any<string?>(),
            Arg.Is(RolloutStatus.Completed),
            Arg.Any<CancellationToken>()).Returns(rollouts);

        _versionRepository.GetLatestByPackIdAsync(
            Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);

        _gapRepository.ListAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceGap>());

        var handler = new GetPackCoverage.Handler(_rolloutRepository, _versionRepository, _gapRepository);
        var query = new GetPackCoverage.Query(packId.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.TotalScopes.Should().BeGreaterThan(0);
        result.Value.Items.Should().ContainSingle(i => i.ScopeType == "Domain" && i.ScopeValue == "payments");
    }

    [Fact]
    public async Task GetPackCoverage_ItemsShouldHaveConsistentCounts()
    {
        // Arrange
        var packId = Guid.NewGuid();
        var packIdTyped = new GovernancePackId(packId);
        var versionId = new GovernancePackVersionId(Guid.NewGuid());

        var rollouts = new List<GovernanceRolloutRecord>
        {
            GovernanceRolloutRecord.Create(
                packIdTyped, versionId, "payments",
                GovernanceScopeType.Domain, EnforcementMode.Required, "admin"),
            GovernanceRolloutRecord.Create(
                packIdTyped, versionId, "platform-core",
                GovernanceScopeType.Team, EnforcementMode.Advisory, "admin")
        };
        rollouts.ForEach(r => r.MarkCompleted());

        _rolloutRepository.ListAsync(
            Arg.Any<GovernancePackId>(), Arg.Any<GovernanceScopeType?>(),
            Arg.Any<string?>(), Arg.Is(RolloutStatus.Completed), Arg.Any<CancellationToken>())
            .Returns(rollouts);

        _versionRepository.GetLatestByPackIdAsync(
            Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);

        _gapRepository.ListAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceGap>());

        var handler = new GetPackCoverage.Handler(_rolloutRepository, _versionRepository, _gapRepository);

        // Act
        var result = await handler.Handle(new GetPackCoverage.Query(packId.ToString()), CancellationToken.None);

        // Assert
        result.Value.Items.Should().AllSatisfy(item =>
        {
            (item.CompliantCount + item.NonCompliantCount).Should().Be(item.TotalRules);
            item.CoveragePercent.Should().BeGreaterThanOrEqualTo(0);
            item.CoveragePercent.Should().BeLessThanOrEqualTo(100);
        });
    }
}
