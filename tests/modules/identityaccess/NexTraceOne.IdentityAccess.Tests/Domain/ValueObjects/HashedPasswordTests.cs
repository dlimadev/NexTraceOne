using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes de domínio para o value object HashedPassword.
/// </summary>
public sealed class HashedPasswordTests
{
    [Fact]
    public void FromPlainText_Should_GenerateDifferentValue_When_PasswordIsHashed()
    {
        var password = HashedPassword.FromPlainText("P@ssw0rd123");

        password.Value.Should().NotBe("P@ssw0rd123");
    }

    [Fact]
    public void Verify_Should_ReturnTrue_When_PasswordMatchesHash()
    {
        var password = HashedPassword.FromPlainText("P@ssw0rd123");

        password.Verify("P@ssw0rd123").Should().BeTrue();
    }
}
