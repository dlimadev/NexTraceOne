using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetFeedbackMetrics;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class GetFeedbackMetricsTests
{
    private readonly IAiFeedbackRepository _repository = Substitute.For<IAiFeedbackRepository>();

    // ── No feedback ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_No_Feedback_Returns_Zeros()
    {
        _repository.CountByRatingAsync(FeedbackRating.Positive, Arg.Any<CancellationToken>()).Returns(0);
        _repository.CountByRatingAsync(FeedbackRating.Negative, Arg.Any<CancellationToken>()).Returns(0);
        _repository.CountByRatingAsync(FeedbackRating.Neutral, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetFeedbackMetrics.Handler(_repository);
        var result = await handler.Handle(new GetFeedbackMetrics.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFeedbacks.Should().Be(0);
        result.Value.PositiveCount.Should().Be(0);
        result.Value.NegativeCount.Should().Be(0);
        result.Value.NeutralCount.Should().Be(0);
        result.Value.SatisfactionRate.Should().Be(0m);
    }

    // ── Mixed feedback ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Mixed_Feedback_Returns_Correct_Metrics()
    {
        _repository.CountByRatingAsync(FeedbackRating.Positive, Arg.Any<CancellationToken>()).Returns(7);
        _repository.CountByRatingAsync(FeedbackRating.Negative, Arg.Any<CancellationToken>()).Returns(3);
        _repository.CountByRatingAsync(FeedbackRating.Neutral, Arg.Any<CancellationToken>()).Returns(5);

        var handler = new GetFeedbackMetrics.Handler(_repository);
        var result = await handler.Handle(new GetFeedbackMetrics.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFeedbacks.Should().Be(15);
        result.Value.PositiveCount.Should().Be(7);
        result.Value.NegativeCount.Should().Be(3);
        result.Value.NeutralCount.Should().Be(5);
    }

    // ── Satisfaction rate calculation ────────────────────────────────────

    [Fact]
    public async Task Handle_Calculates_SatisfactionRate_Correctly()
    {
        // 7 positive / (7 positive + 3 negative) = 70%
        _repository.CountByRatingAsync(FeedbackRating.Positive, Arg.Any<CancellationToken>()).Returns(7);
        _repository.CountByRatingAsync(FeedbackRating.Negative, Arg.Any<CancellationToken>()).Returns(3);
        _repository.CountByRatingAsync(FeedbackRating.Neutral, Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetFeedbackMetrics.Handler(_repository);
        var result = await handler.Handle(new GetFeedbackMetrics.Query(null), CancellationToken.None);

        result.Value.SatisfactionRate.Should().Be(70.00m);
    }

    [Fact]
    public async Task Handle_All_Positive_Returns_100_Percent()
    {
        _repository.CountByRatingAsync(FeedbackRating.Positive, Arg.Any<CancellationToken>()).Returns(10);
        _repository.CountByRatingAsync(FeedbackRating.Negative, Arg.Any<CancellationToken>()).Returns(0);
        _repository.CountByRatingAsync(FeedbackRating.Neutral, Arg.Any<CancellationToken>()).Returns(5);

        var handler = new GetFeedbackMetrics.Handler(_repository);
        var result = await handler.Handle(new GetFeedbackMetrics.Query(null), CancellationToken.None);

        result.Value.SatisfactionRate.Should().Be(100.00m);
    }

    [Fact]
    public async Task Handle_All_Negative_Returns_0_Percent()
    {
        _repository.CountByRatingAsync(FeedbackRating.Positive, Arg.Any<CancellationToken>()).Returns(0);
        _repository.CountByRatingAsync(FeedbackRating.Negative, Arg.Any<CancellationToken>()).Returns(5);
        _repository.CountByRatingAsync(FeedbackRating.Neutral, Arg.Any<CancellationToken>()).Returns(2);

        var handler = new GetFeedbackMetrics.Handler(_repository);
        var result = await handler.Handle(new GetFeedbackMetrics.Query(null), CancellationToken.None);

        result.Value.SatisfactionRate.Should().Be(0m);
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Accepts_Null_AgentName()
    {
        var validator = new GetFeedbackMetrics.Validator();
        var result = await validator.ValidateAsync(new GetFeedbackMetrics.Query(null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_Accepts_Valid_AgentName()
    {
        var validator = new GetFeedbackMetrics.Validator();
        var result = await validator.ValidateAsync(new GetFeedbackMetrics.Query("incident-analyzer"));
        result.IsValid.Should().BeTrue();
    }
}
