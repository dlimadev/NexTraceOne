using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAiFeedback;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SubmitAiFeedbackTests
{
    private readonly IAiFeedbackRepository _repository = Substitute.For<IAiFeedbackRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public SubmitAiFeedbackTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.Id.Returns("user-42");
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    // ── Handler ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Valid_Command_Succeeds()
    {
        var handler = new SubmitAiFeedback.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new SubmitAiFeedback.Command(
            ConversationId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            AgentExecutionId: null,
            RatingValue: "Positive",
            Comment: "Great answer!",
            AgentName: "incident-analyzer",
            ModelUsed: "llama3.2:3b",
            QueryCategory: "incident-analysis");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FeedbackId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<AiFeedback>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Negative_Rating_Succeeds()
    {
        var handler = new SubmitAiFeedback.Handler(
            _repository, _dateTimeProvider, _currentUser, _currentTenant);

        var command = new SubmitAiFeedback.Command(
            null, null, Guid.NewGuid(),
            "Negative", "Not useful", "contract-gen", "phi3:mini", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Rejects_Empty_AgentName()
    {
        var validator = new SubmitAiFeedback.Validator();
        var command = new SubmitAiFeedback.Command(
            null, null, null, "Positive", null, "", "model", null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Rejects_Empty_ModelUsed()
    {
        var validator = new SubmitAiFeedback.Validator();
        var command = new SubmitAiFeedback.Command(
            null, null, null, "Positive", null, "agent", "", null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Rejects_Invalid_RatingValue()
    {
        var validator = new SubmitAiFeedback.Validator();
        var command = new SubmitAiFeedback.Command(
            null, null, null, "Invalid", null, "agent", "model", null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Accepts_Valid_Command()
    {
        var validator = new SubmitAiFeedback.Validator();
        var command = new SubmitAiFeedback.Command(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Neutral", "OK response", "test-agent", "llama3.2", "general");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Positive")]
    [InlineData("Negative")]
    [InlineData("Neutral")]
    public async Task Validator_Accepts_All_Valid_Ratings(string rating)
    {
        var validator = new SubmitAiFeedback.Validator();
        var command = new SubmitAiFeedback.Command(
            null, null, null, rating, null, "agent", "model", null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }
}
