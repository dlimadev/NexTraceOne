using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para o value object <see cref="AuthenticationPolicy"/>.
/// Valida os factory methods (ForSaaS, ForSelfHosted, Default), regras de consistência
/// entre modo de autenticação e parâmetros operacionais, limites de sessão/timeout
/// e igualdade estrutural entre instâncias.
/// </summary>
public sealed class AuthenticationPolicyTests
{
    [Fact]
    public void ForSaaS_CreatesCorrectPolicy()
    {
        var policy = AuthenticationPolicy.ForSaaS("AzureAD");

        policy.Mode.Should().Be(AuthenticationMode.Federated);
        policy.AllowLocalFallback.Should().BeFalse();
        policy.RequireMfa.Should().BeTrue();
        policy.DefaultOidcProvider.Should().Be("AzureAD");
        policy.SessionTimeoutMinutes.Should().Be(30);
        policy.MaxConcurrentSessionsPerUser.Should().Be(3);
    }

    [Fact]
    public void ForSelfHosted_CreatesCorrectPolicy()
    {
        var policy = AuthenticationPolicy.ForSelfHosted();

        policy.Mode.Should().Be(AuthenticationMode.Hybrid);
        policy.AllowLocalFallback.Should().BeTrue();
        policy.RequireMfa.Should().BeFalse();
        policy.DefaultOidcProvider.Should().BeNull();
        policy.SessionTimeoutMinutes.Should().Be(60);
        policy.MaxConcurrentSessionsPerUser.Should().Be(5);
    }

    [Fact]
    public void Default_CreatesHybridPolicy()
    {
        var policy = AuthenticationPolicy.Default();

        policy.Mode.Should().Be(AuthenticationMode.Hybrid);
        policy.AllowLocalFallback.Should().BeTrue();
        policy.RequireMfa.Should().BeFalse();
        policy.DefaultOidcProvider.Should().BeNull();
        policy.SessionTimeoutMinutes.Should().Be(60);
        policy.MaxConcurrentSessionsPerUser.Should().Be(5);
    }

    [Fact]
    public void Create_WithCustomValues_SetsAllProperties()
    {
        var policy = AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            allowLocalFallback: true,
            requireMfa: true,
            defaultOidcProvider: null,
            sessionTimeoutMinutes: 120,
            maxConcurrentSessions: 10);

        policy.Mode.Should().Be(AuthenticationMode.Local);
        policy.AllowLocalFallback.Should().BeTrue();
        policy.RequireMfa.Should().BeTrue();
        policy.DefaultOidcProvider.Should().BeNull();
        policy.SessionTimeoutMinutes.Should().Be(120);
        policy.MaxConcurrentSessionsPerUser.Should().Be(10);
    }

    [Fact]
    public void Create_NegativeSessionTimeout_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            sessionTimeoutMinutes: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_SessionTimeoutBelowMinimum_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            sessionTimeoutMinutes: 4);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_SessionTimeoutAboveMaximum_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            sessionTimeoutMinutes: 1441);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ZeroMaxSessions_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            maxConcurrentSessions: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_MaxSessionsAboveLimit_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Local,
            maxConcurrentSessions: 101);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_FederatedWithLocalFallback_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Federated,
            allowLocalFallback: true,
            defaultOidcProvider: "Okta");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Local fallback*not allowed*Federated*");
    }

    [Fact]
    public void Create_FederatedWithoutOidcProvider_ThrowsException()
    {
        var act = () => AuthenticationPolicy.Create(
            mode: AuthenticationMode.Federated,
            defaultOidcProvider: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*OIDC provider*required*Federated*");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var policy1 = AuthenticationPolicy.ForSelfHosted();
        var policy2 = AuthenticationPolicy.ForSelfHosted();

        policy1.Should().Be(policy2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var policy1 = AuthenticationPolicy.ForSaaS("AzureAD");
        var policy2 = AuthenticationPolicy.ForSelfHosted();

        policy1.Should().NotBe(policy2);
    }

    [Fact]
    public void ToString_ContainsModeAndTimeout()
    {
        var policy = AuthenticationPolicy.ForSaaS("Okta");

        policy.ToString().Should().Contain("Federated");
        policy.ToString().Should().Contain("30");
    }
}
