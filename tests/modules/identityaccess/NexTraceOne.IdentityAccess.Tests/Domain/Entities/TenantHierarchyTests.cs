using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes da hierarquia organizacional do Tenant — validam criação com tipo,
/// hierarquia (parent/child), dados organizacionais e regras de validação.
/// </summary>
public sealed class TenantHierarchyTests
{
    private static readonly DateTimeOffset Now = new(2026, 03, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Backward Compatibility ────────────────────────────────────────────

    [Fact]
    public void Create_Should_DefaultToOrganizationType()
    {
        var tenant = Tenant.Create("Corp ABC", "corp-abc", Now);

        tenant.TenantType.Should().Be(TenantType.Organization);
        tenant.ParentTenantId.Should().BeNull();
        tenant.LegalName.Should().BeNull();
        tenant.TaxId.Should().BeNull();
        tenant.IsRoot.Should().BeTrue();
    }

    // ── CreateWithHierarchy — Root tenants ────────────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_CreateHolding_WithNoParent()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Grupo XYZ", "grupo-xyz", TenantType.Holding, Now,
            legalName: "Grupo XYZ S.A.", taxId: "12.345.678/0001-90");

        tenant.Name.Should().Be("Grupo XYZ");
        tenant.Slug.Should().Be("grupo-xyz");
        tenant.TenantType.Should().Be(TenantType.Holding);
        tenant.ParentTenantId.Should().BeNull();
        tenant.LegalName.Should().Be("Grupo XYZ S.A.");
        tenant.TaxId.Should().Be("12.345.678/0001-90");
        tenant.IsActive.Should().BeTrue();
        tenant.IsRoot.Should().BeTrue();
    }

    [Fact]
    public void CreateWithHierarchy_Should_CreateOrganization_WithNoParent()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Empresa Simples", "empresa-simples", TenantType.Organization, Now);

        tenant.TenantType.Should().Be(TenantType.Organization);
        tenant.ParentTenantId.Should().BeNull();
        tenant.IsRoot.Should().BeTrue();
    }

    [Fact]
    public void CreateWithHierarchy_Should_CreatePartner_WithoutParent()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Parceiro SaaS", "parceiro-saas", TenantType.Partner, Now);

        tenant.TenantType.Should().Be(TenantType.Partner);
        tenant.ParentTenantId.Should().BeNull();
        tenant.IsRoot.Should().BeTrue();
    }

    // ── CreateWithHierarchy — Child tenants ───────────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_CreateSubsidiary_WithParent()
    {
        var parentId = TenantId.From(Guid.NewGuid());

        var subsidiary = Tenant.CreateWithHierarchy(
            "Banco XYZ", "banco-xyz", TenantType.Subsidiary, Now,
            parentTenantId: parentId,
            legalName: "Banco XYZ S.A.",
            taxId: "11.222.333/0001-44");

        subsidiary.TenantType.Should().Be(TenantType.Subsidiary);
        subsidiary.ParentTenantId.Should().Be(parentId);
        subsidiary.LegalName.Should().Be("Banco XYZ S.A.");
        subsidiary.TaxId.Should().Be("11.222.333/0001-44");
        subsidiary.IsRoot.Should().BeFalse();
    }

    [Fact]
    public void CreateWithHierarchy_Should_CreateDepartment_WithParent()
    {
        var parentId = TenantId.From(Guid.NewGuid());

        var department = Tenant.CreateWithHierarchy(
            "TI - Infraestrutura", "ti-infra", TenantType.Department, Now,
            parentTenantId: parentId);

        department.TenantType.Should().Be(TenantType.Department);
        department.ParentTenantId.Should().Be(parentId);
        department.IsRoot.Should().BeFalse();
    }

    // ── Validation: Root types cannot have parent ─────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenHolding_HasParent()
    {
        var parentId = TenantId.From(Guid.NewGuid());

        var act = () => Tenant.CreateWithHierarchy(
            "Holding", "holding", TenantType.Holding, Now,
            parentTenantId: parentId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Holding*root*");
    }

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenOrganization_HasParent()
    {
        var parentId = TenantId.From(Guid.NewGuid());

        var act = () => Tenant.CreateWithHierarchy(
            "Org", "org", TenantType.Organization, Now,
            parentTenantId: parentId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Organization*root*");
    }

    // ── Validation: Child types require parent ────────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenSubsidiary_HasNoParent()
    {
        var act = () => Tenant.CreateWithHierarchy(
            "Filial", "filial", TenantType.Subsidiary, Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Subsidiary*requires*parent*");
    }

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenDepartment_HasNoParent()
    {
        var act = () => Tenant.CreateWithHierarchy(
            "Dept", "dept", TenantType.Department, Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Department*requires*parent*");
    }

    // ── UpdateOrganizationInfo ────────────────────────────────────────────

    [Fact]
    public void UpdateOrganizationInfo_Should_ChangeFields()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Corp", "corp", TenantType.Organization, Now);
        var later = Now.AddDays(1);

        tenant.UpdateOrganizationInfo("Corp S.A.", "99.888.777/0001-66", later);

        tenant.LegalName.Should().Be("Corp S.A.");
        tenant.TaxId.Should().Be("99.888.777/0001-66");
        tenant.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateOrganizationInfo_Should_AllowNullValues()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Corp", "corp", TenantType.Organization, Now,
            legalName: "Old Name", taxId: "00.000.000/0001-00");
        var later = Now.AddDays(1);

        tenant.UpdateOrganizationInfo(null, null, later);

        tenant.LegalName.Should().BeNull();
        tenant.TaxId.Should().BeNull();
    }

    // ── Slug normalization ────────────────────────────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_NormalizeSlugToLowerCase()
    {
        var tenant = Tenant.CreateWithHierarchy(
            "Corp", "MY-SLUG", TenantType.Organization, Now);

        tenant.Slug.Should().Be("my-slug");
    }

    // ── Basic validation ──────────────────────────────────────────────────

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenNameIsEmpty()
    {
        var act = () => Tenant.CreateWithHierarchy(
            "", "slug", TenantType.Organization, Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithHierarchy_Should_ThrowWhenSlugIsEmpty()
    {
        var act = () => Tenant.CreateWithHierarchy(
            "Name", "", TenantType.Organization, Now);
        act.Should().Throw<ArgumentException>();
    }
}

