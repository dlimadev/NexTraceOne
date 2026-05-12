using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

public sealed class PasswordResetTokenTests
{
    private static readonly UserId SomeUserId = UserId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_Should_SetAllProperties()
    {
        var token = PasswordResetToken.Create(SomeUserId, "hash456", Now);

        token.UserId.Should().Be(SomeUserId);
        token.TokenHash.Should().Be("hash456");
        token.CreatedAt.Should().Be(Now);
        token.ExpiresAt.Should().Be(Now.Add(PasswordResetToken.DefaultExpiry));
        token.UsedAt.Should().BeNull();
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void DefaultExpiry_Should_Be_OneHour()
    {
        PasswordResetToken.DefaultExpiry.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void IsValid_Should_ReturnTrue_When_NotUsedAndNotExpired()
    {
        var token = PasswordResetToken.Create(SomeUserId, "hash", Now);

        token.IsValid(Now.AddMinutes(30)).Should().BeTrue();
    }

    [Fact]
    public void IsValid_Should_ReturnFalse_When_Expired()
    {
        var token = PasswordResetToken.Create(SomeUserId, "hash", Now);

        token.IsValid(Now.Add(PasswordResetToken.DefaultExpiry).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_ReturnFalse_When_AlreadyUsed()
    {
        var token = PasswordResetToken.Create(SomeUserId, "hash", Now);
        token.MarkUsed(Now.AddMinutes(5));

        token.IsValid(Now.AddMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_Should_MarkTokenConsumed()
    {
        var token = PasswordResetToken.Create(SomeUserId, "hash", Now);
        var usedAt = Now.AddMinutes(3);

        token.MarkUsed(usedAt);

        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().Be(usedAt);
    }
}

