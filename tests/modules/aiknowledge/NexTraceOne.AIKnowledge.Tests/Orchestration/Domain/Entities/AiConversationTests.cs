using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Domain.Entities;

/// <summary>Testes unitários da entidade AiConversation — ciclo de vida multi-turno.</summary>
public sealed class AiConversationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static AiConversation CreateConversation() =>
        AiConversation.Start("OrderService", "Análise de impacto v2.0", "dev@co.com", FixedNow, Guid.NewGuid());

    // ── Start ─────────────────────────────────────────────────────────────

    [Fact]
    public void Start_ShouldInitializeWithActiveStatus()
    {
        var conversation = CreateConversation();

        conversation.Status.Should().Be(ConversationStatus.Active);
        conversation.TurnCount.Should().Be(0);
        conversation.Summary.Should().BeNull();
        conversation.LastTurnAt.Should().BeNull();
    }

    [Fact]
    public void Start_WithoutRelease_ShouldHaveNullReleaseId()
    {
        var conversation = AiConversation.Start("Svc", "Topic", "user@co.com", FixedNow);

        conversation.ReleaseId.Should().BeNull();
    }

    // ── AddTurn ───────────────────────────────────────────────────────────

    [Fact]
    public void AddTurn_WhenActive_ShouldIncrement()
    {
        var conversation = CreateConversation();

        var result1 = conversation.AddTurn(FixedNow.AddMinutes(1));
        var result2 = conversation.AddTurn(FixedNow.AddMinutes(5));

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        conversation.TurnCount.Should().Be(2);
        conversation.LastTurnAt.Should().Be(FixedNow.AddMinutes(5));
    }

    [Fact]
    public void AddTurn_WhenCompleted_ShouldFail()
    {
        var conversation = CreateConversation();
        conversation.Complete("Summary", FixedNow.AddMinutes(10));

        var result = conversation.AddTurn(FixedNow.AddMinutes(15));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Conversation.NotActive");
    }

    // ── Complete ──────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WithSummary_ShouldTransition()
    {
        var conversation = CreateConversation();
        conversation.AddTurn(FixedNow.AddMinutes(1));

        var result = conversation.Complete(
            "Impact analysis concluded: 3 consumers affected by breaking change.",
            FixedNow.AddMinutes(10));

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.Completed);
        conversation.Summary.Should().Contain("3 consumers");
    }

    [Fact]
    public void Complete_Twice_ShouldFail()
    {
        var conversation = CreateConversation();
        conversation.Complete("Summary 1", FixedNow.AddMinutes(5));

        var result = conversation.Complete("Summary 2", FixedNow.AddMinutes(10));

        result.IsFailure.Should().BeTrue();
    }

    // ── Expire ────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_WhenActive_ShouldTransition()
    {
        var conversation = CreateConversation();

        var result = conversation.Expire(FixedNow.AddHours(24));

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.Expired);
    }

    [Fact]
    public void Expire_WhenAlreadyCompleted_ShouldFail()
    {
        var conversation = CreateConversation();
        conversation.Complete("Summary", FixedNow.AddMinutes(5));

        var result = conversation.Expire(FixedNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
    }
}
