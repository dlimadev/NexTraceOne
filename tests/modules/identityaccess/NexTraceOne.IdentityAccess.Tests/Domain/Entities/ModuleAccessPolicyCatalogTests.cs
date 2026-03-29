using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ModuleAccessPolicyCatalog"/>.
/// Valida que cada papel recebe o conjunto correto de políticas de acesso
/// ao nível de módulo/página/ação, garantindo consistência com a visão
/// enterprise do produto NexTraceOne.
/// </summary>
public sealed class ModuleAccessPolicyCatalogTests
{
    // ── Testes de estrutura geral ──────────────────────────────────────────

    [Theory]
    [InlineData(Role.PlatformAdmin)]
    [InlineData(Role.TechLead)]
    [InlineData(Role.Developer)]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Auditor)]
    [InlineData(Role.SecurityReview)]
    [InlineData(Role.ApprovalOnly)]
    public void GetPoliciesForRole_Should_ReturnNonEmpty_ForAllKnownRoles(string roleName)
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(roleName);

        policies.Should().NotBeNull();
        policies.Should().NotBeEmpty(
            because: $"papel '{roleName}' deve ter ao menos uma política de acesso a módulo");
    }

    [Fact]
    public void GetPoliciesForRole_Should_ReturnEmpty_When_UnknownRole()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole("NonExistentRole");

        policies.Should().BeEmpty();
    }

    // ── PlatformAdmin ──────────────────────────────────────────────────────

    [Fact]
    public void PlatformAdmin_Should_HaveWildcardAccess_ForAllModules()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.PlatformAdmin);
        var modules = ModuleAccessPolicyCatalog.GetAllModules();

        foreach (var module in modules)
        {
            policies.Should().Contain(p => p.Module == module && p.Page == "*" && p.Action == "*" && p.IsAllowed,
                because: $"PlatformAdmin deve ter acesso wildcard ao módulo '{module}'");
        }
    }

    [Fact]
    public void PlatformAdmin_Should_CoverAllModules()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.PlatformAdmin);
        var modules = ModuleAccessPolicyCatalog.GetAllModules();

        policies.Count.Should().Be(modules.Count,
            because: "PlatformAdmin deve ter exatamente uma política wildcard por módulo");
    }

    // ── TechLead ───────────────────────────────────────────────────────────

    [Fact]
    public void TechLead_Should_HaveFullAccessToContracts()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.TechLead);

        policies.Should().Contain(p => p.Module == "Contracts" && p.Page == "*" && p.Action == "*" && p.IsAllowed);
    }

    [Fact]
    public void TechLead_Should_HaveReadOnlyReliability()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.TechLead);

        policies.Should().Contain(p => p.Module == "Operations" && p.Page == "Reliability" && p.Action == "Read" && p.IsAllowed);
    }

    [Fact]
    public void TechLead_Should_NotHaveFullPlatformAccess()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.TechLead);

        policies.Should().NotContain(p => p.Module == "Platform" && p.Page == "*" && p.Action == "*");
    }

    // ── Developer ──────────────────────────────────────────────────────────

    [Fact]
    public void Developer_Should_HaveFullContractAccess()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Developer);

        policies.Should().Contain(p => p.Module == "Contracts" && p.Page == "*" && p.Action == "*" && p.IsAllowed);
    }

    [Fact]
    public void Developer_Should_HaveReadOnlyCatalog()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Developer);

        policies.Should().Contain(p => p.Module == "Catalog" && p.Page == "*" && p.Action == "Read");
    }

    [Fact]
    public void Developer_Should_NotHaveGovernanceAdmin()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Developer);

        policies.Should().NotContain(p => p.Module == "Governance" && p.Action == "Write");
    }

    // ── Viewer ─────────────────────────────────────────────────────────────

    [Fact]
    public void Viewer_Should_HaveOnlyReadActions()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Viewer);

        policies.Should().OnlyContain(p => p.Action == "Read" && p.IsAllowed,
            because: "Viewer deve ter apenas ações de leitura");
    }

    [Fact]
    public void Viewer_Should_NotHaveWriteAccess()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Viewer);

        policies.Should().NotContain(p => p.Action == "Write" || p.Action == "Create" || p.Action == "Delete" || p.Action == "*");
    }

    // ── Auditor ────────────────────────────────────────────────────────────

    [Fact]
    public void Auditor_Should_HaveFullAuditModuleAccess()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Auditor);

        policies.Should().Contain(p => p.Module == "Audit" && p.Page == "*" && p.Action == "*" && p.IsAllowed);
    }

    [Fact]
    public void Auditor_Should_HaveComplianceWildcard()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Auditor);

        policies.Should().Contain(p => p.Module == "Governance" && p.Page == "Compliance" && p.Action == "*");
    }

    [Fact]
    public void Auditor_Should_HaveSessionRead()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.Auditor);

        policies.Should().Contain(p => p.Module == "Identity" && p.Page == "Sessions" && p.Action == "Read");
    }

    // ── SecurityReview ─────────────────────────────────────────────────────

    [Fact]
    public void SecurityReview_Should_HaveBreakGlassDecide()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.SecurityReview);

        policies.Should().Contain(p => p.Module == "Identity" && p.Page == "BreakGlass" && p.Action == "Decide");
    }

    [Fact]
    public void SecurityReview_Should_HaveSessionWildcard()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.SecurityReview);

        policies.Should().Contain(p => p.Module == "Identity" && p.Page == "Sessions" && p.Action == "*");
    }

    [Fact]
    public void SecurityReview_Should_HaveRiskRead()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.SecurityReview);

        policies.Should().Contain(p => p.Module == "Governance" && p.Page == "Risk" && p.Action == "Read");
    }

    // ── ApprovalOnly ───────────────────────────────────────────────────────

    [Fact]
    public void ApprovalOnly_Should_NotHaveIdentityAccess()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.ApprovalOnly);

        policies.Should().NotContain(p => p.Module == "Identity");
    }

    [Fact]
    public void ApprovalOnly_Should_HavePromotionGatesOverride()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.ApprovalOnly);

        policies.Should().Contain(p => p.Module == "Promotion" && p.Page == "Gates" && p.Action == "Override");
    }

    [Fact]
    public void ApprovalOnly_Should_HaveWorkflowInstancesWildcard()
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.ApprovalOnly);

        policies.Should().Contain(p => p.Module == "Workflow" && p.Page == "Instances" && p.Action == "*");
    }

    // ── Teste de hierarquia de permissões ──────────────────────────────────

    [Fact]
    public void PlatformAdmin_Should_CoverAllModulesWithWildcards()
    {
        var adminPolicies = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.PlatformAdmin);
        var allModules = ModuleAccessPolicyCatalog.GetAllModules();

        // PlatformAdmin usa wildcards ("*", "*") por módulo — cobertura total com menos entradas
        var coveredModules = adminPolicies.Select(p => p.Module).Distinct().ToList();
        coveredModules.Should().BeEquivalentTo(allModules);
    }

    [Fact]
    public void ApprovalOnly_Should_HaveFewestPolicies()
    {
        var approvalCount = ModuleAccessPolicyCatalog.GetPoliciesForRole(Role.ApprovalOnly).Count;

        var otherRoles = new[] { Role.PlatformAdmin, Role.TechLead, Role.Developer, Role.Viewer, Role.Auditor, Role.SecurityReview };
        foreach (var role in otherRoles)
        {
            var count = ModuleAccessPolicyCatalog.GetPoliciesForRole(role).Count;
            approvalCount.Should().BeLessThanOrEqualTo(count,
                because: $"ApprovalOnly deve ter menos ou igual políticas que '{role}'");
        }
    }

    // ── Testes de GetAllModules ────────────────────────────────────────────

    [Fact]
    public void GetAllModules_Should_ReturnExpectedModuleList()
    {
        var modules = ModuleAccessPolicyCatalog.GetAllModules();

        modules.Should().NotBeEmpty();
        modules.Should().Contain("Identity");
        modules.Should().Contain("Catalog");
        modules.Should().Contain("Contracts");
        modules.Should().Contain("Operations");
        modules.Should().Contain("Governance");
        modules.Should().Contain("Audit");
        modules.Should().Contain("AI");
        modules.Should().Contain("Platform");
    }

    // ── Testes de PolicyEntry format ───────────────────────────────────────

    [Theory]
    [InlineData(Role.PlatformAdmin)]
    [InlineData(Role.TechLead)]
    [InlineData(Role.Developer)]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Auditor)]
    [InlineData(Role.SecurityReview)]
    [InlineData(Role.ApprovalOnly)]
    public void AllPolicies_Should_HaveNonEmptyModulePageAction(string roleName)
    {
        var policies = ModuleAccessPolicyCatalog.GetPoliciesForRole(roleName);

        foreach (var policy in policies)
        {
            policy.Module.Should().NotBeNullOrWhiteSpace($"Module deve ser preenchido para {roleName}");
            policy.Page.Should().NotBeNullOrWhiteSpace($"Page deve ser preenchido para {roleName}");
            policy.Action.Should().NotBeNullOrWhiteSpace($"Action deve ser preenchido para {roleName}");
        }
    }
}
