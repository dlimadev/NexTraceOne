using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade ContractComplianceGate.
/// Valida factory Create, guard clauses, activação/desactivação e actualização de regras.
/// </summary>
public sealed class ContractComplianceGateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInputs_ShouldCreateGate()
    {
        var gate = CreateValid();

        gate.Id.Value.Should().NotBe(Guid.Empty);
        gate.Name.Should().Be("Breaking Change Gate");
        gate.Description.Should().Be("Blocks deploys with breaking changes");
        gate.Scope.Should().Be(ComplianceGateScope.Organization);
        gate.ScopeId.Should().Be("org-001");
        gate.BlockOnViolation.Should().BeTrue();
        gate.IsActive.Should().BeTrue();
        gate.CreatedBy.Should().Be("admin@example.com");
        gate.CreatedAt.Should().Be(FixedNow);
        gate.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public void Create_AllScopes_ShouldBeAccepted()
    {
        foreach (var scope in Enum.GetValues<ComplianceGateScope>())
        {
            var gate = ContractComplianceGate.Create(
                name: "Gate",
                description: null,
                rules: null,
                scope: scope,
                scopeId: "scope-id",
                blockOnViolation: false,
                createdBy: null,
                createdAt: FixedNow,
                tenantId: null);

            gate.Scope.Should().Be(scope);
        }
    }

    [Fact]
    public void Create_NullOptionalFields_ShouldBeValid()
    {
        var gate = ContractComplianceGate.Create(
            name: "Minimal Gate",
            description: null,
            rules: null,
            scope: ComplianceGateScope.Team,
            scopeId: "team-001",
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        gate.Description.Should().BeNull();
        gate.Rules.Should().BeNull();
        gate.CreatedBy.Should().BeNull();
        gate.TenantId.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var gate = CreateValid();
        gate.IsActive.Should().BeTrue();

        gate.Deactivate();

        gate.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var gate = CreateValid();
        gate.Deactivate();
        gate.IsActive.Should().BeFalse();

        gate.Activate();

        gate.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateRules_ShouldReplaceRules()
    {
        var gate = CreateValid();
        var newRules = """{"rules":[{"type":"HealthScoreMin","threshold":60}]}""";

        gate.UpdateRules(newRules);

        gate.Rules.Should().Be(newRules);
    }

    [Fact]
    public void UpdateRules_NullRules_ShouldClearRules()
    {
        var gate = CreateValid();
        gate.UpdateRules(null);

        gate.Rules.Should().BeNull();
    }

    // ── Guard clauses ──

    [Fact]
    public void Create_NullName_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: null!,
            description: null,
            rules: null,
            scope: ComplianceGateScope.Organization,
            scopeId: "org-001",
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: "   ",
            description: null,
            rules: null,
            scope: ComplianceGateScope.Organization,
            scopeId: "org-001",
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NameTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: new string('x', 201),
            description: null,
            rules: null,
            scope: ComplianceGateScope.Organization,
            scopeId: "org-001",
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NullScopeId_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: "Gate",
            description: null,
            rules: null,
            scope: ComplianceGateScope.Team,
            scopeId: null!,
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ScopeIdTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: "Gate",
            description: null,
            rules: null,
            scope: ComplianceGateScope.Environment,
            scopeId: new string('x', 201),
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_DescriptionTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceGate.Create(
            name: "Gate",
            description: new string('x', 2001),
            rules: null,
            scope: ComplianceGateScope.Organization,
            scopeId: "org-001",
            blockOnViolation: false,
            createdBy: null,
            createdAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helper ──

    private static ContractComplianceGate CreateValid() => ContractComplianceGate.Create(
        name: "Breaking Change Gate",
        description: "Blocks deploys with breaking changes",
        rules: """{"rules":[{"type":"BreakingChangeApproval","required":true}]}""",
        scope: ComplianceGateScope.Organization,
        scopeId: "org-001",
        blockOnViolation: true,
        createdBy: "admin@example.com",
        createdAt: FixedNow,
        tenantId: "tenant1");
}
