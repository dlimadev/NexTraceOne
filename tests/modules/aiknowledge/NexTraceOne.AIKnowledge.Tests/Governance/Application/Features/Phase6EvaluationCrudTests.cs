using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluations;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitEvaluation;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class Phase6EvaluationCrudTests
{
    private readonly IAiEvaluationRepository _repository = Substitute.For<IAiEvaluationRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public Phase6EvaluationCrudTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── ListEvaluations ─────────────────────────────────────────────────

    [Fact]
    public async Task ListEvaluations_By_Conversation_Returns_Results()
    {
        var conversationId = Guid.NewGuid();
        var evaluations = new List<AiEvaluation>
        {
            CreateTestEvaluation(conversationId: conversationId)
        };
        _repository.GetByConversationAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(evaluations.AsReadOnly());

        var handler = new ListEvaluations.Handler(_repository);
        var result = await handler.Handle(
            new ListEvaluations.Query(conversationId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListEvaluations_By_AgentExecution_Returns_Results()
    {
        var executionId = Guid.NewGuid();
        var evaluations = new List<AiEvaluation>
        {
            CreateTestEvaluation(agentExecutionId: executionId)
        };
        _repository.GetByAgentExecutionAsync(executionId, Arg.Any<CancellationToken>())
            .Returns(evaluations.AsReadOnly());

        var handler = new ListEvaluations.Handler(_repository);
        var result = await handler.Handle(
            new ListEvaluations.Query(null, executionId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListEvaluations_By_User_Returns_Results()
    {
        var evaluations = new List<AiEvaluation>
        {
            CreateTestEvaluation()
        };
        _repository.GetByUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(evaluations.AsReadOnly());

        var handler = new ListEvaluations.Handler(_repository);
        var result = await handler.Handle(
            new ListEvaluations.Query(null, null, "user-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListEvaluations_No_Filters_Returns_Empty()
    {
        var handler = new ListEvaluations.Handler(_repository);
        var result = await handler.Handle(
            new ListEvaluations.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetEvaluation ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEvaluation_Existing_Returns_Details()
    {
        var evaluation = CreateTestEvaluation();
        _repository.GetByIdAsync(Arg.Any<AiEvaluationId>(), Arg.Any<CancellationToken>())
            .Returns(evaluation);

        var handler = new GetEvaluation.Handler(_repository);
        var result = await handler.Handle(
            new GetEvaluation.Query(evaluation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelName.Should().Be("llama3.2");
    }

    [Fact]
    public async Task GetEvaluation_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<AiEvaluationId>(), Arg.Any<CancellationToken>())
            .Returns((AiEvaluation?)null);

        var handler = new GetEvaluation.Handler(_repository);
        var result = await handler.Handle(
            new GetEvaluation.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── SubmitEvaluation ────────────────────────────────────────────────

    [Fact]
    public async Task SubmitEvaluation_Valid_Succeeds()
    {
        var handler = new SubmitEvaluation.Handler(_repository, _dateTimeProvider);
        var command = new SubmitEvaluation.Command(
            "user_feedback", Guid.NewGuid(), Guid.NewGuid(), null,
            "user-1", Guid.NewGuid(), "llama3.2", null,
            0.8m, 0.9m, 0.7m, 1.0m, 0.85m,
            "Good response", "accurate");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluationId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<AiEvaluation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Rejects_Invalid_Type()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "invalid_type", null, null, null,
            "user-1", Guid.NewGuid(), "model", null,
            0.5m, 0.5m, 0.5m, 0.5m, 0.5m, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Rejects_Score_Above_One()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "automatic", null, null, null,
            "user-1", Guid.NewGuid(), "model", null,
            1.5m, 0.5m, 0.5m, 0.5m, 0.5m, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Rejects_Negative_Score()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "automatic", null, null, null,
            "user-1", Guid.NewGuid(), "model", null,
            -0.1m, 0.5m, 0.5m, 0.5m, 0.5m, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Accepts_Valid_Command()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "user_feedback", Guid.NewGuid(), null, null,
            "user-1", Guid.NewGuid(), "llama3.2", "template-1",
            0.8m, 0.9m, 0.7m, 1.0m, 0.85m,
            "Excellent", "relevant,accurate");

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Accepts_Boundary_Scores()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "automatic", null, null, null,
            "user-1", Guid.NewGuid(), "model", null,
            0.0m, 0.0m, 0.0m, 0.0m, 0.0m, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitEvaluation_Validator_Accepts_Max_Scores()
    {
        var validator = new SubmitEvaluation.Validator();
        var command = new SubmitEvaluation.Command(
            "peer_review", null, null, Guid.NewGuid(),
            "user-1", Guid.NewGuid(), "model", null,
            1.0m, 1.0m, 1.0m, 1.0m, 1.0m, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiEvaluation CreateTestEvaluation(
        Guid? conversationId = null,
        Guid? agentExecutionId = null) =>
        AiEvaluation.Create(
            evaluationType: "user_feedback",
            conversationId: conversationId ?? Guid.NewGuid(),
            messageId: Guid.NewGuid(),
            agentExecutionId: agentExecutionId,
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            modelName: "llama3.2",
            promptTemplateName: null,
            relevanceScore: 0.8m,
            accuracyScore: 0.9m,
            usefulnessScore: 0.7m,
            safetyScore: 1.0m,
            overallScore: 0.85m,
            feedback: "Good",
            tags: "accurate",
            evaluatedAt: DateTimeOffset.UtcNow);
}
