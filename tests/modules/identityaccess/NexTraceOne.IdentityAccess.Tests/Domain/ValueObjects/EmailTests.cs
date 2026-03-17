using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes de domínio para o value object Email.
/// </summary>
public sealed class EmailTests
{
    [Fact]
    public void Create_Should_NormalizeEmail_When_InputContainsSpacesAndUppercase()
    {
        var email = Email.Create("  ALICE@Example.COM  ");

        email.Value.Should().Be("alice@example.com");
    }

    [Fact]
    public void Create_Should_Throw_When_EmailIsInvalid()
    {
        var act = () => Email.Create("invalid-email");

        act.Should().Throw<ArgumentException>();
    }
}
