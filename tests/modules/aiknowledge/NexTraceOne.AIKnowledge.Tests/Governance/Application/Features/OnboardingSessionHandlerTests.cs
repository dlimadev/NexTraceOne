using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.StartOnboardingSession;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetOnboardingSession;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListOnboardingSessions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class OnboardingSessionHandlerTests
{
    private readonly IOnboardingSessionRepository _repository = Substitute.For<IOnboardingSessionRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public OnboardingSessionHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.Id.Returns("user-42");
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    // ── StartOnboardingSession ──────────────────────────────────────────

    [Fact]
    public async Task Start_Valid_Command_Succeeds()
    {
        var handler = new StartOnboardingSession.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new StartOnboardingSession.Command(
            UserDisplayName: "Alice Smith",
            TeamId: Guid.NewGuid(),
            TeamName: "Platform Team",
            ExperienceLevelValue: "Mid",
            ChecklistItems: "[\"explore-catalog\",\"review-contracts\"]",
            TotalItems: 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<OnboardingSession>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Junior")]
    [InlineData("Mid")]
    [InlineData("Senior")]
    [InlineData("Expert")]
    public async Task Start_All_Experience_Levels_Succeed(string level)
    {
        var handler = new StartOnboardingSession.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new StartOnboardingSession.Command(
            "Name", Guid.NewGuid(), "Team", level, "[]", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── StartOnboardingSession Validator ─────────────────────────────────

    [Fact]
    public async Task Start_Validator_Rejects_Empty_DisplayName()
    {
        var validator = new StartOnboardingSession.Validator();
        var command = new StartOnboardingSession.Command(
            "", Guid.NewGuid(), "Team", "Mid", "[]", 1);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Start_Validator_Rejects_Empty_TeamId()
    {
        var validator = new StartOnboardingSession.Validator();
        var command = new StartOnboardingSession.Command(
            "Name", Guid.Empty, "Team", "Mid", "[]", 1);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Start_Validator_Rejects_Invalid_ExperienceLevel()
    {
        var validator = new StartOnboardingSession.Validator();
        var command = new StartOnboardingSession.Command(
            "Name", Guid.NewGuid(), "Team", "Invalid", "[]", 1);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Start_Validator_Rejects_Zero_TotalItems()
    {
        var validator = new StartOnboardingSession.Validator();
        var command = new StartOnboardingSession.Command(
            "Name", Guid.NewGuid(), "Team", "Mid", "[]", 0);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Start_Validator_Accepts_Valid_Command()
    {
        var validator = new StartOnboardingSession.Validator();
        var command = new StartOnboardingSession.Command(
            "Alice Smith", Guid.NewGuid(), "Platform Team", "Senior",
            "[\"item1\"]", 3);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── GetOnboardingSession ────────────────────────────────────────────

    [Fact]
    public async Task Get_Returns_Session_When_Found()
    {
        var session = CreateValidSession();
        _repository.GetByIdAsync(Arg.Any<OnboardingSessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetOnboardingSession.Handler(_repository);
        var result = await handler.Handle(
            new GetOnboardingSession.Query(session.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(session.Id.Value);
        result.Value.UserId.Should().Be(session.UserId);
        result.Value.Status.Should().Be(OnboardingSessionStatus.Active);
    }

    [Fact]
    public async Task Get_Returns_Error_When_Not_Found()
    {
        _repository.GetByIdAsync(Arg.Any<OnboardingSessionId>(), Arg.Any<CancellationToken>())
            .Returns((OnboardingSession?)null);

        var handler = new GetOnboardingSession.Handler(_repository);
        var result = await handler.Handle(
            new GetOnboardingSession.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Get_Validator_Rejects_Empty_SessionId()
    {
        var validator = new GetOnboardingSession.Validator();
        var result = await validator.ValidateAsync(new GetOnboardingSession.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ── ListOnboardingSessions ──────────────────────────────────────────

    [Fact]
    public async Task List_Returns_Empty_When_No_Sessions()
    {
        _repository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<OnboardingSession>().AsReadOnly());

        var handler = new ListOnboardingSessions.Handler(_repository);
        var result = await handler.Handle(new ListOnboardingSessions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_Returns_Sessions()
    {
        var s1 = CreateValidSession();
        var s2 = CreateValidSession();

        _repository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<OnboardingSession> { s1, s2 }.AsReadOnly());

        var handler = new ListOnboardingSessions.Handler(_repository);
        var result = await handler.Handle(new ListOnboardingSessions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_Filters_By_Status()
    {
        var session = CreateValidSession();
        _repository.ListAsync(null, OnboardingSessionStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<OnboardingSession> { session }.AsReadOnly());

        var handler = new ListOnboardingSessions.Handler(_repository);
        var result = await handler.Handle(
            new ListOnboardingSessions.Query(StatusValue: "Active"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_Validator_Rejects_Invalid_Status()
    {
        var validator = new ListOnboardingSessions.Validator();
        var result = await validator.ValidateAsync(
            new ListOnboardingSessions.Query(StatusValue: "Invalid"));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Active")]
    [InlineData("Completed")]
    [InlineData("Abandoned")]
    public async Task List_Validator_Accepts_Valid_Status(string? status)
    {
        var validator = new ListOnboardingSessions.Validator();
        var result = await validator.ValidateAsync(
            new ListOnboardingSessions.Query(StatusValue: status));
        result.IsValid.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static OnboardingSession CreateValidSession() =>
        OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Alice Smith",
            teamId: Guid.NewGuid(),
            teamName: "Platform Team",
            experienceLevel: OnboardingExperienceLevel.Mid,
            checklistItems: "[\"item1\",\"item2\"]",
            totalItems: 5,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);
}
