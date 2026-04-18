using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para o handler GetPersonaUsage.
/// </summary>
public sealed class GetPersonaUsageTests
{
    private readonly IAnalyticsEventRepository _repository = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public GetPersonaUsageTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private GetPersonaUsage.Handler CreateHandler() => new(_repository, _clock);

    private void SetupEmptyRepository()
    {
        _repository.GetPersonaBreakdownAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PersonaBreakdownRow>());
    }

    private void SetupPersonaBreakdown(params PersonaBreakdownRow[] rows)
    {
        _repository.GetPersonaBreakdownAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<PersonaBreakdownRow>)rows);

        _repository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ModuleUsageRow>)Array.Empty<ModuleUsageRow>());

        _repository.GetTopEventTypesAsync(
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventTypeCountRow>)Array.Empty<EventTypeCountRow>());

        _repository.GetDistinctEventTypesAsync(
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AnalyticsEventType>)Array.Empty<AnalyticsEventType>());
    }

    [Fact]
    public async Task GetPersonaUsage_WithData_ShouldReturnPersonaBreakdown()
    {
        // Arrange
        SetupPersonaBreakdown(
            new PersonaBreakdownRow("Engineer", 120, 5),
            new PersonaBreakdownRow("TechLead", 80, 3));

        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().HaveCount(2);
        result.Value.TotalPersonas.Should().Be(2);
    }

    [Fact]
    public async Task GetPersonaUsage_WithNoData_ShouldReturnEmptyBreakdown()
    {
        // Arrange
        SetupEmptyRepository();
        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().BeEmpty();
        result.Value.TotalPersonas.Should().Be(0);
    }

    [Fact]
    public async Task GetPersonaUsage_WithFilter_ShouldApplyFilter()
    {
        // Arrange
        SetupPersonaBreakdown(
            new PersonaBreakdownRow("Engineer", 120, 5),
            new PersonaBreakdownRow("TechLead", 80, 3));

        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query("Engineer", null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().HaveCount(1);
        result.Value.Profiles[0].Persona.Should().Be("Engineer");
    }

    [Fact]
    public async Task GetPersonaUsage_WithRangeLast7d_ShouldSucceed()
    {
        // Arrange
        SetupPersonaBreakdown(new PersonaBreakdownRow("Architect", 50, 2));
        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query(null, null, "last_7d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_7d");
    }

    [Fact]
    public async Task GetPersonaUsage_WithMultiplePersonas_ShouldIdentifyMostActive()
    {
        // Arrange
        SetupPersonaBreakdown(
            new PersonaBreakdownRow("Engineer", 500, 20),
            new PersonaBreakdownRow("TechLead", 200, 8),
            new PersonaBreakdownRow("Executive", 50, 5));

        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MostActivePersona.Should().Be("Engineer");
    }

    [Fact]
    public async Task GetPersonaUsage_WithEmptyFilter_ShouldReturnAllPersonas()
    {
        // Arrange
        SetupPersonaBreakdown(
            new PersonaBreakdownRow("Engineer", 100, 5),
            new PersonaBreakdownRow("Auditor", 40, 2));

        var handler = CreateHandler();
        var query = new GetPersonaUsage.Query(string.Empty, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().HaveCount(2);
    }
}
