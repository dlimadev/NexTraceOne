using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes adicionais para GetJourneys: filtro por JourneyId, ranges de período,
/// journeys com sessões reais, período label e janela temporal derivada do clock.
/// </summary>
public sealed class GetJourneysRicherTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public GetJourneysRicherTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    private GetJourneys.Handler CreateHandler() => new(_repo, _clock, _configService);

    private void SetupNoSessionData()
    {
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
        _repo.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    // ── JourneyId filter ────────────────────────────────────────────────────

    [Fact]
    public async Task GetJourneys_WithJourneyIdFilter_ShouldReturnOnlyMatchingJourney()
    {
        // Arrange
        SetupNoSessionData();
        _repo.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(10);

        var handler = CreateHandler();
        var query = new GetJourneys.Query(JourneyId: "search_to_entity", Persona: null, Range: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().OnlyContain(j => j.JourneyId == "search_to_entity");
    }

    [Fact]
    public async Task GetJourneys_WithUnknownJourneyIdFilter_ShouldReturnEmptyJourneys()
    {
        // Arrange
        SetupNoSessionData();
        var handler = CreateHandler();
        var query = new GetJourneys.Query(JourneyId: "nonexistent_journey", Persona: null, Range: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().BeEmpty();
    }

    // ── Period label ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("last_7d", "last_7d")]
    [InlineData("last_90d", "last_90d")]
    [InlineData("last_1d", "last_1d")]
    [InlineData(null, "last_30d")]
    public async Task GetJourneys_ShouldReturnCorrectPeriodLabel(string? range, string expectedLabel)
    {
        // Arrange
        SetupNoSessionData();
        var handler = CreateHandler();
        var query = new GetJourneys.Query(null, null, range);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be(expectedLabel);
    }

    // ── Temporal window derived from clock ──────────────────────────────────

    [Fact]
    public async Task GetJourneys_ShouldQueryCorrectTimeWindow()
    {
        // Arrange — clock fixed, verify repository receives window [FixedNow-30d, FixedNow]
        var expectedFrom = FixedNow.AddDays(-30);
        SetupNoSessionData();

        var handler = CreateHandler();
        var query = new GetJourneys.Query(null, null, null);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert — GetSessionEventTypesAsync called with correct window
        await _repo.Received(1).GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(),
            Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(d => d == expectedFrom),
            Arg.Is<DateTimeOffset>(d => d == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetJourneys_WithLast7dRange_ShouldQueryCorrectTimeWindow()
    {
        // Arrange
        var expectedFrom = FixedNow.AddDays(-7);
        SetupNoSessionData();

        var handler = CreateHandler();
        var query = new GetJourneys.Query(null, null, "last_7d");

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        await _repo.Received(1).GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(),
            Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(d => d == expectedFrom),
            Arg.Is<DateTimeOffset>(d => d == FixedNow),
            Arg.Any<CancellationToken>());
    }

    // ── Sessions with full journey completion ────────────────────────────────

    [Fact]
    public async Task GetJourneys_WithSessionsCompletingAllSteps_ShouldHaveHighCompletionRate()
    {
        // Arrange — sessions that contain all events for the "ai_prompt_to_action" journey
        // (AssistantPromptSubmitted → AssistantResponseUsed)
        var sessionEvents = new List<SessionEventTypeRow>
        {
            new("session-1", AnalyticsEventType.AssistantPromptSubmitted, FixedNow.AddHours(-2)),
            new("session-1", AnalyticsEventType.AssistantResponseUsed, FixedNow.AddHours(-1)),
            new("session-2", AnalyticsEventType.AssistantPromptSubmitted, FixedNow.AddHours(-3)),
            new("session-2", AnalyticsEventType.AssistantResponseUsed, FixedNow.AddHours(-2)),
        };

        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sessionEvents);
        _repo.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var handler = CreateHandler();
        var query = new GetJourneys.Query(JourneyId: "ai_prompt_to_action", Persona: null, Range: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var journey = result.Value.Journeys.Should().ContainSingle(j => j.JourneyId == "ai_prompt_to_action").Subject;
        journey.CompletionRate.Should().Be(100m);
        journey.Steps.Should().NotBeEmpty();
    }

    // ── Response shape ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJourneys_WithNoData_ShouldHaveZeroAverageCompletionRate()
    {
        // Arrange
        SetupNoSessionData();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AverageCompletionRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetJourneys_WithPersonaFilter_ShouldPassPersonaToRepository()
    {
        // Arrange
        SetupNoSessionData();
        var handler = CreateHandler();
        var query = new GetJourneys.Query(null, Persona: "Engineer", Range: null);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert — persona passed through to repository
        await _repo.Received(1).GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(),
            Arg.Is<string?>(p => p == "Engineer"),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }
}
