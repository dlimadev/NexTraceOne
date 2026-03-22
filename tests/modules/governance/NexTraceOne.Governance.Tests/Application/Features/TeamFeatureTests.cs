using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateTeam;
using NexTraceOne.Governance.Application.Features.GetTeamDetail;
using NexTraceOne.Governance.Application.Features.ListTeams;
using NexTraceOne.Governance.Application.Features.UpdateTeam;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de gestão de equipas.
/// Utilizam mocks dos repositórios para verificar comportamentos dos handlers.
/// </summary>
public sealed class TeamFeatureTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly ITeamDomainLinkRepository _teamDomainLinkRepository = Substitute.For<ITeamDomainLinkRepository>();
    private readonly ICatalogGraphModule _catalogGraph = Substitute.For<ICatalogGraphModule>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // ── CreateTeam ──

    [Fact]
    public async Task CreateTeam_ValidData_ShouldReturnTeamId()
    {
        // Arrange
        _teamRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new CreateTeam.Command("platform-core", "Platform Core", "Core platform team", "Engineering");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Value.TeamId, out _).Should().BeTrue();
        await _teamRepository.Received(1).AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTeam_DuplicateName_ShouldReturnConflictError()
    {
        // Arrange
        var existing = Team.Create("platform-core", "Platform Core");
        _teamRepository.GetByNameAsync("platform-core", Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new CreateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new CreateTeam.Command("platform-core", "Platform Core", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TEAM_NAME_EXISTS");
    }

    [Fact]
    public async Task CreateTeam_WithOptionalFields_ShouldSucceed()
    {
        // Arrange
        _teamRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new CreateTeam.Command("payments", "Payments Team", "Handles payments", "Finance Division");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateTeam_WithNullOptionalFields_ShouldSucceed()
    {
        // Arrange
        _teamRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new CreateTeam.Command("minimal-team", "Minimal Team", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── ListTeams ──

    [Fact]
    public async Task ListTeams_WithData_ShouldReturnItems()
    {
        // Arrange
        var teams = new List<Team>
        {
            Team.Create("team-a", "Team A", "Description A"),
            Team.Create("team-b", "Team B", "Description B")
        };

        _teamRepository.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>())
            .Returns(teams);
        _teamDomainLinkRepository.ListByTeamIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(new List<TeamDomainLink>());
        _catalogGraph.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var handler = new ListTeams.Handler(_teamRepository, _teamDomainLinkRepository, _catalogGraph);
        var query = new ListTeams.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().HaveCount(2);
        result.Value.Teams[0].ServiceCount.Should().Be(5);
    }

    [Fact]
    public async Task ListTeams_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _teamRepository.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Team>());

        var handler = new ListTeams.Handler(_teamRepository, _teamDomainLinkRepository, _catalogGraph);
        var query = new ListTeams.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().BeEmpty();
    }

    [Fact]
    public async Task ListTeams_ShouldPopulateDeferredFields()
    {
        // Arrange
        var teams = new List<Team> { Team.Create("team-x", "Team X") };
        _teamRepository.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>())
            .Returns(teams);
        _teamDomainLinkRepository.ListByTeamIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(new List<TeamDomainLink>());
        _catalogGraph.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new ListTeams.Handler(_teamRepository, _teamDomainLinkRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new ListTeams.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Teams[0].DeferredFields.Should().NotBeEmpty();
        result.Value.Teams[0].MaturityLevel.Should().Be("Developing");
    }

    // ── GetTeamDetail ──

    [Fact]
    public async Task GetTeamDetail_ValidId_ShouldReturnDetail()
    {
        // Arrange
        var team = Team.Create("commerce", "Commerce Team", "Commerce operations");
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(team);
        _catalogGraph.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(8);

        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);
        var query = new GetTeamDetail.Query(team.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("commerce");
        result.Value.DisplayName.Should().Be("Commerce Team");
        result.Value.ServiceCount.Should().Be(8);
    }

    [Fact]
    public async Task GetTeamDetail_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);
        var query = new GetTeamDetail.Query("not-a-guid");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_TEAM_ID");
    }

    [Fact]
    public async Task GetTeamDetail_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);
        var query = new GetTeamDetail.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TEAM_NOT_FOUND");
    }

    [Fact]
    public async Task GetTeamDetail_ShouldReturnDeferredFields()
    {
        // Arrange
        var team = Team.Create("test", "Test Team");
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(team);
        _catalogGraph.CountServicesByTeamAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new GetTeamDetail.Handler(_teamRepository, _catalogGraph);
        var query = new GetTeamDetail.Query(team.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DeferredFields.Should().NotBeEmpty();
    }

    // ── UpdateTeam ──

    [Fact]
    public async Task UpdateTeam_ValidData_ShouldSucceed()
    {
        // Arrange
        var team = Team.Create("platform", "Platform", "Old description");
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns(team);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new UpdateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new UpdateTeam.Command(team.Id.Value.ToString(), "Platform Core Updated", "New description", "Tech Division");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _teamRepository.Received(1).UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTeam_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new UpdateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new UpdateTeam.Command("invalid-guid", "Name", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_TEAM_ID");
    }

    [Fact]
    public async Task UpdateTeam_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _teamRepository.GetByIdAsync(Arg.Any<TeamId>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        var handler = new UpdateTeam.Handler(_teamRepository, _unitOfWork);
        var command = new UpdateTeam.Command(Guid.NewGuid().ToString(), "Name", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TEAM_NOT_FOUND");
    }
}
