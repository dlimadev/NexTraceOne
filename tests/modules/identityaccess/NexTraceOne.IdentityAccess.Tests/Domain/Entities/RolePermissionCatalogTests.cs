using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="RolePermissionCatalog"/>.
/// Valida que cada papel do sistema recebe o conjunto correto de permissões,
/// garantindo que a política de autorização baseada em papéis está consistente.
/// </summary>
public sealed class RolePermissionCatalogTests
{
    [Fact]
    public void GetPermissionsForRole_Should_ReturnPermissions_When_PlatformAdmin()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.PlatformAdmin);

        // Assert — PlatformAdmin possui o maior conjunto de permissões
        permissions.Should().NotBeEmpty();
        permissions.Should().Contain("identity:users:write");
        permissions.Should().Contain("platform:settings:write");
        permissions.Should().Contain("licensing:write");
        permissions.Should().Contain("audit:export");
    }

    [Fact]
    public void GetPermissionsForRole_Should_ReturnSubsetOfAdmin_When_Developer()
    {
        // Arrange
        var adminPermissions = RolePermissionCatalog.GetPermissionsForRole(Role.PlatformAdmin);
        var developerPermissions = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);

        // Assert — Developer tem menos permissões que PlatformAdmin e é um subconjunto
        developerPermissions.Count.Should().BeLessThan(adminPermissions.Count);
        developerPermissions.Should().OnlyContain(p => adminPermissions.Contains(p));
    }

    [Fact]
    public void GetPermissionsForRole_Should_ReturnEmpty_When_UnknownRole()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole("NonExistentRole");

        // Assert
        permissions.Should().BeEmpty();
    }

    [Fact]
    public void GetPermissionsForRole_Should_IncludeAuditRead_When_Auditor()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.Auditor);

        // Assert — Auditor deve ter acesso de leitura a auditoria e exportação
        permissions.Should().Contain("audit:read");
        permissions.Should().Contain("audit:export");
        permissions.Should().NotContain("identity:users:write");
        permissions.Should().NotContain("platform:settings:write");
    }

    [Fact]
    public void GetPermissionsForRole_Should_ReturnFewerPermissions_When_ApprovalOnly()
    {
        // Arrange
        var developerPermissions = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);
        var approvalPermissions = RolePermissionCatalog.GetPermissionsForRole(Role.ApprovalOnly);

        // Assert — ApprovalOnly é o papel mais restrito com permissões focadas em aprovação
        approvalPermissions.Count.Should().BeLessThan(developerPermissions.Count);
        approvalPermissions.Should().Contain("workflow:approve");
        approvalPermissions.Should().Contain("workflow:read");
    }
}
