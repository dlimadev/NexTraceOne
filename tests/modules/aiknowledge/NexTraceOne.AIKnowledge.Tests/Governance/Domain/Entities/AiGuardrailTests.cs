using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class AiGuardrailTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Guardrail()
    {
        var guardrail = AiGuardrail.Create(
            name: "pii-detection",
            displayName: "PII Detection",
            description: "Detects PII in content",
            category: "privacy",
            guardType: "both",
            pattern: @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            patternType: "regex",
            severity: "high",
            action: "warn",
            userMessage: "PII detected",
            isActive: true,
            isOfficial: true,
            agentId: null,
            modelId: null,
            priority: 10);

        guardrail.Should().NotBeNull();
        guardrail.Id.Value.Should().NotBeEmpty();
        guardrail.Name.Should().Be("pii-detection");
        guardrail.DisplayName.Should().Be("PII Detection");
        guardrail.Description.Should().Be("Detects PII in content");
        guardrail.Category.Should().Be("privacy");
        guardrail.GuardType.Should().Be("both");
        guardrail.PatternType.Should().Be("regex");
        guardrail.Severity.Should().Be("high");
        guardrail.Action.Should().Be("warn");
        guardrail.UserMessage.Should().Be("PII detected");
        guardrail.IsActive.Should().BeTrue();
        guardrail.IsOfficial.Should().BeTrue();
        guardrail.AgentId.Should().BeNull();
        guardrail.ModelId.Should().BeNull();
        guardrail.Priority.Should().Be(10);
    }

    [Fact]
    public void Create_With_Agent_And_Model_Scoping()
    {
        var agentId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var guardrail = AiGuardrail.Create(
            name: "scoped-guard",
            displayName: "Scoped Guard",
            description: "Agent-specific guard",
            category: "security",
            guardType: "input",
            pattern: "test-pattern",
            patternType: "keyword",
            severity: "medium",
            action: "log",
            userMessage: null,
            isActive: true,
            isOfficial: false,
            agentId: agentId,
            modelId: modelId,
            priority: 5);

        guardrail.AgentId.Should().Be(agentId);
        guardrail.ModelId.Should().Be(modelId);
        guardrail.IsOfficial.Should().BeFalse();
        guardrail.UserMessage.Should().BeNull();
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var g1 = CreateValidGuardrail("guard1");
        var g2 = CreateValidGuardrail("guard2");

        g1.Id.Should().NotBe(g2.Id);
    }

    [Fact]
    public void Create_Trims_String_Fields()
    {
        var guardrail = AiGuardrail.Create(
            name: "  trimmed-name  ",
            displayName: "  Trimmed Name  ",
            description: "  desc  ",
            category: "  security  ",
            guardType: "  input  ",
            pattern: "pattern",
            patternType: "  regex  ",
            severity: "  high  ",
            action: "  block  ",
            userMessage: "  message  ",
            isActive: true,
            isOfficial: false,
            agentId: null,
            modelId: null,
            priority: 1);

        guardrail.Name.Should().Be("trimmed-name");
        guardrail.DisplayName.Should().Be("Trimmed Name");
        guardrail.Description.Should().Be("desc");
        guardrail.Category.Should().Be("security");
        guardrail.GuardType.Should().Be("input");
        guardrail.PatternType.Should().Be("regex");
        guardrail.Severity.Should().Be("high");
        guardrail.Action.Should().Be("block");
        guardrail.UserMessage.Should().Be("message");
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Name(string? name)
    {
        var act = () => AiGuardrail.Create(
            name: name!, displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_DisplayName(string? displayName)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: displayName!, description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Category(string? category)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: category!, guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_GuardType(string? guardType)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: guardType!, pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Pattern(string? pattern)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: pattern!,
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_PatternType(string? patternType)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: patternType!, severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Severity(string? severity)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: severity!, action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Action(string? action)
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: action!,
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Negative_Priority()
    {
        var act = () => AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: -1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Accepts_Zero_Priority()
    {
        var guardrail = AiGuardrail.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "security", guardType: "input", pattern: "p",
            patternType: "regex", severity: "high", action: "block",
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 0);

        guardrail.Priority.Should().Be(0);
    }

    // ── State transitions ───────────────────────────────────────────────

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        var guardrail = CreateValidGuardrail("test");

        guardrail.Deactivate();

        guardrail.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Sets_IsActive_True()
    {
        var guardrail = CreateValidGuardrail("test");
        guardrail.Deactivate();

        guardrail.Activate();

        guardrail.IsActive.Should().BeTrue();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void AiGuardrailId_New_Creates_Unique_Id()
    {
        var id1 = AiGuardrailId.New();
        var id2 = AiGuardrailId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void AiGuardrailId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = AiGuardrailId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiGuardrail CreateValidGuardrail(string name) =>
        AiGuardrail.Create(
            name: name,
            displayName: $"Display {name}",
            description: "Description",
            category: "security",
            guardType: "input",
            pattern: "test-pattern",
            patternType: "regex",
            severity: "high",
            action: "block",
            userMessage: null,
            isActive: true,
            isOfficial: false,
            agentId: null,
            modelId: null,
            priority: 1);
}
