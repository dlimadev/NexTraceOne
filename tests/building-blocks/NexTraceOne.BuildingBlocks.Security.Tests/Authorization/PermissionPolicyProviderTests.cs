using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Security.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authorization;

public sealed class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider CreateProvider()
    {
        var options = Options.Create(new AuthorizationOptions());
        return new PermissionPolicyProvider(options);
    }

    [Fact]
    public async Task GetPolicyAsync_WithPermissionPrefix_ReturnsPermissionPolicy()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("Permission:services:read");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<PermissionRequirement>()
            .Which.Permission.Should().Be("services:read");
    }

    [Fact]
    public async Task GetPolicyAsync_WithComplexPermission_ExtractsFullPermissionCode()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("Permission:identity:users:write");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<PermissionRequirement>()
            .Which.Permission.Should().Be("identity:users:write");
    }

    [Fact]
    public async Task GetPolicyAsync_WithNonPermissionPolicy_DelegatesToFallback()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("SomeOtherPolicy");

        // Fallback provider returns null for unknown policies
        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_ReturnsDefaultPolicy()
    {
        var provider = CreateProvider();

        var policy = await provider.GetDefaultPolicyAsync();

        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_ReturnsNull()
    {
        var provider = CreateProvider();

        var policy = await provider.GetFallbackPolicyAsync();

        // Default fallback policy is null unless explicitly configured
        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_IsCaseInsensitiveOnPrefix()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("permission:services:read");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<PermissionRequirement>();
    }

    // ── Testes do novo prefixo ModuleAccess ──────────────────────────────

    [Fact]
    public async Task GetPolicyAsync_WithModuleAccessPrefix_ReturnsModuleAccessPolicy()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("ModuleAccess:AI:Runtime:Write");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<ModuleAccessRequirement>();

        var requirement = (ModuleAccessRequirement)policy.Requirements.Single();
        requirement.Module.Should().Be("AI");
        requirement.Page.Should().Be("Runtime");
        requirement.Action.Should().Be("Write");
    }

    [Fact]
    public async Task GetPolicyAsync_WithModuleAccessWildcard_ReturnsCorrectRequirement()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("ModuleAccess:Catalog:*:Read");

        policy.Should().NotBeNull();
        var requirement = (ModuleAccessRequirement)policy!.Requirements.Single();
        requirement.Module.Should().Be("Catalog");
        requirement.Page.Should().Be("*");
        requirement.Action.Should().Be("Read");
    }

    [Fact]
    public async Task GetPolicyAsync_WithInvalidModuleAccessFormat_DelegatesToFallback()
    {
        var provider = CreateProvider();

        // Only 2 parts instead of 3
        var policy = await provider.GetPolicyAsync("ModuleAccess:AI:Runtime");

        policy.Should().BeNull();
    }
}
