using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para o value object <see cref="MfaPolicy"/>.
/// Valida factory methods por deployment model, criação customizada,
/// validações de domínio e igualdade estrutural.
/// </summary>
public sealed class MfaPolicyTests
{
    [Fact]
    public void ForSaaS_ShouldRequireMfaOnLogin()
    {
        var policy = MfaPolicy.ForSaaS();

        policy.RequiredOnLogin.Should().BeTrue();
    }

    [Fact]
    public void ForSaaS_ShouldRequireMfaForPrivilegedOps()
    {
        var policy = MfaPolicy.ForSaaS();

        policy.RequiredForPrivilegedOps.Should().BeTrue();
    }

    [Fact]
    public void ForSaaS_ShouldRequireMfaForVendorOps()
    {
        var policy = MfaPolicy.ForSaaS();

        policy.RequiredForVendorOps.Should().BeTrue();
    }

    [Fact]
    public void ForSelfHosted_ShouldNotRequireMfaOnLogin()
    {
        var policy = MfaPolicy.ForSelfHosted();

        policy.RequiredOnLogin.Should().BeFalse();
    }

    [Fact]
    public void ForSelfHosted_ShouldRequireMfaForPrivilegedOps()
    {
        var policy = MfaPolicy.ForSelfHosted();

        policy.RequiredForPrivilegedOps.Should().BeTrue();
    }

    [Fact]
    public void ForOnPremise_ShouldNotRequireAnyMfa()
    {
        var policy = MfaPolicy.ForOnPremise();

        policy.RequiredOnLogin.Should().BeFalse();
        policy.RequiredForPrivilegedOps.Should().BeFalse();
        policy.RequiredForVendorOps.Should().BeFalse();
    }

    [Fact]
    public void Disabled_ShouldDisableAllMfa()
    {
        var policy = MfaPolicy.Disabled();

        policy.RequiredOnLogin.Should().BeFalse();
        policy.RequiredForPrivilegedOps.Should().BeFalse();
        policy.RequiredForVendorOps.Should().BeFalse();
        policy.AllowedMethods.Should().BeEmpty();
        policy.StepUpValidityMinutes.Should().Be(0);
        policy.MaxAttempts.Should().Be(0);
    }

    [Fact]
    public void Create_WithCustomValues_ShouldSetProperties()
    {
        var policy = MfaPolicy.Create(
            requiredOnLogin: true,
            requiredForPrivilegedOps: true,
            requiredForVendorOps: false,
            allowedMethods: "TOTP",
            stepUpValidityMinutes: 20,
            maxAttempts: 3);

        policy.RequiredOnLogin.Should().BeTrue();
        policy.RequiredForPrivilegedOps.Should().BeTrue();
        policy.RequiredForVendorOps.Should().BeFalse();
        policy.AllowedMethods.Should().Be("TOTP");
        policy.StepUpValidityMinutes.Should().Be(20);
        policy.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void MfaPolicy_Equality_SameValues_ShouldBeEqual()
    {
        var policy1 = MfaPolicy.ForSaaS();
        var policy2 = MfaPolicy.ForSaaS();

        policy1.Should().Be(policy2);
    }

    [Fact]
    public void MfaPolicy_Equality_DifferentValues_ShouldNotBeEqual()
    {
        var policy1 = MfaPolicy.ForSaaS();
        var policy2 = MfaPolicy.ForSelfHosted();

        policy1.Should().NotBe(policy2);
    }
}
