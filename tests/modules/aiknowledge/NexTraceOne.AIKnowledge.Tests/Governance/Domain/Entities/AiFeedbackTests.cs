using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class AiFeedbackTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Feedback()
    {
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var feedback = AiFeedback.Create(
            conversationId: conversationId,
            messageId: messageId,
            agentExecutionId: null,
            rating: FeedbackRating.Positive,
            comment: "Very helpful answer",
            agentName: "incident-analyzer",
            modelUsed: "llama3.2:3b",
            queryCategory: "incident-analysis",
            createdByUserId: "user-1",
            tenantId: tenantId,
            submittedAt: now);

        feedback.Should().NotBeNull();
        feedback.Id.Value.Should().NotBeEmpty();
        feedback.ConversationId.Should().Be(conversationId);
        feedback.MessageId.Should().Be(messageId);
        feedback.AgentExecutionId.Should().BeNull();
        feedback.Rating.Should().Be(FeedbackRating.Positive);
        feedback.Comment.Should().Be("Very helpful answer");
        feedback.AgentName.Should().Be("incident-analyzer");
        feedback.ModelUsed.Should().Be("llama3.2:3b");
        feedback.QueryCategory.Should().Be("incident-analysis");
        feedback.CreatedByUserId.Should().Be("user-1");
        feedback.TenantId.Should().Be(tenantId);
        feedback.SubmittedAt.Should().Be(now);
    }

    [Fact]
    public void Create_With_Negative_Rating()
    {
        var feedback = CreateValidFeedback(rating: FeedbackRating.Negative);
        feedback.Rating.Should().Be(FeedbackRating.Negative);
    }

    [Fact]
    public void Create_With_Neutral_Rating()
    {
        var feedback = CreateValidFeedback(rating: FeedbackRating.Neutral);
        feedback.Rating.Should().Be(FeedbackRating.Neutral);
    }

    [Fact]
    public void Create_With_Positive_Rating()
    {
        var feedback = CreateValidFeedback(rating: FeedbackRating.Positive);
        feedback.Rating.Should().Be(FeedbackRating.Positive);
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var f1 = CreateValidFeedback();
        var f2 = CreateValidFeedback();
        f1.Id.Should().NotBe(f2.Id);
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_AgentName(string? agentName)
    {
        var act = () => AiFeedback.Create(
            conversationId: null, messageId: null, agentExecutionId: null,
            rating: FeedbackRating.Positive, comment: null,
            agentName: agentName!, modelUsed: "model",
            queryCategory: null, createdByUserId: "user-1",
            tenantId: Guid.NewGuid(), submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_ModelUsed(string? modelUsed)
    {
        var act = () => AiFeedback.Create(
            conversationId: null, messageId: null, agentExecutionId: null,
            rating: FeedbackRating.Positive, comment: null,
            agentName: "agent", modelUsed: modelUsed!,
            queryCategory: null, createdByUserId: "user-1",
            tenantId: Guid.NewGuid(), submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_CreatedByUserId(string? userId)
    {
        var act = () => AiFeedback.Create(
            conversationId: null, messageId: null, agentExecutionId: null,
            rating: FeedbackRating.Positive, comment: null,
            agentName: "agent", modelUsed: "model",
            queryCategory: null, createdByUserId: userId!,
            tenantId: Guid.NewGuid(), submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Default_TenantId()
    {
        var act = () => AiFeedback.Create(
            conversationId: null, messageId: null, agentExecutionId: null,
            rating: FeedbackRating.Positive, comment: null,
            agentName: "agent", modelUsed: "model",
            queryCategory: null, createdByUserId: "user-1",
            tenantId: Guid.Empty, submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Invalid_Rating()
    {
        var act = () => AiFeedback.Create(
            conversationId: null, messageId: null, agentExecutionId: null,
            rating: (FeedbackRating)99, comment: null,
            agentName: "agent", modelUsed: "model",
            queryCategory: null, createdByUserId: "user-1",
            tenantId: Guid.NewGuid(), submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void AiFeedbackId_New_Creates_Unique_Id()
    {
        var id1 = AiFeedbackId.New();
        var id2 = AiFeedbackId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void AiFeedbackId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = AiFeedbackId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiFeedback CreateValidFeedback(FeedbackRating rating = FeedbackRating.Positive) =>
        AiFeedback.Create(
            conversationId: Guid.NewGuid(),
            messageId: Guid.NewGuid(),
            agentExecutionId: null,
            rating: rating,
            comment: "Test feedback",
            agentName: "test-agent",
            modelUsed: "llama3.2:3b",
            queryCategory: null,
            createdByUserId: "user-1",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);
}
