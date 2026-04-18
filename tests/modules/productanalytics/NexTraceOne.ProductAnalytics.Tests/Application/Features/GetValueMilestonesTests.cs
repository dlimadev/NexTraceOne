using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para o handler GetValueMilestones.
/// </summary>
public sealed class GetValueMilestonesTests
{
    private readonly IAnalyticsEventRepository _repository = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public GetValueMilestonesTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private GetValueMilestones.Handler CreateHandler() => new(_repository, _clock);

    private void SetupZeroUsers()
    {
        _repository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(0);
    }

    private void SetupUsersWithEvents(int totalUsers)
    {
        _repository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(totalUsers);

        _repository.CountUsersByEventTypeAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventTypeUserCountRow>)Array.Empty<EventTypeUserCountRow>());

        _repository.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UserFirstEventRow>)Array.Empty<UserFirstEventRow>());
    }

    [Fact]
    public async Task GetValueMilestones_WithFirstValue_ShouldReturnMilestones()
    {
        // Arrange
        SetupUsersWithEvents(10);
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milestones.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetValueMilestones_WithNoEvents_ShouldReturnEmpty()
    {
        // Arrange
        SetupZeroUsers();
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvgTimeToFirstValueMinutes.Should().Be(0m);
        result.Value.AvgTimeToCoreValueMinutes.Should().Be(0m);
        result.Value.OverallCompletionRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetValueMilestones_WithNoUsers_ShouldReturnZeroCompletionRate()
    {
        // Arrange
        SetupZeroUsers();
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.OverallCompletionRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetValueMilestones_WithRangeLast7d_ShouldSucceedWithCorrectLabel()
    {
        // Arrange
        SetupZeroUsers();
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, "last_7d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_7d");
    }

    [Fact]
    public async Task GetValueMilestones_WithPersonaFilter_ShouldPassPersonaToRepository()
    {
        // Arrange
        SetupUsersWithEvents(5);
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query("Engineer", null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received().CountUniqueUsersAsync(
            "Engineer", Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetValueMilestones_WithUsersButNoMilestoneEvents_ShouldReturnAllZeroCompletionRates()
    {
        // Arrange
        SetupUsersWithEvents(10);
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milestones.Should().AllSatisfy(m => m.CompletionRate.Should().Be(0m));
    }

    [Fact]
    public async Task GetValueMilestones_WithDefaultRange_ShouldUseLast30dLabel()
    {
        // Arrange
        SetupZeroUsers();
        var handler = CreateHandler();
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.PeriodLabel.Should().Be("last_30d");
    }
}
