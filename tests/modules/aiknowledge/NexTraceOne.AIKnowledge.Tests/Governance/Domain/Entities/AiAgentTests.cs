using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

/// <summary>Testes unitários da entidade AiAgent — criação, atualização e ciclo de vida.</summary>
public sealed class AiAgentTests
{
    // ── Register ──────────────────────────────────────────────────────────

    [Fact]
    public void Register_WithValidData_ShouldCreateAgent()
    {
        var agent = AiAgent.Register(
            "service-analyst",
            "Service Analyst",
            "Analyzes service health and dependencies",
            AgentCategory.ServiceAnalysis,
            isOfficial: true,
            "You are a service analyst agent.",
            capabilities: "analysis,diagnostics",
            targetPersona: "Engineer",
            icon: "🔍",
            sortOrder: 10);

        agent.Name.Should().Be("service-analyst");
        agent.DisplayName.Should().Be("Service Analyst");
        agent.Slug.Should().Be("service-analyst");
        agent.Description.Should().Be("Analyzes service health and dependencies");
        agent.Category.Should().Be(AgentCategory.ServiceAnalysis);
        agent.IsOfficial.Should().BeTrue();
        agent.IsActive.Should().BeTrue();
        agent.SystemPrompt.Should().Be("You are a service analyst agent.");
        agent.PreferredModelId.Should().BeNull();
        agent.Capabilities.Should().Be("analysis,diagnostics");
        agent.TargetPersona.Should().Be("Engineer");
        agent.Icon.Should().Be("🔍");
        agent.SortOrder.Should().Be(10);
    }

    [Fact]
    public void Register_WithNullName_ShouldThrow()
    {
        var act = () => AiAgent.Register(
            null!, "Display", "desc", AgentCategory.General, false, "prompt");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_WithEmptyDisplayName_ShouldThrow()
    {
        var act = () => AiAgent.Register(
            "name", "", "desc", AgentCategory.General, false, "prompt");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_ShouldDeriveSlugFromName()
    {
        var agent = AiAgent.Register(
            "Contract Governance Agent",
            "Contract Governance",
            "desc",
            AgentCategory.ContractGovernance,
            false,
            "prompt");

        agent.Slug.Should().Be("contract-governance-agent");
    }

    [Fact]
    public void Register_WithExplicitSlug_ShouldUseProvidedSlug()
    {
        var agent = AiAgent.Register(
            "some-name",
            "Some Name",
            "desc",
            AgentCategory.General,
            false,
            "prompt",
            slug: "custom-slug");

        agent.Slug.Should().Be("custom-slug");
    }

    [Fact]
    public void Register_WithPreferredModel_ShouldSetModelId()
    {
        var modelId = Guid.NewGuid();
        var agent = AiAgent.Register(
            "agent-with-model",
            "Agent With Model",
            "desc",
            AgentCategory.CodeReview,
            true,
            "prompt",
            preferredModelId: modelId);

        agent.PreferredModelId.Should().Be(modelId);
    }

    [Fact]
    public void Register_ShouldDefaultSortOrderTo100()
    {
        var agent = AiAgent.Register(
            "default-order",
            "Default Order",
            "desc",
            AgentCategory.General,
            false,
            "prompt");

        agent.SortOrder.Should().Be(100);
    }

    [Fact]
    public void Register_WithNullOptionals_ShouldDefaultToEmpty()
    {
        var agent = AiAgent.Register(
            "minimal",
            "Minimal Agent",
            "desc",
            AgentCategory.General,
            false,
            "prompt");

        agent.Capabilities.Should().BeEmpty();
        agent.TargetPersona.Should().BeEmpty();
        agent.Icon.Should().BeEmpty();
    }

    // ── Update ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldModifyMutableProperties()
    {
        var agent = AiAgent.Register(
            "test", "Test Agent", "old description",
            AgentCategory.General, false, "prompt",
            capabilities: "old", targetPersona: "Engineer", icon: "🤖", sortOrder: 50);

        var result = agent.Update("Updated Agent", "new description", "new-caps", "Architect", "🚀", 20);

        result.IsSuccess.Should().BeTrue();
        agent.DisplayName.Should().Be("Updated Agent");
        agent.Description.Should().Be("new description");
        agent.Capabilities.Should().Be("new-caps");
        agent.TargetPersona.Should().Be("Architect");
        agent.Icon.Should().Be("🚀");
        agent.SortOrder.Should().Be(20);
    }

    [Fact]
    public void Update_WithNullCapabilities_ShouldPreserveExisting()
    {
        var agent = AiAgent.Register(
            "test", "Agent", "desc",
            AgentCategory.General, false, "prompt",
            capabilities: "existing-caps");

        agent.Update("Agent", "desc", null, null, null, null);

        agent.Capabilities.Should().Be("existing-caps");
    }

    [Fact]
    public void Update_WithEmptyDisplayName_ShouldThrow()
    {
        var agent = AiAgent.Register(
            "test", "Agent", "desc", AgentCategory.General, false, "prompt");

        var act = () => agent.Update("", "desc", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    // ── Activate / Deactivate ─────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var agent = AiAgent.Register(
            "test", "Agent", "desc", AgentCategory.General, false, "prompt");

        agent.IsActive.Should().BeTrue();

        var result = agent.Deactivate();

        result.IsSuccess.Should().BeTrue();
        agent.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var agent = AiAgent.Register(
            "test", "Agent", "desc", AgentCategory.General, false, "prompt");
        agent.Deactivate();

        var result = agent.Activate();

        result.IsSuccess.Should().BeTrue();
        agent.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var agent = AiAgent.Register(
            "test", "Agent", "desc", AgentCategory.General, false, "prompt");

        var result = agent.Activate();

        result.IsSuccess.Should().BeTrue();
        agent.IsActive.Should().BeTrue();
    }

    // ── AgentCategory coverage ────────────────────────────────────────────

    [Theory]
    [InlineData(AgentCategory.General)]
    [InlineData(AgentCategory.ServiceAnalysis)]
    [InlineData(AgentCategory.ContractGovernance)]
    [InlineData(AgentCategory.IncidentResponse)]
    [InlineData(AgentCategory.ChangeIntelligence)]
    [InlineData(AgentCategory.SecurityAudit)]
    [InlineData(AgentCategory.FinOps)]
    [InlineData(AgentCategory.CodeReview)]
    [InlineData(AgentCategory.Documentation)]
    [InlineData(AgentCategory.Testing)]
    [InlineData(AgentCategory.Compliance)]
    public void Register_ShouldAcceptAllCategories(AgentCategory category)
    {
        var agent = AiAgent.Register(
            $"agent-{category}", $"Agent {category}", "desc",
            category, false, "prompt");

        agent.Category.Should().Be(category);
    }
}
