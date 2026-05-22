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
        var password = HashedPassword.FromPlainText("P@ssw0rd123!");

        password.Value.Should().NotBe("P@ssw0rd123!");
    }

    [Fact]
    public void Verify_Should_ReturnTrue_When_PasswordMatchesHash()
    {
        var password = HashedPassword.FromPlainText("P@ssw0rd123!");

        password.Verify("P@ssw0rd123!").Should().BeTrue();
    }

    [Fact]
    public void FromPlainText_Should_UseV2Format_ForNewHashes()
    {
        var password = HashedPassword.FromPlainText("P@ssw0rd123!");

        password.Value.Should().StartWith("v2.");
    }

    [Fact]
    public void Verify_Should_ReturnFalse_When_PasswordDoesNotMatch()
    {
        var password = HashedPassword.FromPlainText("P@ssw0rd123!");

        password.Verify("WrongPassword99!").Should().BeFalse();
    }

    [Fact]
    public void FromPlainText_Should_Throw_When_PasswordIsTooShort()
    {
        var act = () => HashedPassword.FromPlainText("short");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least 12 characters*");
    }

    [Fact]
    public void Verify_Should_SupportLegacyV1Hashes()
    {
        // V1 hashes persisted before the v2 upgrade must still verify correctly.
        var salt = new byte[16];
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            System.Text.Encoding.UTF8.GetBytes("P@ssw0rd123!"),
            salt,
            100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32);
        var legacyHash = $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        var hashedPassword = HashedPassword.FromHash(legacyHash);

        hashedPassword.Verify("P@ssw0rd123!").Should().BeTrue();
    }
}
