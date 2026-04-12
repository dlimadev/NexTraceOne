using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListNegativeFeedback;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class ListNegativeFeedbackTests
{
    private readonly IAiFeedbackRepository _repository = Substitute.For<IAiFeedbackRepository>();

    // ── No negatives ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_No_Negatives_Returns_Empty()
    {
        _repository.ListByRatingAsync(FeedbackRating.Negative, 50, Arg.Any<CancellationToken>())
            .Returns(new List<AiFeedback>().AsReadOnly());

        var handler = new ListNegativeFeedback.Handler(_repository);
        var result = await handler.Handle(new ListNegativeFeedback.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── With negatives ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_With_Negatives_Returns_Items()
    {
        var feedback1 = CreateNegativeFeedback("agent-a");
        var feedback2 = CreateNegativeFeedback("agent-b");

        _repository.ListByRatingAsync(FeedbackRating.Negative, 50, Arg.Any<CancellationToken>())
            .Returns(new List<AiFeedback> { feedback1, feedback2 }.AsReadOnly());

        var handler = new ListNegativeFeedback.Handler(_repository);
        var result = await handler.Handle(new ListNegativeFeedback.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Rating.Should().Be(FeedbackRating.Negative);
        result.Value.Items[1].Rating.Should().Be(FeedbackRating.Negative);
    }

    // ── Custom limit ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Respects_Custom_Limit()
    {
        var feedback = CreateNegativeFeedback("agent-x");
        _repository.ListByRatingAsync(FeedbackRating.Negative, 10, Arg.Any<CancellationToken>())
            .Returns(new List<AiFeedback> { feedback }.AsReadOnly());

        var handler = new ListNegativeFeedback.Handler(_repository);
        var result = await handler.Handle(new ListNegativeFeedback.Query(Limit: 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).ListByRatingAsync(FeedbackRating.Negative, 10, Arg.Any<CancellationToken>());
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Rejects_Zero_Limit()
    {
        var validator = new ListNegativeFeedback.Validator();
        var result = await validator.ValidateAsync(new ListNegativeFeedback.Query(Limit: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Rejects_Limit_Above_500()
    {
        var validator = new ListNegativeFeedback.Validator();
        var result = await validator.ValidateAsync(new ListNegativeFeedback.Query(Limit: 501));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Accepts_Default_Limit()
    {
        var validator = new ListNegativeFeedback.Validator();
        var result = await validator.ValidateAsync(new ListNegativeFeedback.Query());
        result.IsValid.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiFeedback CreateNegativeFeedback(string agentName) =>
        AiFeedback.Create(
            conversationId: Guid.NewGuid(),
            messageId: Guid.NewGuid(),
            agentExecutionId: null,
            rating: FeedbackRating.Negative,
            comment: "Not helpful",
            agentName: agentName,
            modelUsed: "llama3.2:3b",
            queryCategory: "general",
            createdByUserId: "user-1",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);
}
