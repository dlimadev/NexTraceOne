using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ComputeTeamHealth;
using NexTraceOne.Governance.Application.Features.GetTeamHealthSnapshot;
using NexTraceOne.Governance.Application.Features.ListTeamHealthSnapshots;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Tests.Application;

/// <summary>
/// Testes dos handlers de saúde de equipas (Team Health Dashboard).
/// Cobre ComputeTeamHealth, GetTeamHealthSnapshot e ListTeamHealthSnapshots.
/// </summary>
public sealed class TeamHealthHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly ITeamHealthSnapshotRepository _repository =
        Substitute.For<ITeamHealthSnapshotRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public TeamHealthHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    // ── ComputeTeamHealth ──

    [Fact]
    public async Task Compute_NewTeam_ShouldCreateSnapshot()
    {
        _repository.GetByTeamIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(null));

        var handler = new ComputeTeamHealth.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeTeamHealth.Command(
            TeamId: Guid.NewGuid(),
            TeamName: "platform-team",
            ServiceCountScore: 80,
            ContractHealthScore: 70,
            IncidentFrequencyScore: 60,
            MttrScore: 55,
            TechDebtScore: 40,
            DocCoverageScore: 90,
            PolicyComplianceScore: 75,
            TenantId: "tenant1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRecomputation.Should().BeFalse();
        result.Value.TeamName.Should().Be("platform-team");
        // (80 + 70 + 60 + 55 + 40 + 90 + 75) = 470 / 7 = 67.14 → 67
        result.Value.OverallScore.Should().Be(67);

        await _repository.Received(1).AddAsync(Arg.Any<TeamHealthSnapshot>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Compute_ExistingTeam_ShouldRecompute()
    {
        var teamId = Guid.NewGuid();
        var existing = TeamHealthSnapshot.Compute(
            teamId: teamId,
            teamName: "platform-team",
            serviceCountScore: 50,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: "tenant1",
            now: FixedNow.AddDays(-30));

        _repository.GetByTeamIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(existing));

        var handler = new ComputeTeamHealth.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeTeamHealth.Command(
            TeamId: teamId,
            TeamName: "platform-team",
            ServiceCountScore: 90,
            ContractHealthScore: 85,
            IncidentFrequencyScore: 80,
            MttrScore: 75,
            TechDebtScore: 70,
            DocCoverageScore: 95,
            PolicyComplianceScore: 88);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRecomputation.Should().BeTrue();
        // (90 + 85 + 80 + 75 + 70 + 95 + 88) = 583 / 7 = 83.2857 → 83
        result.Value.OverallScore.Should().Be(83);

        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Compute_AllScoresMax_ShouldReturn100()
    {
        _repository.GetByTeamIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(null));

        var handler = new ComputeTeamHealth.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeTeamHealth.Command(
            TeamId: Guid.NewGuid(),
            TeamName: "perfect-team",
            ServiceCountScore: 100,
            ContractHealthScore: 100,
            IncidentFrequencyScore: 100,
            MttrScore: 100,
            TechDebtScore: 100,
            DocCoverageScore: 100,
            PolicyComplianceScore: 100);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallScore.Should().Be(100);
    }

    [Fact]
    public async Task Compute_AllScoresZero_ShouldReturn0()
    {
        _repository.GetByTeamIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(null));

        var handler = new ComputeTeamHealth.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeTeamHealth.Command(
            TeamId: Guid.NewGuid(),
            TeamName: "empty-team",
            ServiceCountScore: 0,
            ContractHealthScore: 0,
            IncidentFrequencyScore: 0,
            MttrScore: 0,
            TechDebtScore: 0,
            DocCoverageScore: 0,
            PolicyComplianceScore: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallScore.Should().Be(0);
    }

    // ── GetTeamHealthSnapshot ──

    [Fact]
    public async Task Get_ExistingTeam_ShouldReturnSnapshot()
    {
        var teamId = Guid.NewGuid();
        var snapshot = TeamHealthSnapshot.Compute(
            teamId: teamId,
            teamName: "api-team",
            serviceCountScore: 80,
            contractHealthScore: 70,
            incidentFrequencyScore: 60,
            mttrScore: 55,
            techDebtScore: 40,
            docCoverageScore: 90,
            policyComplianceScore: 75,
            dimensionDetails: """{"test":true}""",
            tenantId: "t1",
            now: FixedNow);

        _repository.GetByTeamIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(snapshot));

        var handler = new GetTeamHealthSnapshot.Handler(_repository);
        var query = new GetTeamHealthSnapshot.Query(teamId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be(teamId);
        result.Value.TeamName.Should().Be("api-team");
        result.Value.ServiceCountScore.Should().Be(80);
        result.Value.ContractHealthScore.Should().Be(70);
        result.Value.IncidentFrequencyScore.Should().Be(60);
        result.Value.MttrScore.Should().Be(55);
        result.Value.TechDebtScore.Should().Be(40);
        result.Value.DocCoverageScore.Should().Be(90);
        result.Value.PolicyComplianceScore.Should().Be(75);
        result.Value.DimensionDetails.Should().Be("""{"test":true}""");
    }

    [Fact]
    public async Task Get_NonExistentTeam_ShouldReturnNotFoundError()
    {
        var teamId = Guid.NewGuid();
        _repository.GetByTeamIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamHealthSnapshot?>(null));

        var handler = new GetTeamHealthSnapshot.Handler(_repository);
        var query = new GetTeamHealthSnapshot.Query(teamId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Governance.TeamHealthSnapshot.TeamNotFound");
    }

    // ── ListTeamHealthSnapshots ──

    [Fact]
    public async Task List_NoFilter_ShouldReturnAll()
    {
        var snapshots = new List<TeamHealthSnapshot>
        {
            TeamHealthSnapshot.Compute(
                Guid.NewGuid(), "team-a", 80, 70, 60, 55, 40, 90, 75, null, null, FixedNow),
            TeamHealthSnapshot.Compute(
                Guid.NewGuid(), "team-b", 100, 100, 100, 100, 100, 100, 100, null, null, FixedNow)
        };

        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamHealthSnapshot>>(snapshots));

        var handler = new ListTeamHealthSnapshots.Handler(_repository);
        var result = await handler.Handle(new ListTeamHealthSnapshots.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.MinOverallScore.Should().BeNull();
    }

    [Fact]
    public async Task List_WithMinScore_ShouldPassFilterToRepository()
    {
        _repository.ListAsync(70, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamHealthSnapshot>>([]));

        var handler = new ListTeamHealthSnapshots.Handler(_repository);
        var result = await handler.Handle(
            new ListTeamHealthSnapshots.Query(MinOverallScore: 70), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.MinOverallScore.Should().Be(70);

        await _repository.Received(1).ListAsync(70, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_EmptyResult_ShouldReturnEmptyList()
    {
        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamHealthSnapshot>>([]));

        var handler = new ListTeamHealthSnapshots.Handler(_repository);
        var result = await handler.Handle(new ListTeamHealthSnapshots.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ShouldMapDtoFieldsCorrectly()
    {
        var teamId = Guid.NewGuid();
        var snapshot = TeamHealthSnapshot.Compute(
            teamId, "mapped-team", 85, 70, 60, 55, 40, 90, 75, null, "t1", FixedNow);

        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamHealthSnapshot>>(new[] { snapshot }));

        var handler = new ListTeamHealthSnapshots.Handler(_repository);
        var result = await handler.Handle(new ListTeamHealthSnapshots.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.TeamId.Should().Be(teamId);
        item.TeamName.Should().Be("mapped-team");
        item.ServiceCountScore.Should().Be(85);
        item.ContractHealthScore.Should().Be(70);
        item.IncidentFrequencyScore.Should().Be(60);
        item.MttrScore.Should().Be(55);
        item.TechDebtScore.Should().Be(40);
        item.DocCoverageScore.Should().Be(90);
        item.PolicyComplianceScore.Should().Be(75);
    }
}
