using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para SecurityEvent e SecurityEventType.
/// Cobre criação, score de risco e revisão administrativa.
/// </summary>
public sealed class SecurityEventTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_BeUnreviewed_When_Created()
    {
        var evt = SecurityEvent.Create(
            TenantId.New(), UserId.New(), SessionId.New(),
            SecurityEventType.UnknownLocation,
            "Login from new IP address 203.0.113.42.",
            riskScore: 60,
            "203.0.113.42", "Mozilla/5.0", null, Now);

        evt.IsReviewed.Should().BeFalse();
        evt.RiskScore.Should().Be(60);
        evt.EventType.Should().Be(SecurityEventType.UnknownLocation);
    }

    [Fact]
    public void Create_Should_ClampRiskScore_When_OutOfRange()
    {
        var evt = SecurityEvent.Create(
            TenantId.New(), UserId.New(), null,
            SecurityEventType.BreakGlassActivated,
            "Break glass activated.", riskScore: 150,
            null, null, null, Now);

        evt.RiskScore.Should().Be(100);
    }

    [Fact]
    public void MarkReviewed_Should_UpdateReviewFields_When_Called()
    {
        var evt = SecurityEvent.Create(
            TenantId.New(), UserId.New(), null,
            SecurityEventType.ConcurrentSessions,
            "Multiple simultaneous sessions detected.",
            riskScore: 45, null, null, null, Now);

        var reviewer = UserId.New();
        evt.MarkReviewed(reviewer, Now.AddHours(2));

        evt.IsReviewed.Should().BeTrue();
        evt.ReviewedBy.Should().Be(reviewer);
        evt.ReviewedAt.Should().Be(Now.AddHours(2));
    }
}

