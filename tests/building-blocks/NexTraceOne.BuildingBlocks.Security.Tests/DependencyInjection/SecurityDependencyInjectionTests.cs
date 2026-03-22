using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.DependencyInjection;

public sealed class SecurityDependencyInjectionTests : IDisposable
{
    private const string TestSigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-validation!!";

    private readonly string? _originalEnvironment;

    public SecurityDependencyInjectionTests()
    {
        _originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnvironment);
    }

    private static IConfiguration CreateConfiguration(
        string? signingKey = TestSigningKey,
        string? issuer = null,
        string? audience = null)
    {
        var configData = new Dictionary<string, string?>();
        if (signingKey is not null) configData["Jwt:Secret"] = signingKey;
        if (issuer is not null) configData["Jwt:Issuer"] = issuer;
        if (audience is not null) configData["Jwt:Audience"] = audience;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersCurrentTenant()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();

        services.AddBuildingBlocksSecurity(configuration);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tenant = scope.ServiceProvider.GetService<ICurrentTenant>();

        tenant.Should().NotBeNull();
        tenant.Should().BeOfType<CurrentTenantAccessor>();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersCurrentUser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();

        services.AddBuildingBlocksSecurity(configuration);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var user = scope.ServiceProvider.GetService<ICurrentUser>();

        user.Should().NotBeNull();
        user.Should().BeOfType<HttpContextCurrentUser>();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersJwtTokenService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Substitute_IDateTimeProvider());

        services.AddBuildingBlocksSecurity(configuration);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var jwtService = scope.ServiceProvider.GetService<JwtTokenService>();

        jwtService.Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersPermissionPolicyProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();

        services.AddBuildingBlocksSecurity(configuration);

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetService<IAuthorizationPolicyProvider>();

        policyProvider.Should().NotBeNull();
        policyProvider.Should().BeOfType<PermissionPolicyProvider>();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersPermissionAuthorizationHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();

        services.AddBuildingBlocksSecurity(configuration);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IAuthorizationHandler>();

        handlers.Should().Contain(h => h is PermissionAuthorizationHandler);
    }

    [Fact]
    public void AddBuildingBlocksSecurity_InDevelopment_WithNoKey_UsesFallback()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(signingKey: null);

        var act = () => services.AddBuildingBlocksSecurity(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_InProduction_WithNoKey_Throws()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(signingKey: null);

        var act = () => services.AddBuildingBlocksSecurity(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT signing key*");
    }

    [Fact]
    public void AddBuildingBlocksSecurity_UsesConfiguredIssuerAndAudience()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(issuer: "CustomIssuer", audience: "CustomAudience");

        // Should not throw — verifying configuration is accepted
        var act = () => services.AddBuildingBlocksSecurity(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_RegistersCurrentTenantAccessorAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration();

        services.AddBuildingBlocksSecurity(configuration);

        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(CurrentTenantAccessor));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    private static IDateTimeProvider Substitute_IDateTimeProvider()
    {
        var provider = NSubstitute.Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(DateTimeOffset.UtcNow);
        return provider;
    }
}
