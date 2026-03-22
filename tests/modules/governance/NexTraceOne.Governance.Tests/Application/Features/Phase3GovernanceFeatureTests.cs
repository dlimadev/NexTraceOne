using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetGovernancePack;
using NexTraceOne.Governance.Application.Features.GetTeamDetail;
using NexTraceOne.Governance.Application.Features.ListGovernancePacks;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade Phase 3 — scope counting, IngestionSource behaviours e cross-team enrichment.
/// </summary>
public sealed class Phase3GovernanceFeatureTests
{
    private readonly IGovernancePackRepository _packRepository = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernancePackVersionRepository _versionRepository = Substitute.For<IGovernancePackVersionRepository>();
    private readonly IGovernanceRolloutRecordRepository _rolloutRecordRepository = Substitute.For<IGovernanceRolloutRecordRepository>();
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly ICatalogGraphModule _catalogGraph = Substitute.For<ICatalogGraphModule>();

    // ── ListGovernancePacks — Scope Counting ──

    [Fact]
    public async Task ListPacks_WithRolloutRecords_ShouldReturnRealScopeCount()
    {
        // Arrange
        var pack = GovernancePack.Create("pack-a", "Pack A", "Desc", GovernanceRuleCategory.Contracts);
        var packId = pack.Id;
        var versionId = new GovernancePackVersionId(Guid.NewGuid());

        var rolloutCompleted1 = GovernanceRolloutRecord.Create(
            packId, versionId, "team-alpha", GovernanceScopeType.Team, EnforcementMode.Required, "admin");
        rolloutCompleted1.MarkCompleted();

        var rolloutCompleted2 = GovernanceRolloutRecord.Create(
            packId, versionId, "team-beta", GovernanceScopeType.Team, EnforcementMode.Advisory, "admin");
        rolloutCompleted2.MarkCompleted();

        var rolloutPending = GovernanceRolloutRecord.Create(
            packId, versionId, "team-gamma", GovernanceScopeType.Team, EnforcementMode.Blocking, "admin");

        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { pack });
        _versionRepository.GetLatestByPackIdAsync(packId, Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRecordRepository.ListByPackIdAsync(packId, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord> { rolloutCompleted1, rolloutCompleted2, rolloutPending });

        var handler = new ListGovernancePacks.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new ListGovernancePacks.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Should().HaveCount(1);
        result.Value.Packs[0].ScopeCount.Should().Be(2);
    }

    [Fact]
    public async Task ListPacks_WithNoRollouts_ShouldReturnZeroScopeCount()
    {
        // Arrange
        var pack = GovernancePack.Create("pack-empty", "Empty Pack", null, GovernanceRuleCategory.Changes);

        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { pack });
        _versionRepository.GetLatestByPackIdAsync(pack.Id, Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRecordRepository.ListByPackIdAsync(pack.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new ListGovernancePacks.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new ListGovernancePacks.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Should().HaveCount(1);
        result.Value.Packs[0].ScopeCount.Should().Be(0);
    }

    // ── GetGovernancePack — Scope Counting ──

    [Fact]
    public async Task GetPack_WithRolloutRecords_ShouldReturnRealScopeCountAndScopes()
    {
        // Arrange
        var pack = GovernancePack.Create("pack-scoped", "Scoped Pack", "Desc", GovernanceRuleCategory.Reliability);
        var versionId = new GovernancePackVersionId(Guid.NewGuid());

        var rolloutCompleted = GovernanceRolloutRecord.Create(
            pack.Id, versionId, "team-alpha", GovernanceScopeType.Team, EnforcementMode.Required, "admin");
        rolloutCompleted.MarkCompleted();

        var rolloutPending = GovernanceRolloutRecord.Create(
            pack.Id, versionId, "team-beta", GovernanceScopeType.Team, EnforcementMode.Advisory, "admin");

        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _versionRepository.ListByPackIdAsync(pack.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePackVersion>());
        _versionRepository.GetLatestByPackIdAsync(pack.Id, Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRecordRepository.ListByPackIdAsync(pack.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord> { rolloutCompleted, rolloutPending });

        var handler = new GetGovernancePack.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new GetGovernancePack.Query(pack.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Pack.ScopeCount.Should().Be(1);
        result.Value.Pack.Scopes.Should().HaveCount(1);
        result.Value.Pack.Scopes[0].ScopeValue.Should().Be("team-alpha");
        result.Value.Pack.Scopes[0].ScopeType.Should().Be(GovernanceScopeType.Team);
    }

    // ── IngestionSource — LastProcessedAt ──

    [Fact]
    public void IngestionSource_RecordDataReceived_ShouldUpdateLastProcessedAt()
    {
        // Arrange
        var connectorId = new IntegrationConnectorId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var source = IngestionSource.Create(connectorId, "Webhook", "Webhook", "Changes", "Desc", null, 30, now);

        var processTime = now.AddMinutes(5);

        // Act
        source.RecordDataReceived(10, processTime);

        // Assert
        source.LastProcessedAt.Should().Be(processTime);
        source.LastDataReceivedAt.Should().Be(processTime);
        source.DataItemsProcessed.Should().Be(10);
    }

    [Fact]
    public void IngestionSource_RecordProcessingCompleted_ShouldOnlyUpdateLastProcessedAt()
    {
        // Arrange
        var connectorId = new IntegrationConnectorId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var source = IngestionSource.Create(connectorId, "Poller", "API Polling", "Runtime", null, null, 60, now);

        var processTime = now.AddMinutes(10);

        // Act
        source.RecordProcessingCompleted(processTime);

        // Assert
        source.LastProcessedAt.Should().Be(processTime);
        source.LastDataReceivedAt.Should().BeNull();
        source.DataItemsProcessed.Should().Be(0);
    }

    // ── GetTeamDetail — Cross-Team Enrichment ──

    [Fact]
    public async Task GetTeamDetail_WithCatalogData_ShouldReturnEnrichedServices()
    {
        // Arrange
        var team = Team.Create("platform-core", "Platform Core", "Core team", "Engineering");

        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(team);
        _catalogGraph.CountServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(2);
        _catalogGraph.ListServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<TeamServiceInfo>
            {
                new("svc-1", "auth-service", "Identity", "High", "Team"),
                new("svc-2", "api-gateway", "Platform", "Critical", "Team")
            });
        _catalogGraph.ListContractsByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<TeamContractInfo>
            {
                new("ctr-1", "Auth API", "REST", "1.0.0", "Published")
            });
        _catalogGraph.ListCrossTeamDependenciesAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<CrossTeamDependencyInfo>
            {
                new("dep-1", "auth-service", "user-service", "team-users-id", "team-users", "REST")
            });

        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetTeamDetail.Query(team.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().HaveCount(2);
        result.Value.Contracts.Should().HaveCount(1);
        result.Value.CrossTeamDependencies.Should().HaveCount(1);
        result.Value.ServiceCount.Should().Be(2);
        result.Value.ContractCount.Should().Be(1);
        result.Value.Services[0].Name.Should().Be("auth-service");
        result.Value.Contracts[0].Name.Should().Be("Auth API");
        result.Value.CrossTeamDependencies[0].TargetTeamName.Should().Be("team-users");
    }

    [Fact]
    public async Task GetTeamDetail_WithEmptyCatalog_ShouldReturnEmptyCollections()
    {
        // Arrange
        var team = Team.Create("empty-team", "Empty Team", null, null);

        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(team);
        _catalogGraph.CountServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(0);
        _catalogGraph.ListServicesByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<TeamServiceInfo>());
        _catalogGraph.ListContractsByTeamAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<TeamContractInfo>());
        _catalogGraph.ListCrossTeamDependenciesAsync(team.Name, Arg.Any<CancellationToken>())
            .Returns(new List<CrossTeamDependencyInfo>());

        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetTeamDetail.Query(team.Id.Value.ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().BeEmpty();
        result.Value.Contracts.Should().BeEmpty();
        result.Value.CrossTeamDependencies.Should().BeEmpty();
        result.Value.ServiceCount.Should().Be(0);
        result.Value.ContractCount.Should().Be(0);
    }
}
