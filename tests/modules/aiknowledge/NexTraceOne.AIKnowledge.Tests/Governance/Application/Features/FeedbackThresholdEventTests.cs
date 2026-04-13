using System.Linq;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para ModelFeedbackThresholdExceededIntegrationEvent
/// e a lógica de deteção de threshold de feedback negativo.
/// </summary>
public sealed class FeedbackThresholdEventTests
{
    private static readonly DateTimeOffset UtcNow =
        new DateTimeOffset(2026, 4, 13, 12, 0, 0, TimeSpan.Zero);

    // ── Event creation ───────────────────────────────────────────────────

    [Fact]
    public void ModelFeedbackThresholdExceededIntegrationEvent_CreatedCorrectly()
    {
        var ev = new ModelFeedbackThresholdExceededIntegrationEvent(
            "contract-agent",
            "gpt-4o",
            7,
            5,
            "24h",
            TenantId: null);

        ev.AgentName.Should().Be("contract-agent");
        ev.ModelUsed.Should().Be("gpt-4o");
        ev.NegativeCount.Should().Be(7);
        ev.ThresholdValue.Should().Be(5);
        ev.Period.Should().Be("24h");
        ev.TenantId.Should().BeNull();
    }

    [Fact]
    public void ModelFeedbackThresholdExceededIntegrationEvent_HasCorrectModuleSource()
    {
        var ev = new ModelFeedbackThresholdExceededIntegrationEvent(
            "test-agent", "claude-3", 6, 5, "24h", Guid.NewGuid());
        ev.SourceModule.Should().Be("AIKnowledge");
    }

    // ── CountNegativeSinceAsync ──────────────────────────────────────────

    [Fact]
    public async Task CountNegativeSinceAsync_ReturnsExpectedCount()
    {
        var repo = Substitute.For<IAiFeedbackRepository>();
        var since = UtcNow.AddHours(-24);

        repo.CountNegativeSinceAsync(
            "contract-agent",
            "gpt-4o",
            since,
            Arg.Any<CancellationToken>())
            .Returns(7);

        var count = await repo.CountNegativeSinceAsync(
            "contract-agent", "gpt-4o", since, CancellationToken.None);

        count.Should().Be(7);
        await repo.Received(1).CountNegativeSinceAsync(
            "contract-agent", "gpt-4o", since, Arg.Any<CancellationToken>());
    }

    // ── ListByRatingAsync + threshold logic ─────────────────────────────

    [Fact]
    public async Task ListByRatingAsync_FiltersAndGroupsNegativeFeedback()
    {
        var tenantId = Guid.NewGuid();
        var recent = UtcNow.AddHours(-2);
        var oldDate = UtcNow.AddHours(-30); // outside 24h window

        var feedbacks = new List<AiFeedback>
        {
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u1", tenantId, recent),
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u2", tenantId, recent),
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u3", tenantId, recent),
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u4", tenantId, recent),
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u5", tenantId, recent),
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u6", tenantId, recent),
            // Old feedback outside window
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-a", "gpt-4o", null, "u7", tenantId, oldDate),
            // Different agent — should not be included in agent-a group
            AiFeedback.Create(null, null, null, FeedbackRating.Negative, null, "agent-b", "gpt-4o", null, "u8", tenantId, recent),
        };

        var repo = Substitute.For<IAiFeedbackRepository>();
        repo.ListByRatingAsync(FeedbackRating.Negative, 500, Arg.Any<CancellationToken>())
            .Returns(feedbacks);

        var since = UtcNow.AddHours(-24);

        var all = await repo.ListByRatingAsync(FeedbackRating.Negative, 500, CancellationToken.None);

        var grouped = all
            .Where(f => f.SubmittedAt >= since)
            .GroupBy(f => (f.AgentName, f.ModelUsed))
            .ToList();

        grouped.Should().HaveCount(2); // agent-a/gpt-4o and agent-b/gpt-4o
        var agentAGroup = grouped.Single(g => g.Key.AgentName == "agent-a");
        agentAGroup.Count().Should().Be(6); // only recent feedbacks for agent-a
    }

    [Fact]
    public async Task ListByRatingAsync_BelowThreshold_NoGroupExceedsLimit()
    {
        var tenantId = Guid.NewGuid();
        var recent = UtcNow.AddHours(-1);
        const int threshold = 5;

        var feedbacks = Enumerable.Range(0, 4)
            .Select(i => AiFeedback.Create(
                null, null, null, FeedbackRating.Negative, null,
                "agent-x", "gpt-4o", null, $"user-{i}", tenantId, recent))
            .ToList();

        var repo = Substitute.For<IAiFeedbackRepository>();
        repo.ListByRatingAsync(FeedbackRating.Negative, 500, Arg.Any<CancellationToken>())
            .Returns(feedbacks);

        var since = UtcNow.AddHours(-24);
        var all = await repo.ListByRatingAsync(FeedbackRating.Negative, 500, CancellationToken.None);

        var exceeds = all
            .Where(f => f.SubmittedAt >= since)
            .GroupBy(f => (f.AgentName, f.ModelUsed))
            .Any(g => g.Count() >= threshold);

        exceeds.Should().BeFalse();
    }
}
