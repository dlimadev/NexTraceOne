using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

public sealed class AccountActivationTokenTests
{
    private static readonly UserId SomeUserId = UserId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_Should_SetAllProperties()
    {
        var token = AccountActivationToken.Create(SomeUserId, "hash123", Now);

        token.UserId.Should().Be(SomeUserId);
        token.TokenHash.Should().Be("hash123");
        token.CreatedAt.Should().Be(Now);
        token.ExpiresAt.Should().Be(Now.Add(AccountActivationToken.DefaultExpiry));
        token.UsedAt.Should().BeNull();
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_ReturnTrue_When_NotUsedAndNotExpired()
    {
        var token = AccountActivationToken.Create(SomeUserId, "hash", Now);

        token.IsValid(Now.AddHours(1)).Should().BeTrue();
    }

    [Fact]
    public void IsValid_Should_ReturnFalse_When_TokenIsExpired()
    {
        var token = AccountActivationToken.Create(SomeUserId, "hash", Now);

        token.IsValid(Now.Add(AccountActivationToken.DefaultExpiry).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_ReturnFalse_When_TokenIsUsed()
    {
        var token = AccountActivationToken.Create(SomeUserId, "hash", Now);
        token.MarkUsed(Now.AddMinutes(10));

        token.IsValid(Now.AddMinutes(15)).Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_Should_SetUsedAt()
    {
        var token = AccountActivationToken.Create(SomeUserId, "hash", Now);
        var usedAt = Now.AddMinutes(5);

        token.MarkUsed(usedAt);

        token.UsedAt.Should().Be(usedAt);
        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var t1 = AccountActivationToken.Create(SomeUserId, "h1", Now);
        var t2 = AccountActivationToken.Create(SomeUserId, "h2", Now);

        t1.Id.Should().NotBe(t2.Id);
    }
}

