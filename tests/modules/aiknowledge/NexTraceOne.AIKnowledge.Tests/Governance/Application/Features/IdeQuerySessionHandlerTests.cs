using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitIdeQuery;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeQuerySession;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeQuerySessions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class IdeQuerySessionHandlerTests
{
    private readonly IIdeQuerySessionRepository _repository = Substitute.For<IIdeQuerySessionRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public IdeQuerySessionHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.Id.Returns("dev-42");
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    // ── SubmitIdeQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task Submit_Valid_Command_Succeeds()
    {
        var handler = new SubmitIdeQuery.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new SubmitIdeQuery.Command(
            IdeClient: "vscode",
            IdeClientVersion: "1.90.0",
            QueryTypeValue: "ContractSuggestion",
            QueryText: "Suggest a REST contract for OrderService",
            QueryContext: "{\"file\":\"OrderController.cs\"}",
            ModelUsed: "llama3.2:3b");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<IdeQuerySession>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ContractSuggestion")]
    [InlineData("BreakingChangeAlert")]
    [InlineData("OwnershipLookup")]
    [InlineData("TestGeneration")]
    [InlineData("GeneralQuery")]
    [InlineData("CodeGeneration")]
    public async Task Submit_All_QueryTypes_Succeed(string queryType)
    {
        var handler = new SubmitIdeQuery.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new SubmitIdeQuery.Command(
            "vscode", "1.0", queryType, "query text", null, "model");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── SubmitIdeQuery Validator ─────────────────────────────────────────

    [Fact]
    public async Task Submit_Validator_Rejects_Empty_IdeClient()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "", "1.0", "GeneralQuery", "query", null, "model");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_Validator_Rejects_Empty_IdeClientVersion()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "", "GeneralQuery", "query", null, "model");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_Validator_Rejects_Invalid_QueryType()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "1.0", "Invalid", "query", null, "model");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_Validator_Rejects_Empty_QueryText()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "1.0", "GeneralQuery", "", null, "model");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_Validator_Rejects_Empty_ModelUsed()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "1.0", "GeneralQuery", "query", null, "");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_Validator_Accepts_Valid_Command()
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "1.90.0", "ContractSuggestion",
            "Suggest a contract", "{\"file\":\"test.cs\"}", "llama3.2:3b");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ContractSuggestion")]
    [InlineData("BreakingChangeAlert")]
    [InlineData("OwnershipLookup")]
    [InlineData("TestGeneration")]
    [InlineData("GeneralQuery")]
    [InlineData("CodeGeneration")]
    public async Task Submit_Validator_Accepts_All_Valid_QueryTypes(string queryType)
    {
        var validator = new SubmitIdeQuery.Validator();
        var command = new SubmitIdeQuery.Command(
            "vscode", "1.0", queryType, "query", null, "model");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── GetIdeQuerySession ──────────────────────────────────────────────

    [Fact]
    public async Task Get_Returns_Session_When_Found()
    {
        var session = CreateValidSession();
        _repository.GetByIdAsync(Arg.Any<IdeQuerySessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetIdeQuerySession.Handler(_repository);
        var result = await handler.Handle(
            new GetIdeQuerySession.Query(session.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(session.Id.Value);
        result.Value.UserId.Should().Be(session.UserId);
        result.Value.IdeClient.Should().Be("vscode");
        result.Value.Status.Should().Be(IdeQuerySessionStatus.Processing);
    }

    [Fact]
    public async Task Get_Returns_Error_When_Not_Found()
    {
        _repository.GetByIdAsync(Arg.Any<IdeQuerySessionId>(), Arg.Any<CancellationToken>())
            .Returns((IdeQuerySession?)null);

        var handler = new GetIdeQuerySession.Handler(_repository);
        var result = await handler.Handle(
            new GetIdeQuerySession.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Get_Validator_Rejects_Empty_SessionId()
    {
        var validator = new GetIdeQuerySession.Validator();
        var result = await validator.ValidateAsync(new GetIdeQuerySession.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ── ListIdeQuerySessions ────────────────────────────────────────────

    [Fact]
    public async Task List_Returns_Empty_When_No_Sessions()
    {
        _repository.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<IdeQuerySession>().AsReadOnly());

        var handler = new ListIdeQuerySessions.Handler(_repository);
        var result = await handler.Handle(new ListIdeQuerySessions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_Returns_Sessions()
    {
        var s1 = CreateValidSession();
        var s2 = CreateValidSession();

        _repository.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<IdeQuerySession> { s1, s2 }.AsReadOnly());

        var handler = new ListIdeQuerySessions.Handler(_repository);
        var result = await handler.Handle(new ListIdeQuerySessions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_Filters_By_Status()
    {
        var session = CreateValidSession();
        _repository.ListAsync(null, null, IdeQuerySessionStatus.Processing, Arg.Any<CancellationToken>())
            .Returns(new List<IdeQuerySession> { session }.AsReadOnly());

        var handler = new ListIdeQuerySessions.Handler(_repository);
        var result = await handler.Handle(
            new ListIdeQuerySessions.Query(StatusValue: "Processing"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_Validator_Rejects_Invalid_Status()
    {
        var validator = new ListIdeQuerySessions.Validator();
        var result = await validator.ValidateAsync(
            new ListIdeQuerySessions.Query(StatusValue: "Invalid"));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Processing")]
    [InlineData("Responded")]
    [InlineData("Blocked")]
    [InlineData("Failed")]
    public async Task List_Validator_Accepts_Valid_Status(string? status)
    {
        var validator = new ListIdeQuerySessions.Validator();
        var result = await validator.ValidateAsync(
            new ListIdeQuerySessions.Query(StatusValue: status));
        result.IsValid.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static IdeQuerySession CreateValidSession() =>
        IdeQuerySession.Create(
            userId: "dev-1",
            ideClient: "vscode",
            ideClientVersion: "1.90.0",
            queryType: IdeQueryType.ContractSuggestion,
            queryText: "Suggest a REST contract for OrderService",
            queryContext: null,
            modelUsed: "llama3.2:3b",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);
}
