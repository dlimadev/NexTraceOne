using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Domain.Entities;

/// <summary>Testes unitários da entidade PromotionGate.</summary>
public sealed class PromotionGateTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Create com valores válidos ─────────────────────────────────────────

    [Fact]
    public void Create_ShouldReturnGate_WithValidValues()
    {
        var gate = PromotionGate.Create(
            "Staging → Production Gate",
            "Ensures all validations pass before production deploy",
            "staging",
            "production",
            """{"rules":[{"type":"test_coverage","threshold":80}]}""",
            true,
            "admin@company.com",
            FixedNow,
            "tenant-1");

        gate.Should().NotBeNull();
        gate.Id.Value.Should().NotBeEmpty();
        gate.Name.Should().Be("Staging → Production Gate");
        gate.Description.Should().Be("Ensures all validations pass before production deploy");
        gate.EnvironmentFrom.Should().Be("staging");
        gate.EnvironmentTo.Should().Be("production");
        gate.Rules.Should().Contain("test_coverage");
        gate.IsActive.Should().BeTrue();
        gate.BlockOnFailure.Should().BeTrue();
        gate.CreatedBy.Should().Be("admin@company.com");
        gate.CreatedAt.Should().Be(FixedNow);
        gate.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void Create_ShouldAllowNullOptionalFields()
    {
        var gate = PromotionGate.Create(
            "Minimal Gate",
            null,
            "dev",
            "staging",
            null,
            false,
            null,
            FixedNow,
            null);

        gate.Description.Should().BeNull();
        gate.Rules.Should().BeNull();
        gate.CreatedBy.Should().BeNull();
        gate.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldDefaultToActive()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging", null, false, null, FixedNow, null);

        gate.IsActive.Should().BeTrue();
    }

    // ── Activate / Deactivate ──────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging", null, true, null, FixedNow, null);

        gate.Deactivate();

        gate.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging", null, true, null, FixedNow, null);

        gate.Deactivate();
        gate.Activate();

        gate.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ThenDeactivate_ShouldRemainInactive()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging", null, true, null, FixedNow, null);

        gate.Deactivate();
        gate.Deactivate();

        gate.IsActive.Should().BeFalse();
    }

    // ── UpdateRules ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateRules_ShouldReplaceExistingRules()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging",
            """{"rules":[]}""", true, null, FixedNow, null);

        gate.UpdateRules("""{"rules":[{"type":"approval_required"}]}""");

        gate.Rules.Should().Contain("approval_required");
    }

    [Fact]
    public void UpdateRules_ShouldAcceptNull()
    {
        var gate = PromotionGate.Create(
            "Gate", null, "dev", "staging",
            """{"rules":[]}""", true, null, FixedNow, null);

        gate.UpdateRules(null);

        gate.Rules.Should().BeNull();
    }

    // ── Guard clauses ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsNullOrWhitespace(string? name)
    {
        var act = () => PromotionGate.Create(
            name!, null, "dev", "staging", null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenNameExceedsMaxLength()
    {
        var longName = new string('x', 201);

        var act = () => PromotionGate.Create(
            longName, null, "dev", "staging", null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenDescriptionExceedsMaxLength()
    {
        var longDesc = new string('x', 2001);

        var act = () => PromotionGate.Create(
            "Gate", longDesc, "dev", "staging", null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenEnvironmentFromIsNullOrWhitespace(string? envFrom)
    {
        var act = () => PromotionGate.Create(
            "Gate", null, envFrom!, "staging", null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenEnvironmentFromExceedsMaxLength()
    {
        var longEnv = new string('x', 101);

        var act = () => PromotionGate.Create(
            "Gate", null, longEnv, "staging", null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenEnvironmentToIsNullOrWhitespace(string? envTo)
    {
        var act = () => PromotionGate.Create(
            "Gate", null, "dev", envTo!, null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenEnvironmentToExceedsMaxLength()
    {
        var longEnv = new string('x', 101);

        var act = () => PromotionGate.Create(
            "Gate", null, "dev", longEnv, null, true, null, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenCreatedByExceedsMaxLength()
    {
        var longCreatedBy = new string('x', 201);

        var act = () => PromotionGate.Create(
            "Gate", null, "dev", "staging", null, true, longCreatedBy, FixedNow, null);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly Typed Id ──────────────────────────────────────────────────

    [Fact]
    public void PromotionGateId_New_ShouldGenerateUniqueIds()
    {
        var id1 = PromotionGateId.New();
        var id2 = PromotionGateId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void PromotionGateId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = PromotionGateId.From(guid);

        id.Value.Should().Be(guid);
    }
}
