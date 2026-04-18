using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para o handler GetFrictionIndicators.
/// </summary>
public sealed class GetFrictionIndicatorsTests
{
    private readonly IAnalyticsEventRepository _repository = Substitute.For<IAnalyticsEventRepository>();

    private GetFrictionIndicators.Handler CreateHandler() => new(_repository);

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
