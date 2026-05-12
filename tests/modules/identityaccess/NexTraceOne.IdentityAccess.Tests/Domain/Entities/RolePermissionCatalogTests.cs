using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

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
        permissions.Should().Contain("identity:jit-access:decide");
        permissions.Should().Contain("audit:trail:read");
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
        permissions.Should().Contain("audit:trail:read");
        permissions.Should().Contain("audit:compliance:read");
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
        approvalPermissions.Should().Contain("promotion:requests:write");
        approvalPermissions.Should().Contain("change-intelligence:read");
    }

    // ── Testes de permissões do Developer Portal ──────────────────────────

    [Fact]
    public void GetPermissionsForRole_Should_IncludeDeveloperPortalRead_When_Developer()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);

        // Assert — Developer acessa o portal para consumir APIs e gerenciar subscrições
        permissions.Should().Contain("developer-portal:read");
        permissions.Should().Contain("developer-portal:write");
    }

    [Fact]
    public void GetPermissionsForRole_Should_IncludeDeveloperPortalReadOnly_When_Viewer()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.Viewer);

        // Assert — Viewer pode apenas consultar o portal, sem criar subscrições ou executar playground
        permissions.Should().Contain("developer-portal:read");
        permissions.Should().NotContain("developer-portal:write");
    }

    [Fact]
    public void GetPermissionsForRole_Should_IncludeDeveloperPortalReadOnly_When_Auditor()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.Auditor);

        // Assert — Auditor consulta o portal para verificação, mas não cria dados
        permissions.Should().Contain("developer-portal:read");
        permissions.Should().NotContain("developer-portal:write");
    }

    [Fact]
    public void GetPermissionsForRole_Should_NotIncludeDeveloperPortal_When_ApprovalOnly()
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(Role.ApprovalOnly);

        // Assert — ApprovalOnly é focado em aprovação de workflow, sem acesso ao portal
        permissions.Should().NotContain("developer-portal:read");
        permissions.Should().NotContain("developer-portal:write");
    }

    // ── Testes de permissões do Engineering Graph ─────────────────────────

    [Fact]
    public void GetPermissionsForRole_Should_IncludeGraphWrite_When_AdminOrTechLead()
    {
        // Act
        var adminPerms = RolePermissionCatalog.GetPermissionsForRole(Role.PlatformAdmin);
        var techLeadPerms = RolePermissionCatalog.GetPermissionsForRole(Role.TechLead);
        var developerPerms = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);
        var viewerPerms = RolePermissionCatalog.GetPermissionsForRole(Role.Viewer);

        // Assert — escrita no grafo limitada a PlatformAdmin e TechLead
        adminPerms.Should().Contain("catalog:assets:write");
        techLeadPerms.Should().Contain("catalog:assets:write");
        developerPerms.Should().NotContain("catalog:assets:write");
        viewerPerms.Should().NotContain("catalog:assets:write");
    }

    [Fact]
    public void GetPermissionsForRole_Should_IncludeGraphRead_When_AnyNonApprovalRole()
    {
        // Assert — todos os papéis exceto ApprovalOnly devem ler o grafo
        var rolesWithGraphRead = new[]
        {
            Role.PlatformAdmin, Role.TechLead, Role.Developer,
            Role.Viewer, Role.Auditor, Role.SecurityReview
        };

        foreach (var role in rolesWithGraphRead)
        {
            var permissions = RolePermissionCatalog.GetPermissionsForRole(role);
            permissions.Should().Contain("catalog:assets:read",
                because: $"'{role}' deve ter leitura do grafo para visualização de dependências");
        }
    }

    // ── Testes de permissões de Contracts ──────────────────────────────────

    [Fact]
    public void GetPermissionsForRole_Should_IncludeContractsImport_When_DeveloperOrAbove()
    {
        // Act
        var adminPerms = RolePermissionCatalog.GetPermissionsForRole(Role.PlatformAdmin);
        var techLeadPerms = RolePermissionCatalog.GetPermissionsForRole(Role.TechLead);
        var developerPerms = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);
        var viewerPerms = RolePermissionCatalog.GetPermissionsForRole(Role.Viewer);

        // Assert — importação de contratos requer ao menos papel Developer
        adminPerms.Should().Contain("contracts:import");
        techLeadPerms.Should().Contain("contracts:import");
        developerPerms.Should().Contain("contracts:import");
        viewerPerms.Should().NotContain("contracts:import");
    }

    // ── Teste de hierarquia geral de permissões ───────────────────────────

    [Theory]
    [InlineData(Role.PlatformAdmin)]
    [InlineData(Role.TechLead)]
    [InlineData(Role.Developer)]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Auditor)]
    [InlineData(Role.SecurityReview)]
    [InlineData(Role.ApprovalOnly)]
    public void GetPermissionsForRole_Should_ReturnNonNull_ForAllKnownRoles(string roleName)
    {
        // Act
        var permissions = RolePermissionCatalog.GetPermissionsForRole(roleName);

        // Assert — nenhum papel válido retorna null
        permissions.Should().NotBeNull();
        permissions.Should().NotBeEmpty(
            because: $"papel '{roleName}' deve ter ao menos uma permissão");
    }
}

