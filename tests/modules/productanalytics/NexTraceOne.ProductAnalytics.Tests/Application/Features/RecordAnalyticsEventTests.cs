using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de unidade para o handler RecordAnalyticsEvent.
/// </summary>
public sealed class RecordAnalyticsEventTests
{
    private readonly IAnalyticsEventRepository _repository = Substitute.For<IAnalyticsEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IAnalyticsEventForwarder _forwarder = Substitute.For<IAnalyticsEventForwarder>();

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public RecordAnalyticsEventTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.Id.Returns(Guid.NewGuid());
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-001");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    private RecordAnalyticsEvent.Handler CreateHandler() =>
        new(_repository, _unitOfWork, _currentTenant, _currentUser, _clock, _forwarder);

    private static RecordAnalyticsEvent.Command BuildCommand(
        AnalyticsEventType eventType = AnalyticsEventType.ModuleViewed,
        ProductModule module = ProductModule.ServiceCatalog,
        string route = "/services",
        string? feature = "list",
        string? personaHint = "Engineer",
        string? sessionId = "session-xyz",
        string? clientType = "web") =>
        new(eventType, module, route, feature, null, null, personaHint, null, null, sessionId, clientType, null);

    [Fact]
    public async Task RecordEvent_WithValidInput_ShouldSucceed()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordEvent_WithAllFields_ShouldCallRepositoryOnce()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand(
            AnalyticsEventType.ContractPublished,
            ProductModule.ContractStudio,
            "/contracts/new",
            "publish",
            "TechLead",
            "session-abc",
            "web");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(Arg.Any<AnalyticsEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordEvent_ShouldReturnSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand(AnalyticsEventType.SearchExecuted, ProductModule.Search, "/search");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordEvent_ShouldReturnResponseWithCorrectEventTypeAndModule()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand(AnalyticsEventType.IncidentInvestigated, ProductModule.Incidents);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.EventType.Should().Be(AnalyticsEventType.IncidentInvestigated);
        result.Value.Module.Should().Be(ProductModule.Incidents);
    }

    [Fact]
    public async Task RecordEvent_ShouldReturnNonEmptyEventId()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.EventId.Should().NotBeNullOrWhiteSpace();
        result.Value.EventId.Length.Should().Be(12);
    }

    [Fact]
    public async Task RecordEvent_ShouldReturnRecordedAtFromClock()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.RecordedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task RecordEvent_ShouldCommitUnitOfWork()
    {
        // Arrange
        var handler = CreateHandler();
        var command = BuildCommand();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordEvent_WithUnauthenticatedUser_ShouldStillSucceed()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();
        var command = BuildCommand(personaHint: null, sessionId: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<AnalyticsEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordEvent_WithDifferentModules_ShouldSucceedForEach()
    {
        // Arrange & Act & Assert
        foreach (var module in new[] { ProductModule.ServiceCatalog, ProductModule.ChangeIntelligence, ProductModule.Incidents, ProductModule.Runbooks })
        {
            var handler = CreateHandler();
            var command = BuildCommand(module: module, route: $"/{module.ToString().ToLowerInvariant()}");
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Module.Should().Be(module);
        }
    }
}
