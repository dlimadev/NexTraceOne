using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class AiToolDefinitionTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_ToolDefinition()
    {
        var tool = AiToolDefinition.Create(
            name: "get_service_health",
            displayName: "Get Service Health",
            description: "Retrieves service health status",
            category: "service_catalog",
            parametersSchema: """{"type":"object","properties":{"serviceId":{"type":"string"}}}""",
            version: 1,
            isActive: true,
            requiresApproval: false,
            riskLevel: 0,
            isOfficial: true,
            timeoutMs: 15000);

        tool.Should().NotBeNull();
        tool.Id.Value.Should().NotBeEmpty();
        tool.Name.Should().Be("get_service_health");
        tool.DisplayName.Should().Be("Get Service Health");
        tool.Description.Should().Be("Retrieves service health status");
        tool.Category.Should().Be("service_catalog");
        tool.ParametersSchema.Should().Contain("serviceId");
        tool.Version.Should().Be(1);
        tool.IsActive.Should().BeTrue();
        tool.RequiresApproval.Should().BeFalse();
        tool.RiskLevel.Should().Be(0);
        tool.IsOfficial.Should().BeTrue();
        tool.TimeoutMs.Should().Be(15000);
    }

    [Fact]
    public void Create_With_Default_Timeout_Uses_30000()
    {
        var tool = AiToolDefinition.Create(
            name: "test_tool",
            displayName: "Test Tool",
            description: "Test",
            category: "test",
            parametersSchema: "{}",
            version: 1,
            isActive: true,
            requiresApproval: false,
            riskLevel: 0,
            isOfficial: false);

        tool.TimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var t1 = CreateValidTool("tool1");
        var t2 = CreateValidTool("tool2");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void Create_Trims_String_Fields()
    {
        var tool = AiToolDefinition.Create(
            name: "  trimmed_tool  ",
            displayName: "  Trimmed Tool  ",
            description: "  description  ",
            category: "  operations  ",
            parametersSchema: "{}",
            version: 1,
            isActive: true,
            requiresApproval: false,
            riskLevel: 0,
            isOfficial: false);

        tool.Name.Should().Be("trimmed_tool");
        tool.DisplayName.Should().Be("Trimmed Tool");
        tool.Description.Should().Be("description");
        tool.Category.Should().Be("operations");
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Name(string? name)
    {
        var act = () => AiToolDefinition.Create(
            name: name!, displayName: "Display", description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: 0, isOfficial: false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_DisplayName(string? displayName)
    {
        var act = () => AiToolDefinition.Create(
            name: "valid", displayName: displayName!, description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: 0, isOfficial: false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Category(string? category)
    {
        var act = () => AiToolDefinition.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: category!, parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: 0, isOfficial: false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Rejects_Invalid_Version(int version)
    {
        var act = () => AiToolDefinition.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: version, isActive: true, requiresApproval: false,
            riskLevel: 0, isOfficial: false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(10)]
    public void Create_Rejects_Invalid_RiskLevel(int riskLevel)
    {
        var act = () => AiToolDefinition.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: riskLevel, isOfficial: false);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Rejects_Invalid_TimeoutMs(int timeoutMs)
    {
        var act = () => AiToolDefinition.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: 0, isOfficial: false, timeoutMs: timeoutMs);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_Accepts_Valid_RiskLevels(int riskLevel)
    {
        var tool = AiToolDefinition.Create(
            name: "valid", displayName: "Display", description: "Desc",
            category: "cat", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: riskLevel, isOfficial: false);

        tool.RiskLevel.Should().Be(riskLevel);
    }

    // ── State transitions ───────────────────────────────────────────────

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        var tool = CreateValidTool("test");

        tool.Deactivate();

        tool.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Sets_IsActive_True()
    {
        var tool = CreateValidTool("test");
        tool.Deactivate();

        tool.Activate();

        tool.IsActive.Should().BeTrue();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void AiToolDefinitionId_New_Creates_Unique_Id()
    {
        var id1 = AiToolDefinitionId.New();
        var id2 = AiToolDefinitionId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void AiToolDefinitionId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = AiToolDefinitionId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiToolDefinition CreateValidTool(string name) =>
        AiToolDefinition.Create(
            name: name,
            displayName: $"Display {name}",
            description: "Description",
            category: "service_catalog",
            parametersSchema: "{}",
            version: 1,
            isActive: true,
            requiresApproval: false,
            riskLevel: 0,
            isOfficial: false);
}
