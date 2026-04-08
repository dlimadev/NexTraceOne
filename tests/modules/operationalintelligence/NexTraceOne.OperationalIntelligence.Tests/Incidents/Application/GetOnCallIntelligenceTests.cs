using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetOnCallIntelligence;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para GetOnCallIntelligence.
/// </summary>
public sealed class GetOnCallIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly IIncidentStore _store = new InMemoryIncidentStore();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public GetOnCallIntelligenceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ValidTeamId_ReturnsIntelligenceReport()
    {
        var handler = new GetOnCallIntelligence.Handler(_store, _clock);
        var query = new GetOnCallIntelligence.Query("team-alpha", 30);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-alpha");
        result.Value.PeriodDays.Should().Be(30);
        result.Value.ComputedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ValidTeamId_ReturnsFatigueLevel()
    {
        var handler = new GetOnCallIntelligence.Handler(_store, _clock);
        var query = new GetOnCallIntelligence.Query("team-beta", 30);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var validLevels = new[] { "Low", "Medium", "High", "Critical" };
        validLevels.Should().Contain(result.Value.FatigueLevel);
    }

    [Fact]
    public async Task Handle_ValidTeamId_ReturnsRecommendations()
    {
        var handler = new GetOnCallIntelligence.Handler(_store, _clock);
        var query = new GetOnCallIntelligence.Query("team-gamma", 30);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidTeamId_ReturnsFatigueIndicators()
    {
        var handler = new GetOnCallIntelligence.Handler(_store, _clock);
        var query = new GetOnCallIntelligence.Query("team-delta", 30);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FatigueIndicators.NightCallsPercent.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.FatigueIndicators.WeekendCallsPercent.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.FatigueIndicators.AvgResponseMinutes.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.FatigueIndicators.ConsecutiveIncidentDays.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_AnyTeam_PeakHourWithinValidRange()
    {
        var handler = new GetOnCallIntelligence.Handler(_store, _clock);
        var query = new GetOnCallIntelligence.Query("team-epsilon", 30);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeakHour.Should().BeInRange(0, 23);
    }

    [Fact]
    public void Validator_EmptyTeamId_ShouldFail()
    {
        var validator = new GetOnCallIntelligence.Validator();
        var result = validator.Validate(new GetOnCallIntelligence.Query("", 30));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_PeriodDaysOutOfRange_ShouldFail()
    {
        var validator = new GetOnCallIntelligence.Validator();
        var result = validator.Validate(new GetOnCallIntelligence.Query("team-x", 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidQuery_ShouldPass()
    {
        var validator = new GetOnCallIntelligence.Validator();
        var result = validator.Validate(new GetOnCallIntelligence.Query("team-x", 30));
        result.IsValid.Should().BeTrue();
    }
}
