using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para o value object <see cref="SessionPolicy"/>.
/// Valida factory methods por perfil de configuração, política padrão
/// e igualdade estrutural entre instâncias.
/// </summary>
public sealed class SessionPolicyTests
{
    [Fact]
    public void ForSaaS_ShouldHaveStrictTimeouts()
    {
        var policy = SessionPolicy.ForSaaS();

        policy.MaxConcurrentSessions.Should().Be(3);
        policy.SessionTimeoutMinutes.Should().Be(30);
        policy.IdleTimeoutMinutes.Should().Be(15);
        policy.RequireReauthForSensitiveOps.Should().BeTrue();
        policy.AllowRememberMe.Should().BeFalse();
    }

    [Fact]
    public void ForStandardDeployment_ShouldHaveBalancedTimeouts()
    {
        var policy = SessionPolicy.ForStandardDeployment();

        policy.MaxConcurrentSessions.Should().Be(5);
        policy.SessionTimeoutMinutes.Should().Be(60);
        policy.IdleTimeoutMinutes.Should().Be(30);
        policy.RequireReauthForSensitiveOps.Should().BeTrue();
        policy.AllowRememberMe.Should().BeTrue();
        policy.RememberMeDays.Should().Be(14);
    }

    [Fact]
    public void ForRestrictedConnectivityDeployment_ShouldHavePermissiveTimeouts()
    {
        var policy = SessionPolicy.ForRestrictedConnectivityDeployment();

        policy.MaxConcurrentSessions.Should().Be(10);
        policy.SessionTimeoutMinutes.Should().Be(120);
        policy.IdleTimeoutMinutes.Should().Be(60);
        policy.RequireReauthForSensitiveOps.Should().BeFalse();
        policy.AllowRememberMe.Should().BeTrue();
        policy.RememberMeDays.Should().Be(30);
    }

    [Fact]
    public void Default_ShouldReturnReasonableDefaults()
    {
        var policy = SessionPolicy.Default();

        policy.MaxConcurrentSessions.Should().Be(5);
        policy.SessionTimeoutMinutes.Should().Be(60);
        policy.IdleTimeoutMinutes.Should().Be(30);
        policy.RequireReauthForSensitiveOps.Should().BeTrue();
        policy.AllowRememberMe.Should().BeFalse();
    }

    [Fact]
    public void SessionPolicy_Equality_SameValues_ShouldBeEqual()
    {
        var policy1 = SessionPolicy.ForSaaS();
        var policy2 = SessionPolicy.ForSaaS();

        policy1.Should().Be(policy2);
    }
}
