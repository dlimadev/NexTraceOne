using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para o value object <see cref="AuthenticationMode"/>.
/// Valida criação via factory method, propriedades de suporte a login local/federado
/// e igualdade estrutural entre instâncias.
/// </summary>
public sealed class AuthenticationModeTests
{
    [Fact]
    public void From_ValidFederated_ReturnsMode()
    {
        var mode = AuthenticationMode.From("Federated");

        mode.Value.Should().Be("Federated");
        mode.IsFederated.Should().BeTrue();
    }

    [Fact]
    public void From_ValidLocal_ReturnsMode()
    {
        var mode = AuthenticationMode.From("Local");

        mode.Value.Should().Be("Local");
        mode.IsLocal.Should().BeTrue();
    }

    [Fact]
    public void From_ValidHybrid_ReturnsMode()
    {
        var mode = AuthenticationMode.From("Hybrid");

        mode.Value.Should().Be("Hybrid");
        mode.IsHybrid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Invalid")]
    [InlineData("Basic")]
    public void From_InvalidValue_ThrowsException(string value)
    {
        var act = () => AuthenticationMode.From(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_NullValue_ThrowsException()
    {
        var act = () => AuthenticationMode.From(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_CaseInsensitive_ReturnsCanonicalValue()
    {
        var mode = AuthenticationMode.From("federated");

        mode.Value.Should().Be("Federated");
    }

    [Fact]
    public void SupportsLocalLogin_Local_ReturnsTrue()
    {
        AuthenticationMode.Local.SupportsLocalLogin.Should().BeTrue();
    }

    [Fact]
    public void SupportsLocalLogin_Federated_ReturnsFalse()
    {
        AuthenticationMode.Federated.SupportsLocalLogin.Should().BeFalse();
    }

    [Fact]
    public void SupportsFederatedLogin_Federated_ReturnsTrue()
    {
        AuthenticationMode.Federated.SupportsFederatedLogin.Should().BeTrue();
    }

    [Fact]
    public void SupportsFederatedLogin_Local_ReturnsFalse()
    {
        AuthenticationMode.Local.SupportsFederatedLogin.Should().BeFalse();
    }

    [Fact]
    public void Hybrid_SupportsBothLoginMethods()
    {
        var mode = AuthenticationMode.Hybrid;

        mode.SupportsLocalLogin.Should().BeTrue();
        mode.SupportsFederatedLogin.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var mode1 = AuthenticationMode.From("Local");
        var mode2 = AuthenticationMode.From("Local");

        mode1.Should().Be(mode2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var mode1 = AuthenticationMode.Federated;
        var mode2 = AuthenticationMode.Local;

        mode1.Should().NotBe(mode2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var mode = AuthenticationMode.Hybrid;

        mode.ToString().Should().Be("Hybrid");
    }
}

