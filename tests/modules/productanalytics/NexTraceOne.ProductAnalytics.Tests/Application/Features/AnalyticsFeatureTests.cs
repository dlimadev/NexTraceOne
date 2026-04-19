using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Features.GetAnalyticsSummary;
using NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;
using NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones;
using NexTraceOne.ProductAnalytics.Application.Features.GetAdoptionFunnel;
using NexTraceOne.ProductAnalytics.Application.Features.GetFeatureHeatmap;
using NexTraceOne.ProductAnalytics.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de product analytics.
/// </summary>
public sealed class AnalyticsFeatureTests
{
    private readonly IAnalyticsEventRepository _analyticsRepository = Substitute.For<IAnalyticsEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public AnalyticsFeatureTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentTenant.Id.Returns(Guid.NewGuid());
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-123");
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    // ── RecordAnalyticsEvent ──

    [Fact]
    public async Task RecordEvent_ValidData_ShouldReturnEventId()
    {
        // Arrange
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new RecordAnalyticsEvent.Handler(_analyticsRepository, _unitOfWork, _currentTenant, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            EventType: AnalyticsEventType.ModuleViewed,
            Module: ProductModule.ServiceCatalog,
            Route: "/services",
            Feature: "list",
            EntityType: null,
            Outcome: null,
            PersonaHint: "Engineer",
            TeamId: null,
            DomainId: null,
            SessionCorrelationId: "session-abc",
            ClientType: "web",
            MetadataJson: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().NotBeNullOrWhiteSpace();
        result.Value.EventType.Should().Be(AnalyticsEventType.ModuleViewed);
        result.Value.Module.Should().Be(ProductModule.ServiceCatalog);
        await _analyticsRepository.Received(1).AddAsync(Arg.Any<AnalyticsEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordEvent_UnauthenticatedUser_ShouldStillSucceed()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new RecordAnalyticsEvent.Handler(_analyticsRepository, _unitOfWork, _currentTenant, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.SearchExecuted, ProductModule.Search, "/search",
            null, null, null, null, null, null, null, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── GetAnalyticsSummary ──

    [Fact]
    public async Task GetAnalyticsSummary_ShouldReturnSummary()
    {
        // Arrange
        _analyticsRepository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(100L);
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(25);
        _analyticsRepository.CountActivePersonasAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(5);
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _analyticsRepository.ListSessionEventsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var handler = new GetAnalyticsSummary.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetAnalyticsSummary.Query(null, null, null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(100);
        result.Value.UniqueUsers.Should().Be(25);
        result.Value.ActivePersonas.Should().Be(5);
    }

    [Fact]
    public async Task GetAnalyticsSummary_WithZeroEvents_ShouldReturnZeroScores()
    {
        // Arrange
        _analyticsRepository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _analyticsRepository.CountActivePersonasAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _analyticsRepository.ListSessionEventsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var handler = new GetAnalyticsSummary.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetAnalyticsSummary.Query(null, null, null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdoptionScore.Should().Be(0m);
        result.Value.ValueScore.Should().Be(0m);
        result.Value.FrictionScore.Should().Be(0m);
    }

    // ── GetPersonaUsage ──

    [Fact]
    public async Task GetPersonaUsage_ShouldReturnProfiles()
    {
        // Arrange
        _analyticsRepository.GetPersonaBreakdownAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonaBreakdownRow>
            {
                new("Engineer", 50, 10),
                new("Architect", 20, 5)
            });
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _analyticsRepository.GetTopEventTypesAsync(
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventTypeCountRow>());
        _analyticsRepository.GetDistinctEventTypesAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<AnalyticsEventType>());

        var handler = new GetPersonaUsage.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetPersonaUsage.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().NotBeEmpty();
        result.Value.TotalPersonas.Should().Be(2);
        result.Value.MostActivePersona.Should().Be("Engineer");
    }

    [Fact]
    public async Task GetPersonaUsage_WithFilter_ShouldReturnFilteredProfiles()
    {
        // Arrange
        _analyticsRepository.GetPersonaBreakdownAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonaBreakdownRow>
            {
                new("Engineer", 50, 10)
            });
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _analyticsRepository.GetTopEventTypesAsync(
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventTypeCountRow>());
        _analyticsRepository.GetDistinctEventTypesAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<AnalyticsEventType>());

        var handler = new GetPersonaUsage.Handler(_analyticsRepository, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetPersonaUsage.Query("Engineer", null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().HaveCount(1);
        result.Value.Profiles[0].Persona.Should().Be("Engineer");
    }

    // ── GetModuleAdoption ──

    [Fact]
    public async Task GetModuleAdoption_ShouldReturnModules()
    {
        // Arrange
        _analyticsRepository.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.ServiceCatalog, 150, 20),
                new(ProductModule.ContractStudio, 80, 12)
            });
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(30);
        _analyticsRepository.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetModuleAdoption.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().HaveCount(2);
        result.Value.OverallAdoptionScore.Should().BeGreaterThan(0);
    }

    // ── GetJourneys ──

    [Fact]
    public async Task GetJourneys_ShouldReturnJourneys()
    {
        // Arrange
        _analyticsRepository.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
        _analyticsRepository.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(50);

        var handler = new GetJourneys.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetJourneys.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // With no session data, handler returns skeleton journeys (BUG-04 fix)
        result.Value.AverageCompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetJourneys_WithNoSessions_ShouldReturnZeroCompletion()
    {
        // Arrange
        _analyticsRepository.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
        _analyticsRepository.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new GetJourneys.Handler(_analyticsRepository, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var j in result.Value.Journeys)
        {
            j.CompletionRate.Should().Be(0);
        }
    }

    // ── GetValueMilestones ──

    [Fact]
    public async Task GetValueMilestones_ShouldReturnMilestones()
    {
        // Arrange
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(100);
        _analyticsRepository.CountUsersByEventTypeAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventTypeUserCountRow>());
        _analyticsRepository.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserFirstEventRow>());

        var handler = new GetValueMilestones.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetValueMilestones.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milestones.Should().HaveCount(15);
    }

    [Fact]
    public async Task GetValueMilestones_WithZeroUsers_ShouldReturnZeroCompletion()
    {
        // Arrange
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _analyticsRepository.CountUsersByEventTypeAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventTypeUserCountRow>());
        _analyticsRepository.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserFirstEventRow>());

        var handler = new GetValueMilestones.Handler(_analyticsRepository, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetValueMilestones.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var m in result.Value.Milestones)
        {
            m.CompletionRate.Should().Be(0);
        }
    }

    // ── GetAdoptionFunnel ──

    [Fact]
    public async Task GetAdoptionFunnel_ShouldReturnModuleFunnels()
    {
        // Arrange
        _analyticsRepository.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
        _analyticsRepository.CountDistinctSessionsAsync(
            Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(20);

        var handler = new GetAdoptionFunnel.Handler(_analyticsRepository, _clock);
        var query = new GetAdoptionFunnel.Query(null, null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Funnels.Should().NotBeEmpty();
        result.Value.Funnels.Should().HaveCountGreaterThanOrEqualTo(6);
    }

    // ── GetFeatureHeatmap ──

    [Fact]
    public async Task GetFeatureHeatmap_ShouldReturnCells()
    {
        // Arrange
        _analyticsRepository.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.ServiceCatalog, 200, 30),
                new(ProductModule.ContractStudio, 100, 15)
            });
        _analyticsRepository.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());
        _analyticsRepository.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(40);

        var handler = new GetFeatureHeatmap.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetFeatureHeatmap.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().HaveCount(2);
        result.Value.TotalUniqueUsers.Should().Be(40);
    }

    // ── GetFrictionIndicators ──

    [Fact]
    public async Task GetFrictionIndicators_DefaultQuery_ShouldReturnIndicators()
    {
        // Arrange
        _analyticsRepository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(200L);
        _analyticsRepository.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(30L);
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.ServiceCatalog, 50, 10) });

        var handler = new GetFrictionIndicators.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetFrictionIndicators.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.OverallFrictionScore.Should().BeGreaterThan(0);
        result.Value.PeriodLabel.Should().NotBeNullOrWhiteSpace();
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("analytics");
    }

    [Fact]
    public async Task GetFrictionIndicators_WithPersonaFilter_ShouldSucceed()
    {
        // Arrange
        _analyticsRepository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(150L);
        _analyticsRepository.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(20L);
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.ContractStudio, 40, 8) });

        var handler = new GetFrictionIndicators.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetFrictionIndicators.Query(Persona: "Engineer", Module: null, Range: "last_7d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.OverallFrictionScore.Should().BeGreaterThan(0);
        result.Value.PeriodLabel.Should().Be("last_7d");
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetFrictionIndicators_WithModuleFilter_ShouldSucceed()
    {
        // Arrange — all indicators point to ServiceCatalog; filtering by ServiceCatalog keeps them
        _analyticsRepository.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(300L);
        _analyticsRepository.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(40L);
        _analyticsRepository.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.ServiceCatalog, 80, 15) });

        var handler = new GetFrictionIndicators.Handler(_analyticsRepository, _clock, _configService);
        var query = new GetFrictionIndicators.Query(Persona: null, Module: "ServiceCatalog", Range: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.Indicators.Should().OnlyContain(i => i.Module == ProductModule.ServiceCatalog);
        result.Value.PeriodLabel.Should().NotBeNullOrWhiteSpace();
        result.Value.IsSimulated.Should().BeFalse();
    }
}
