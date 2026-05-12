using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para o value object <see cref="DeploymentModel"/>.
/// Valida criação via factory method, propriedades derivadas de conectividade
/// e igualdade estrutural entre instâncias.
/// </summary>
public sealed class DeploymentModelTests
{
    [Fact]
    public void From_ValidSaaS_ReturnsDeploymentModel()
    {
        var model = DeploymentModel.From("SaaS");

        model.Value.Should().Be("SaaS");
        model.IsSaaS.Should().BeTrue();
    }

    [Fact]
    public void From_ValidSelfHosted_ReturnsDeploymentModel()
    {
        var model = DeploymentModel.From("SelfHosted");

        model.Value.Should().Be("SelfHosted");
        model.IsSelfHosted.Should().BeTrue();
    }

    [Fact]
    public void From_ValidOnPremise_ReturnsDeploymentModel()
    {
        var model = DeploymentModel.From("OnPremise");

        model.Value.Should().Be("OnPremise");
        model.IsOnPremise.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Invalid")]
    [InlineData("cloud")]
    public void From_InvalidValue_ThrowsException(string value)
    {
        var act = () => DeploymentModel.From(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_NullValue_ThrowsException()
    {
        var act = () => DeploymentModel.From(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_CaseInsensitive_ReturnsCanonicalValue()
    {
        var model = DeploymentModel.From("saas");

        model.Value.Should().Be("SaaS");
    }

    [Fact]
    public void AllowsExternalConnectivity_SaaS_ReturnsTrue()
    {
        var model = DeploymentModel.SaaS;

        model.AllowsExternalConnectivity.Should().BeTrue();
    }

    [Fact]
    public void AllowsExternalConnectivity_SelfHosted_ReturnsTrue()
    {
        var model = DeploymentModel.SelfHosted;

        model.AllowsExternalConnectivity.Should().BeTrue();
    }

    [Fact]
    public void AllowsExternalConnectivity_OnPremise_ReturnsFalse()
    {
        var model = DeploymentModel.OnPremise;

        model.AllowsExternalConnectivity.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var model1 = DeploymentModel.From("SaaS");
        var model2 = DeploymentModel.From("SaaS");

        model1.Should().Be(model2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var model1 = DeploymentModel.SaaS;
        var model2 = DeploymentModel.OnPremise;

        model1.Should().NotBe(model2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var model = DeploymentModel.SelfHosted;

        model.ToString().Should().Be("SelfHosted");
    }
}

