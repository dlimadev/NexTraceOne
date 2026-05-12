using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Infrastructure.Services;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Services;

/// <summary>
/// Testes do PermissionResolver — validam resolução DB-first com fallback estático
/// e resolução multi-role com UNIÃO de permissões.
/// </summary>
public sealed class PermissionResolverTests
{
    private readonly IRolePermissionRepository _rolePermissionRepo = Substitute.For<IRolePermissionRepository>();
    private readonly PermissionResolver _sut;

    private static readonly RoleId DeveloperRoleId = RoleId.From(Guid.NewGuid());
    private static readonly RoleId AuditorRoleId = RoleId.From(Guid.NewGuid());
    private static readonly TenantId TestTenantId = TenantId.From(Guid.NewGuid());

    public PermissionResolverTests()
    {
        _sut = new PermissionResolver(_rolePermissionRepo);
    }

    // ── Single-role (existing behavior) ───────────────────────────────────

    [Fact]
    public async Task ResolvePermissionsAsync_Should_ReturnDbPermissions_WhenMappingsExist()
    {
        var dbPermissions = new List<string> { "contracts:read", "contracts:write" };
        _rolePermissionRepo.HasMappingsForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(true);
        _rolePermissionRepo.GetPermissionCodesForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(dbPermissions);

        var result = await _sut.ResolvePermissionsAsync(
            DeveloperRoleId, Role.Developer, TestTenantId, CancellationToken.None);

        result.Should().BeEquivalentTo(dbPermissions);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_Should_FallbackToStaticCatalog_WhenNoDbMappings()
    {
        _rolePermissionRepo.HasMappingsForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.ResolvePermissionsAsync(
            DeveloperRoleId, Role.Developer, TestTenantId, CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(RolePermissionCatalog.GetPermissionsForRole(Role.Developer));
    }

    // ── Multi-role resolution ─────────────────────────────────────────────

    [Fact]
    public async Task ResolvePermissionsForMultipleRolesAsync_Should_ReturnEmpty_WhenNoRoles()
    {
        var result = await _sut.ResolvePermissionsForMultipleRolesAsync(
            Array.Empty<(RoleId, string)>(), TestTenantId, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolvePermissionsForMultipleRolesAsync_Should_DelegateSingleRole()
    {
        _rolePermissionRepo.HasMappingsForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var roles = new List<(RoleId, string)> { (DeveloperRoleId, Role.Developer) };

        var result = await _sut.ResolvePermissionsForMultipleRolesAsync(
            roles, TestTenantId, CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(RolePermissionCatalog.GetPermissionsForRole(Role.Developer));
    }

    [Fact]
    public async Task ResolvePermissionsForMultipleRolesAsync_Should_ReturnUnion_OfMultipleRoles()
    {
        // Both roles use static catalog fallback.
        _rolePermissionRepo.HasMappingsForRoleAsync(Arg.Any<RoleId>(), TestTenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var roles = new List<(RoleId, string)>
        {
            (DeveloperRoleId, Role.Developer),
            (AuditorRoleId, Role.Auditor)
        };

        var result = await _sut.ResolvePermissionsForMultipleRolesAsync(
            roles, TestTenantId, CancellationToken.None);

        var expectedDev = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);
        var expectedAudit = RolePermissionCatalog.GetPermissionsForRole(Role.Auditor);
        var expectedUnion = expectedDev.Union(expectedAudit).Distinct().OrderBy(x => x).ToList();

        result.Should().BeEquivalentTo(expectedUnion);
    }

    [Fact]
    public async Task ResolvePermissionsForMultipleRolesAsync_Should_DedupPermissions()
    {
        // Developer has some permissions, set DB to return overlapping ones.
        var devPermissions = new List<string> { "contracts:read", "contracts:write", "shared:perm" };
        var auditPermissions = new List<string> { "audit:trail:read", "audit:reports:read", "shared:perm" };

        _rolePermissionRepo.HasMappingsForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(true);
        _rolePermissionRepo.GetPermissionCodesForRoleAsync(DeveloperRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(devPermissions);

        _rolePermissionRepo.HasMappingsForRoleAsync(AuditorRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(true);
        _rolePermissionRepo.GetPermissionCodesForRoleAsync(AuditorRoleId, TestTenantId, Arg.Any<CancellationToken>())
            .Returns(auditPermissions);

        var roles = new List<(RoleId, string)>
        {
            (DeveloperRoleId, Role.Developer),
            (AuditorRoleId, Role.Auditor)
        };

        var result = await _sut.ResolvePermissionsForMultipleRolesAsync(
            roles, TestTenantId, CancellationToken.None);

        // "shared:perm" should appear only once.
        result.Count(x => x == "shared:perm").Should().Be(1);
        result.Should().HaveCount(5); // 3 + 3 - 1 overlap = 5.
    }

    [Fact]
    public async Task ResolvePermissionsForMultipleRolesAsync_Should_ReturnSortedResults()
    {
        _rolePermissionRepo.HasMappingsForRoleAsync(Arg.Any<RoleId>(), TestTenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var roles = new List<(RoleId, string)>
        {
            (DeveloperRoleId, Role.Developer),
            (AuditorRoleId, Role.Auditor)
        };

        var result = await _sut.ResolvePermissionsForMultipleRolesAsync(
            roles, TestTenantId, CancellationToken.None);

        result.Should().BeInAscendingOrder();
    }
}

