using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade ModuleAccessPolicy.
/// Valida criação, matching com wildcard, atualização e ativação/desativação.
/// </summary>
public sealed class ModuleAccessPolicyTests
{
    private static readonly DateTimeOffset Now = new(2025, 8, 20, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_SetPropertiesCorrectly_When_ValidInput()
    {
        var roleId = RoleId.New();
        var policy = ModuleAccessPolicy.Create(
            roleId,
            null,
            "Contracts",
            "ContractStudio",
            "Import",
            isAllowed: true,
            Now,
            "admin@example.com");

        policy.RoleId.Should().Be(roleId);
        policy.TenantId.Should().BeNull();
        policy.Module.Should().Be("Contracts");
        policy.Page.Should().Be("ContractStudio");
        policy.Action.Should().Be("Import");
        policy.IsAllowed.Should().BeTrue();
        policy.IsActive.Should().BeTrue();
        policy.CreatedAt.Should().Be(Now);
        policy.CreatedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void Create_Should_SetTenantId_When_TenantSpecific()
    {
        var tenantId = TenantId.New();
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(),
            tenantId,
            "Operations",
            "Incidents",
            "Delete",
            isAllowed: false,
            Now,
            null);

        policy.TenantId.Should().Be(tenantId);
        policy.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Matches_Should_ReturnTrue_When_ExactMatch()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Catalog", "ServiceCatalog", "Create", true, Now, null);

        policy.Matches("Catalog", "ServiceCatalog", "Create").Should().BeTrue();
    }

    [Fact]
    public void Matches_Should_ReturnFalse_When_ModuleDiffers()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Catalog", "ServiceCatalog", "Create", true, Now, null);

        policy.Matches("Operations", "ServiceCatalog", "Create").Should().BeFalse();
    }

    [Fact]
    public void Matches_Should_ReturnTrue_When_PageIsWildcard()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Catalog", "*", "Read", true, Now, null);

        policy.Matches("Catalog", "ServiceCatalog", "Read").Should().BeTrue();
        policy.Matches("Catalog", "ContractStudio", "Read").Should().BeTrue();
    }

    [Fact]
    public void Matches_Should_ReturnTrue_When_ActionIsWildcard()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Catalog", "ServiceCatalog", "*", true, Now, null);

        policy.Matches("Catalog", "ServiceCatalog", "Read").Should().BeTrue();
        policy.Matches("Catalog", "ServiceCatalog", "Write").Should().BeTrue();
        policy.Matches("Catalog", "ServiceCatalog", "Delete").Should().BeTrue();
    }

    [Fact]
    public void Matches_Should_ReturnTrue_When_BothPageAndActionAreWildcard()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Contracts", "*", "*", true, Now, null);

        policy.Matches("Contracts", "AnyPage", "AnyAction").Should().BeTrue();
    }

    [Fact]
    public void Matches_Should_BeCaseInsensitive()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Catalog", "ServiceCatalog", "Read", true, Now, null);

        policy.Matches("catalog", "servicecatalog", "read").Should().BeTrue();
        policy.Matches("CATALOG", "SERVICECATALOG", "READ").Should().BeTrue();
    }

    [Fact]
    public void UpdateAccess_Should_ChangeDecision_When_Called()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Identity", "Users", "Delete", true, Now, null);
        var later = Now.AddHours(2);

        policy.UpdateAccess(isAllowed: false, later, "security-admin");

        policy.IsAllowed.Should().BeFalse();
        policy.UpdatedAt.Should().Be(later);
        policy.UpdatedBy.Should().Be("security-admin");
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse_When_Called()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "Operations", "Runbooks", "Execute", true, Now, null);
        var later = Now.AddHours(1);

        policy.Deactivate(later, "admin");

        policy.IsActive.Should().BeFalse();
        policy.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrue_When_PreviouslyDeactivated()
    {
        var policy = ModuleAccessPolicy.Create(
            RoleId.New(), null, "AI", "Assistant", "Write", true, Now, null);
        policy.Deactivate(Now.AddHours(1), "admin");

        policy.Activate(Now.AddHours(2), "admin");

        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_ModuleIsEmpty()
    {
        var act = () => ModuleAccessPolicy.Create(
            RoleId.New(), null, "", "Page", "Action", true, Now, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_PageIsEmpty()
    {
        var act = () => ModuleAccessPolicy.Create(
            RoleId.New(), null, "Module", "", "Action", true, Now, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_ActionIsEmpty()
    {
        var act = () => ModuleAccessPolicy.Create(
            RoleId.New(), null, "Module", "Page", "", true, Now, null);

        act.Should().Throw<ArgumentException>();
    }
}
