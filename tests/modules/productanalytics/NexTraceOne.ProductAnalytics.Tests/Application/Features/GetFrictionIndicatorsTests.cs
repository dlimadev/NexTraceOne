using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para o handler GetFrictionIndicators.
/// </summary>
public sealed class GetFrictionIndicatorsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly IAnalyticsEventRepository _repository = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public GetFrictionIndicatorsTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private GetFrictionIndicators.Handler CreateHandler() => new(_repository, _clock);

    private void SetupZeroTotalEvents()
    {
        _repository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(0L);
    }

    private void SetupTotalEvents(long totalEvents)
    {
        _repository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(totalEvents);
    }

    private void SetupFrictionEventCount(AnalyticsEventType eventType, long count)
    {
        _repository.CountByEventTypeAsync(
            eventType, Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(count);
    }

    private void SetupTopModules(params ModuleUsageRow[] modules)
    {
        _repository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ModuleUsageRow>)modules);
    }

    [Fact]
    public async Task GetFrictionIndicators_WithFrictionEvents_ShouldReturnIndicators()
    {
        // Arrange
        SetupTotalEvents(500);
        SetupFrictionEventCount(AnalyticsEventType.ZeroResultSearch, 30);
        SetupFrictionEventCount(AnalyticsEventType.EmptyStateEncountered, 0);
        SetupFrictionEventCount(AnalyticsEventType.JourneyAbandoned, 0);
        SetupTopModules(new ModuleUsageRow(ProductModule.Search, 50, 10));

        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.Indicators.Should().ContainSingle(i => i.SignalType == FrictionSignalType.ZeroResultSearch);
    }

    [Fact]
    public async Task GetFrictionIndicators_WithNoFriction_ShouldReturnEmptyIndicators()
    {
        // Arrange
        SetupZeroTotalEvents();
        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().BeEmpty();
        result.Value.OverallFrictionScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetFrictionIndicators_WithAllFrictionTypes_ShouldReturnAllSignals()
    {
        // Arrange
        SetupTotalEvents(1000);
        SetupFrictionEventCount(AnalyticsEventType.ZeroResultSearch, 50);
        SetupFrictionEventCount(AnalyticsEventType.EmptyStateEncountered, 30);
        SetupFrictionEventCount(AnalyticsEventType.JourneyAbandoned, 20);
        SetupTopModules(new ModuleUsageRow(ProductModule.ServiceCatalog, 200, 40));

        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFrictionIndicators_WithRange_ShouldSucceed()
    {
        // Arrange
        SetupZeroTotalEvents();
        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, "last_7d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_7d");
    }

    [Fact]
    public async Task GetFrictionIndicators_ParseRange_UsesClockUtcNow()
    {
        // Arrange — clock fixed at 2026-04-19. The handler should query from FixedNow-30d to FixedNow.
        var expectedFrom = FixedNow.AddDays(-30);
        SetupZeroTotalEvents();

        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null); // default last_30d

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert — CountAsync must have been called with the window derived from clock.UtcNow
        await _repository.Received(1).CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(d => d == expectedFrom),
            Arg.Is<DateTimeOffset>(d => d == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("last_1d", 1)]
    [InlineData("last_7d", 7)]
    [InlineData("last_30d", 30)]
    [InlineData("last_90d", 90)]
    public async Task GetFrictionIndicators_ParseRange_CorrectWindowForEachRange(string range, int days)
    {
        // Arrange
        var expectedFrom = FixedNow.AddDays(-days);
        SetupZeroTotalEvents();

        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, range);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert — CountAsync is called with the correct window
        await _repository.Received(1).CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(d => d == expectedFrom),
            Arg.Is<DateTimeOffset>(d => d == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFrictionIndicators_WithNoFrictionEvents_ShouldReturnZeroImprovingAndDeclining()
    {
        // Arrange
        SetupZeroTotalEvents();
        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.ImprovingSignals.Should().Be(0);
        result.Value.DecliningSignals.Should().Be(0);
        result.Value.StableSignals.Should().Be(0);
    }

    [Fact]
    public async Task GetFrictionIndicators_DataSourceShouldBeAnalytics()
    {
        // Arrange
        SetupZeroTotalEvents();
        var handler = CreateHandler();
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.DataSource.Should().Be("analytics");
        result.Value.IsSimulated.Should().BeFalse();
    }
}
