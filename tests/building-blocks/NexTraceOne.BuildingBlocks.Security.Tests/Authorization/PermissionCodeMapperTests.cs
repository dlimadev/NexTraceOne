using FluentAssertions;
using NexTraceOne.BuildingBlocks.Security.Authorization;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authorization;

public sealed class PermissionCodeMapperTests
{
    // ── Mapeamento de 3 partes (módulo:recurso:ação) ─────────────────────

    [Theory]
    [InlineData("ai:runtime:write", "AI", "Runtime", "Write")]
    [InlineData("ai:runtime:read", "AI", "Runtime", "Read")]
    [InlineData("ai:assistant:write", "AI", "Assistant", "Write")]
    [InlineData("ai:governance:read", "AI", "Governance", "Read")]
    [InlineData("identity:users:read", "Identity", "Users", "Read")]
    [InlineData("identity:users:write", "Identity", "Users", "Write")]
    [InlineData("identity:roles:read", "Identity", "Roles", "Read")]
    [InlineData("identity:sessions:revoke", "Identity", "Sessions", "Revoke")]
    [InlineData("catalog:assets:read", "Catalog", "Assets", "Read")]
    [InlineData("catalog:assets:write", "Catalog", "Assets", "Write")]
    [InlineData("operations:incidents:read", "Operations", "Incidents", "Read")]
    [InlineData("operations:runbooks:write", "Operations", "Runbooks", "Write")]
    [InlineData("operations:reliability:read", "Operations", "Reliability", "Read")]
    [InlineData("governance:domains:read", "Governance", "Domains", "Read")]
    [InlineData("governance:teams:write", "Governance", "Teams", "Write")]
    [InlineData("governance:compliance:read", "Governance", "Compliance", "Read")]
    [InlineData("notifications:inbox:read", "Notifications", "Inbox", "Read")]
    [InlineData("notifications:preferences:write", "Notifications", "Preferences", "Write")]
    [InlineData("audit:trail:read", "Audit", "Trail", "Read")]
    [InlineData("audit:compliance:write", "Audit", "Compliance", "Write")]
    [InlineData("workflow:instances:read", "Workflow", "Instances", "Read")]
    [InlineData("workflow:templates:write", "Workflow", "Templates", "Write")]
    [InlineData("promotion:requests:read", "Promotion", "Requests", "Read")]
    [InlineData("promotion:gates:override", "Promotion", "Gates", "Override")]
    [InlineData("env:environments:read", "Environments", "Environments", "Read")]
    [InlineData("env:access:admin", "Environments", "Access", "Admin")]
    [InlineData("platform:settings:write", "Platform", "Settings", "Write")]
    public void Map_ThreeParts_ReturnsCorrectModulePageAction(
        string permissionCode, string expectedModule, string expectedPage, string expectedAction)
    {
        var result = PermissionCodeMapper.Map(permissionCode);

        result.Should().NotBeNull();
        result!.Module.Should().Be(expectedModule);
        result.Page.Should().Be(expectedPage);
        result.Action.Should().Be(expectedAction);
    }

    // ── Mapeamento de 2 partes (módulo:ação → Page="*") ─────────────────

    [Theory]
    [InlineData("contracts:read", "Contracts", "*", "Read")]
    [InlineData("contracts:write", "Contracts", "*", "Write")]
    [InlineData("contracts:import", "Contracts", "*", "Import")]
    [InlineData("integrations:read", "Integrations", "*", "Read")]
    [InlineData("integrations:write", "Integrations", "*", "Write")]
    [InlineData("configuration:read", "Configuration", "*", "Read")]
    [InlineData("configuration:write", "Configuration", "*", "Write")]
    [InlineData("analytics:read", "Governance", "*", "Read")]
    [InlineData("analytics:write", "Governance", "*", "Write")]
    [InlineData("rulesets:read", "Governance", "*", "Read")]
    [InlineData("rulesets:execute", "Governance", "*", "Execute")]
    public void Map_TwoParts_ReturnsModuleWithWildcardPage(
        string permissionCode, string expectedModule, string expectedPage, string expectedAction)
    {
        var result = PermissionCodeMapper.Map(permissionCode);

        result.Should().NotBeNull();
        result!.Module.Should().Be(expectedModule);
        result.Page.Should().Be(expectedPage);
        result.Action.Should().Be(expectedAction);
    }

    // ── Mapeamento de prefixos com kebab-case ────────────────────────────

    [Theory]
    [InlineData("developer-portal:read", "DeveloperPortal", "*", "Read")]
    [InlineData("developer-portal:write", "DeveloperPortal", "*", "Write")]
    [InlineData("change-intelligence:read", "ChangeIntelligence", "*", "Read")]
    [InlineData("change-intelligence:write", "ChangeIntelligence", "*", "Write")]
    public void Map_KebabCasePrefix_ReturnsCorrectPascalCaseModule(
        string permissionCode, string expectedModule, string expectedPage, string expectedAction)
    {
        var result = PermissionCodeMapper.Map(permissionCode);

        result.Should().NotBeNull();
        result!.Module.Should().Be(expectedModule);
        result.Page.Should().Be(expectedPage);
        result.Action.Should().Be(expectedAction);
    }

    // ── Recursos com kebab-case ──────────────────────────────────────────

    [Theory]
    [InlineData("identity:jit-access:decide", "Identity", "JitAccess", "Decide")]
    [InlineData("identity:break-glass:decide", "Identity", "BreakGlass", "Decide")]
    public void Map_KebabCaseResource_ReturnsCorrectPascalCasePage(
        string permissionCode, string expectedModule, string expectedPage, string expectedAction)
    {
        var result = PermissionCodeMapper.Map(permissionCode);

        result.Should().NotBeNull();
        result!.Module.Should().Be(expectedModule);
        result.Page.Should().Be(expectedPage);
        result.Action.Should().Be(expectedAction);
    }

    // ── Códigos inválidos ou vazios ──────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("single")]
    [InlineData("a:b:c:d")]
    public void Map_InvalidCode_ReturnsNull(string permissionCode)
    {
        var result = PermissionCodeMapper.Map(permissionCode);

        result.Should().BeNull();
    }

    [Fact]
    public void Map_NullCode_ReturnsNull()
    {
        var result = PermissionCodeMapper.Map(null!);

        result.Should().BeNull();
    }

    // ── CanMap ────────────────────────────────────────────────────────────

    [Fact]
    public void CanMap_ValidCode_ReturnsTrue()
    {
        PermissionCodeMapper.CanMap("ai:runtime:write").Should().BeTrue();
    }

    [Fact]
    public void CanMap_InvalidCode_ReturnsFalse()
    {
        PermissionCodeMapper.CanMap("invalid").Should().BeFalse();
    }

    // ── GetKnownModulePrefixes ───────────────────────────────────────────

    [Fact]
    public void GetKnownModulePrefixes_ContainsExpectedPrefixes()
    {
        var prefixes = PermissionCodeMapper.GetKnownModulePrefixes();

        prefixes.Should().Contain("identity");
        prefixes.Should().Contain("ai");
        prefixes.Should().Contain("catalog");
        prefixes.Should().Contain("operations");
        prefixes.Should().Contain("governance");
        prefixes.Should().Contain("env");
        prefixes.Should().Contain("change-intelligence");
        prefixes.Should().Contain("developer-portal");
    }
}
