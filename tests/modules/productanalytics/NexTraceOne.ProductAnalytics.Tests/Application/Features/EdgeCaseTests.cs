using FluentAssertions;
using NSubstitute;
using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Application.Features.GetAnalyticsSummary;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;
using NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de casos extremos e regressões para o módulo ProductAnalytics.
/// </summary>
public sealed class EdgeCaseTests
{
    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public EdgeCaseTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 4, 19, 12, 0, 0, TimeSpan.Zero));
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    [Fact]
    public async Task GetAnalyticsSummary_WithNoEvents_ShouldReturnZeroMetrics()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _repo.CountActivePersonasAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetAnalyticsSummary.Query(null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(0);
        result.Value.UniqueUsers.Should().Be(0);
        result.Value.AdoptionScore.Should().Be(0m);
        result.Value.ValueScore.Should().Be(0m);
        result.Value.FrictionScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetAnalyticsSummary_Last1d_ShouldQueryCorrectTimeWindow()
    {
        var fixedNow = _clock.UtcNow;
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(10L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(2);
        _repo.CountActivePersonasAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(1);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetAnalyticsSummary.Query(null, null, null, null, "last_1d"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_1d");

        await _repo.Received().CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(d => d >= fixedNow.AddDays(-1).AddMinutes(-1) && d <= fixedNow.AddDays(-1).AddMinutes(1)),
            Arg.Is<DateTimeOffset>(d => d >= fixedNow.AddMinutes(-1) && d <= fixedNow.AddMinutes(1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetJourneys_WithNoSessions_ShouldReturnSkeletonJourneys()
    {
        _repo.GetSessionEventTypesAsync(Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetJourneys.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetJourneys.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().HaveCount(5, "deve retornar 5 skeleton journeys para novos tenants");
        result.Value.Journeys.Should().AllSatisfy(j =>
        {
            j.CompletionRate.Should().Be(0m);
            j.Steps.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task GetValueMilestones_ShouldUseAutomationWorkflowManaged_ForFirstAutomationCreated()
    {
        // Regressão BUG-02: AutomationWorkflowManaged deve ser o evento, não OnboardingStepCompleted
        var automationDef = AnalyticsConstants.MilestoneDefs
            .Single(d => d.Type == ValueMilestoneType.FirstAutomationCreated);

        automationDef.EventType.Should().Be(AnalyticsEventType.AutomationWorkflowManaged,
            "FirstAutomationCreated deve mapear para AutomationWorkflowManaged, não OnboardingStepCompleted");
    }

    [Fact]
    public async Task GetPersonaUsage_WithUnknownPersonaFilter_ShouldReturnEmpty()
    {
        _repo.GetPersonaBreakdownAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([new PersonaBreakdownRow("Engineer", 5, 50)]);

        var handler = new GetPersonaUsage.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetPersonaUsage.Query("NonExistentPersona999", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().BeEmpty("persona desconhecida não deve retornar perfis");
    }

    [Fact]
    public async Task GetFrictionIndicators_WithNoFrictionEvents_ShouldReturnZeroScore()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0L);

        var handler = new GetFrictionIndicators.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetFrictionIndicators.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallFrictionScore.Should().Be(0m);
        result.Value.Indicators.Should().BeEmpty();
    }
}
