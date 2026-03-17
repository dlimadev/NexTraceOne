using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate Session.
/// </summary>
public sealed class SessionTests
{
    [Fact]
    public void Create_Should_BeActive_When_NotExpiredAndNotRevoked()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(
            UserId.New(),
            RefreshTokenHash.Create("refresh-token"),
            now.AddMinutes(30),
            "127.0.0.1",
            "unit-test");

        session.IsActive(now).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_ReturnTrue_When_ExpirationHasPassed()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(
            UserId.New(),
            RefreshTokenHash.Create("refresh-token"),
            now.AddMinutes(-1),
            "127.0.0.1",
            "unit-test");

        session.IsExpired(now).Should().BeTrue();
    }

    [Fact]
    public void Revoke_Should_DeactivateSession_When_Called()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(
            UserId.New(),
            RefreshTokenHash.Create("refresh-token"),
            now.AddMinutes(30),
            "127.0.0.1",
            "unit-test");

        session.Revoke(now);

        session.IsActive(now).Should().BeFalse();
        session.RevokedAt.Should().Be(now);
    }
}
