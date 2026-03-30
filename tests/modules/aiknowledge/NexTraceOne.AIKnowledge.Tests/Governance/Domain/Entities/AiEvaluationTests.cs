using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class AiEvaluationTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Evaluation()
    {
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var evaluation = AiEvaluation.Create(
            evaluationType: "user_feedback",
            conversationId: conversationId,
            messageId: messageId,
            agentExecutionId: null,
            userId: "user-1",
            tenantId: tenantId,
            modelName: "llama3.2:3b",
            promptTemplateName: "incident-root-cause-analysis",
            relevanceScore: 0.85m,
            accuracyScore: 0.90m,
            usefulnessScore: 0.75m,
            safetyScore: 1.0m,
            overallScore: 0.87m,
            feedback: "Very helpful analysis",
            tags: "grounding-good,detailed",
            evaluatedAt: now);

        evaluation.Should().NotBeNull();
        evaluation.Id.Value.Should().NotBeEmpty();
        evaluation.EvaluationType.Should().Be("user_feedback");
        evaluation.ConversationId.Should().Be(conversationId);
        evaluation.MessageId.Should().Be(messageId);
        evaluation.AgentExecutionId.Should().BeNull();
        evaluation.UserId.Should().Be("user-1");
        evaluation.TenantId.Should().Be(tenantId);
        evaluation.ModelName.Should().Be("llama3.2:3b");
        evaluation.PromptTemplateName.Should().Be("incident-root-cause-analysis");
        evaluation.RelevanceScore.Should().Be(0.85m);
        evaluation.AccuracyScore.Should().Be(0.90m);
        evaluation.UsefulnessScore.Should().Be(0.75m);
        evaluation.SafetyScore.Should().Be(1.0m);
        evaluation.OverallScore.Should().Be(0.87m);
        evaluation.Feedback.Should().Be("Very helpful analysis");
        evaluation.Tags.Should().Be("grounding-good,detailed");
        evaluation.EvaluatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_For_Agent_Execution_Without_Conversation()
    {
        var executionId = Guid.NewGuid();

        var evaluation = AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null,
            messageId: null,
            agentExecutionId: executionId,
            userId: "system",
            tenantId: Guid.NewGuid(),
            modelName: "phi3:mini",
            promptTemplateName: null,
            relevanceScore: 0.70m,
            accuracyScore: 0.65m,
            usefulnessScore: 0.80m,
            safetyScore: 0.95m,
            overallScore: 0.78m,
            feedback: null,
            tags: null,
            evaluatedAt: DateTimeOffset.UtcNow);

        evaluation.ConversationId.Should().BeNull();
        evaluation.MessageId.Should().BeNull();
        evaluation.AgentExecutionId.Should().Be(executionId);
        evaluation.PromptTemplateName.Should().BeNull();
        evaluation.Feedback.Should().BeNull();
        evaluation.Tags.Should().BeNull();
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var e1 = CreateValidEvaluation("user-1");
        var e2 = CreateValidEvaluation("user-2");

        e1.Id.Should().NotBe(e2.Id);
    }

    // ── Score boundary validation ───────────────────────────────────────

    [Fact]
    public void Create_Accepts_Zero_Scores()
    {
        var evaluation = AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0m, accuracyScore: 0m, usefulnessScore: 0m,
            safetyScore: 0m, overallScore: 0m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        evaluation.RelevanceScore.Should().Be(0m);
        evaluation.OverallScore.Should().Be(0m);
    }

    [Fact]
    public void Create_Accepts_Perfect_Scores()
    {
        var evaluation = AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 1m, accuracyScore: 1m, usefulnessScore: 1m,
            safetyScore: 1m, overallScore: 1m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        evaluation.RelevanceScore.Should().Be(1m);
        evaluation.OverallScore.Should().Be(1m);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(-1)]
    [InlineData(2)]
    public void Create_Rejects_Invalid_RelevanceScore(double score)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: (decimal)score, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_Rejects_Invalid_AccuracyScore(double score)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: (decimal)score, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_Rejects_Invalid_UsefulnessScore(double score)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: (decimal)score,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_Rejects_Invalid_SafetyScore(double score)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: (decimal)score, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_Rejects_Invalid_OverallScore(double score)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: (decimal)score,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_EvaluationType(string? evaluationType)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: evaluationType!,
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_UserId(string? userId)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: userId!, tenantId: Guid.NewGuid(), modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Default_TenantId()
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.Empty, modelName: "model",
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_ModelName(string? modelName)
    {
        var act = () => AiEvaluation.Create(
            evaluationType: "automatic",
            conversationId: null, messageId: null, agentExecutionId: null,
            userId: "user-1", tenantId: Guid.NewGuid(), modelName: modelName!,
            promptTemplateName: null,
            relevanceScore: 0.5m, accuracyScore: 0.5m, usefulnessScore: 0.5m,
            safetyScore: 0.5m, overallScore: 0.5m,
            feedback: null, tags: null, evaluatedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void AiEvaluationId_New_Creates_Unique_Id()
    {
        var id1 = AiEvaluationId.New();
        var id2 = AiEvaluationId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void AiEvaluationId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = AiEvaluationId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiEvaluation CreateValidEvaluation(string userId) =>
        AiEvaluation.Create(
            evaluationType: "user_feedback",
            conversationId: Guid.NewGuid(),
            messageId: Guid.NewGuid(),
            agentExecutionId: null,
            userId: userId,
            tenantId: Guid.NewGuid(),
            modelName: "llama3.2:3b",
            promptTemplateName: null,
            relevanceScore: 0.8m,
            accuracyScore: 0.8m,
            usefulnessScore: 0.8m,
            safetyScore: 0.9m,
            overallScore: 0.85m,
            feedback: "Good",
            tags: null,
            evaluatedAt: DateTimeOffset.UtcNow);
}
