using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class PromptTemplateTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Template()
    {
        var template = PromptTemplate.Create(
            name: "incident-root-cause",
            displayName: "Incident Root Cause",
            description: "Analyzes incidents",
            category: "analysis",
            content: "Analyze {{serviceName}} in {{environment}}",
            variables: "serviceName,environment",
            version: 1,
            isActive: true,
            isOfficial: true,
            agentId: null,
            targetPersonas: "Engineer,TechLead",
            scopeHint: "incidentId",
            relevance: "high");

        template.Should().NotBeNull();
        template.Id.Value.Should().NotBeEmpty();
        template.Name.Should().Be("incident-root-cause");
        template.DisplayName.Should().Be("Incident Root Cause");
        template.Description.Should().Be("Analyzes incidents");
        template.Category.Should().Be("analysis");
        template.Content.Should().Contain("{{serviceName}}");
        template.Variables.Should().Be("serviceName,environment");
        template.Version.Should().Be(1);
        template.IsActive.Should().BeTrue();
        template.IsOfficial.Should().BeTrue();
        template.AgentId.Should().BeNull();
        template.TargetPersonas.Should().Be("Engineer,TechLead");
        template.ScopeHint.Should().Be("incidentId");
        template.Relevance.Should().Be("high");
    }

    [Fact]
    public void Create_With_Optional_Model_Parameters()
    {
        var modelId = Guid.NewGuid();
        var template = PromptTemplate.Create(
            name: "test-template",
            displayName: "Test Template",
            description: "Test",
            category: "engineering",
            content: "Do something",
            variables: "",
            version: 1,
            isActive: true,
            isOfficial: false,
            agentId: Guid.NewGuid(),
            targetPersonas: "Engineer",
            scopeHint: null,
            relevance: "medium",
            preferredModelId: modelId,
            recommendedTemperature: 0.7m,
            maxOutputTokens: 2048);

        template.PreferredModelId.Should().Be(modelId);
        template.RecommendedTemperature.Should().Be(0.7m);
        template.MaxOutputTokens.Should().Be(2048);
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var t1 = CreateValidTemplate("t1");
        var t2 = CreateValidTemplate("t2");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void Create_Trims_String_Fields()
    {
        var template = PromptTemplate.Create(
            name: "  trimmed-name  ",
            displayName: "  Trimmed Name  ",
            description: "  desc  ",
            category: "  analysis  ",
            content: "content",
            variables: "  var1,var2  ",
            version: 1,
            isActive: true,
            isOfficial: false,
            agentId: null,
            targetPersonas: "  Engineer  ",
            scopeHint: "  serviceId  ",
            relevance: "  high  ");

        template.Name.Should().Be("trimmed-name");
        template.DisplayName.Should().Be("Trimmed Name");
        template.Description.Should().Be("desc");
        template.Category.Should().Be("analysis");
        template.TargetPersonas.Should().Be("Engineer");
        template.ScopeHint.Should().Be("serviceId");
        template.Relevance.Should().Be("high");
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Name(string? name)
    {
        var act = () => PromptTemplate.Create(
            name: name!, displayName: "Display", description: "Desc",
            category: "analysis", content: "content", variables: "",
            version: 1, isActive: true, isOfficial: false, agentId: null,
            targetPersonas: "", scopeHint: null, relevance: "medium");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_DisplayName(string? displayName)
    {
        var act = () => PromptTemplate.Create(
            name: "valid-name", displayName: displayName!, description: "Desc",
            category: "analysis", content: "content", variables: "",
            version: 1, isActive: true, isOfficial: false, agentId: null,
            targetPersonas: "", scopeHint: null, relevance: "medium");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Category(string? category)
    {
        var act = () => PromptTemplate.Create(
            name: "valid-name", displayName: "Display", description: "Desc",
            category: category!, content: "content", variables: "",
            version: 1, isActive: true, isOfficial: false, agentId: null,
            targetPersonas: "", scopeHint: null, relevance: "medium");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_Content(string? content)
    {
        var act = () => PromptTemplate.Create(
            name: "valid-name", displayName: "Display", description: "Desc",
            category: "analysis", content: content!, variables: "",
            version: 1, isActive: true, isOfficial: false, agentId: null,
            targetPersonas: "", scopeHint: null, relevance: "medium");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Rejects_Invalid_Version(int version)
    {
        var act = () => PromptTemplate.Create(
            name: "valid-name", displayName: "Display", description: "Desc",
            category: "analysis", content: "content", variables: "",
            version: version, isActive: true, isOfficial: false, agentId: null,
            targetPersonas: "", scopeHint: null, relevance: "medium");

        act.Should().Throw<ArgumentException>();
    }

    // ── State transitions ───────────────────────────────────────────────

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        var template = CreateValidTemplate("test");

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Sets_IsActive_True()
    {
        var template = CreateValidTemplate("test");
        template.Deactivate();

        template.Activate();

        template.IsActive.Should().BeTrue();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void PromptTemplateId_New_Creates_Unique_Id()
    {
        var id1 = PromptTemplateId.New();
        var id2 = PromptTemplateId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void PromptTemplateId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = PromptTemplateId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static PromptTemplate CreateValidTemplate(string name) =>
        PromptTemplate.Create(
            name: name,
            displayName: $"Display {name}",
            description: "Description",
            category: "analysis",
            content: "Analyze {{serviceName}}",
            variables: "serviceName",
            version: 1,
            isActive: true,
            isOfficial: false,
            agentId: null,
            targetPersonas: "Engineer",
            scopeHint: null,
            relevance: "medium");
}
