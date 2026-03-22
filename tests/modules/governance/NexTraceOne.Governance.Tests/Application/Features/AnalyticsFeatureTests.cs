using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetAnalyticsSummary;
using NexTraceOne.Governance.Application.Features.GetModuleAdoption;
using NexTraceOne.Governance.Application.Features.GetPersonaUsage;
using NexTraceOne.Governance.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

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

    public AnalyticsFeatureTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentTenant.Id.Returns(Guid.NewGuid());
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-123");
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

        var handler = new GetAnalyticsSummary.Handler(_analyticsRepository, _clock);
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

        var handler = new GetAnalyticsSummary.Handler(_analyticsRepository, _clock);
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
        var handler = new GetPersonaUsage.Handler();
        var query = new GetPersonaUsage.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().NotBeEmpty();
        result.Value.TotalPersonas.Should().BeGreaterThan(0);
        result.Value.MostActivePersona.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPersonaUsage_WithFilter_ShouldReturnFilteredProfiles()
    {
        // Arrange
        var handler = new GetPersonaUsage.Handler();

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

        var handler = new GetModuleAdoption.Handler(_analyticsRepository, _clock);
        var query = new GetModuleAdoption.Query(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().HaveCount(2);
        result.Value.OverallAdoptionScore.Should().BeGreaterThan(0);
    }
}
