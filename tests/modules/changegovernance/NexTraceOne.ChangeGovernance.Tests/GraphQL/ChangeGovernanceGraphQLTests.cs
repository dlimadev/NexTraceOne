using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.API.GraphQL;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary;

namespace NexTraceOne.ChangeGovernance.Tests.GraphQL;

/// <summary>
/// Testes unitários para ChangeGovernanceQuery — Phase 5.3 Schema Stitching.
/// Cobrem: resolver GetChangesSummaryAsync e mapeamento de resposta.
/// </summary>
public sealed class ChangeGovernanceGraphQLTests
{
    // ─────────────────────────────────────────────────────────────────────
    // ChangesSummaryGraphType
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ChangesSummaryGraphType_ShouldHoldAllMetrics()
    {
        var type = new ChangesSummaryGraphType
        {
            TotalChanges = 120,
            ValidatedChanges = 95,
            ChangesNeedingAttention = 15,
            SuspectedRegressions = 3,
            ChangesCorrelatedWithIncidents = 8
        };

        type.TotalChanges.Should().Be(120);
        type.ValidatedChanges.Should().Be(95);
        type.ChangesNeedingAttention.Should().Be(15);
        type.SuspectedRegressions.Should().Be(3);
        type.ChangesCorrelatedWithIncidents.Should().Be(8);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ChangeGovernanceQuery — GetChangesSummaryAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChangesSummary_ReturnsNull_WhenMediatorReturnsError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("NOT_FOUND", "No changes found"));

        var query = new ChangeGovernanceQuery();
        var result = await query.GetChangesSummaryAsync(mediator, cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChangesSummary_MapsAllFields_WhenMediatorSucceeds()
    {
        var mediator = Substitute.For<IMediator>();
        var response = new GetChangesSummary.Response(
            TotalChanges: 50,
            ValidatedChanges: 40,
            ChangesNeedingAttention: 7,
            SuspectedRegressions: 2,
            ChangesCorrelatedWithIncidents: 4);

        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new ChangeGovernanceQuery();
        var result = await query.GetChangesSummaryAsync(mediator, teamName: "team-alpha", cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalChanges.Should().Be(50);
        result.ValidatedChanges.Should().Be(40);
        result.ChangesNeedingAttention.Should().Be(7);
        result.SuspectedRegressions.Should().Be(2);
        result.ChangesCorrelatedWithIncidents.Should().Be(4);
    }

    [Fact]
    public async Task GetChangesSummary_PassesTeamNameFilter_ToMediator()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(new GetChangesSummary.Response(10, 8, 2, 0, 1));

        var query = new ChangeGovernanceQuery();
        await query.GetChangesSummaryAsync(mediator, teamName: "backend-team", cancellationToken: CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<GetChangesSummary.Query>(q => q.TeamName == "backend-team"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetChangesSummary_PassesEnvironmentFilter_ToMediator()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(new GetChangesSummary.Response(5, 5, 0, 0, 0));

        var query = new ChangeGovernanceQuery();
        await query.GetChangesSummaryAsync(mediator, environment: "Production", cancellationToken: CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<GetChangesSummary.Query>(q => q.Environment == "Production"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetChangesSummary_UsesDefaultDaysBack30_WhenNotSpecified()
    {
        const double ExpectedMinDaysRange = 29.9; // tolerância de 0.1s para execução do teste

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(new GetChangesSummary.Response(0, 0, 0, 0, 0));

        var query = new ChangeGovernanceQuery();
        await query.GetChangesSummaryAsync(mediator, cancellationToken: CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<GetChangesSummary.Query>(q =>
                q.From.HasValue &&
                q.To.HasValue &&
                (q.To.Value - q.From.Value).TotalDays >= ExpectedMinDaysRange),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetChangesSummary_WithZeroChanges_ReturnsZeroSummary()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetChangesSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(new GetChangesSummary.Response(
                TotalChanges: 0,
                ValidatedChanges: 0,
                ChangesNeedingAttention: 0,
                SuspectedRegressions: 0,
                ChangesCorrelatedWithIncidents: 0));

        var query = new ChangeGovernanceQuery();
        var result = await query.GetChangesSummaryAsync(mediator, cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalChanges.Should().Be(0);
        result.SuspectedRegressions.Should().Be(0);
    }
}
