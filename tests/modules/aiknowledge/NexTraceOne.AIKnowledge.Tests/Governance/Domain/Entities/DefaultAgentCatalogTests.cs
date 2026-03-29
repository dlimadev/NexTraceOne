using System.Linq;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class DefaultAgentCatalogTests
{
    [Fact]
    public void GetAll_Returns_NonEmpty_List()
    {
        var agents = DefaultAgentCatalog.GetAll();

        agents.Should().NotBeEmpty();
        agents.Count.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void All_Agents_Have_Required_Fields()
    {
        foreach (var agent in DefaultAgentCatalog.GetAll())
        {
            agent.Name.Should().NotBeNullOrWhiteSpace($"Agent must have a name");
            agent.DisplayName.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have a display name");
            agent.Description.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have a description");
            agent.SystemPrompt.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have a system prompt");
            agent.Capabilities.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have capabilities");
            agent.TargetPersona.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have a target persona");
            agent.Icon.Should().NotBeNullOrWhiteSpace($"Agent '{agent.Name}' must have an icon");
        }
    }

    [Fact]
    public void Agent_Names_Are_Unique()
    {
        var names = DefaultAgentCatalog.GetAll().Select(a => a.Name).ToList();

        names.Should().OnlyHaveUniqueItems("agent names must be unique in the catalog");
    }

    [Fact]
    public void Agent_SortOrders_Are_Unique()
    {
        var sortOrders = DefaultAgentCatalog.GetAll().Select(a => a.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("agent sort orders must be unique in the catalog");
    }

    [Fact]
    public void Covers_Core_NexTraceOne_Domains()
    {
        var categories = DefaultAgentCatalog.GetAll().Select(a => a.Category).ToHashSet();

        categories.Should().Contain(AgentCategory.ServiceAnalysis, "must cover service analysis");
        categories.Should().Contain(AgentCategory.ApiDesign, "must cover API/contract design");
        categories.Should().Contain(AgentCategory.ChangeIntelligence, "must cover change intelligence");
        categories.Should().Contain(AgentCategory.IncidentResponse, "must cover incident response");
    }

    [Fact]
    public void All_Categories_Are_Valid_Enum_Values()
    {
        foreach (var agent in DefaultAgentCatalog.GetAll())
        {
            Enum.IsDefined(typeof(AgentCategory), agent.Category).Should()
                .BeTrue($"Agent '{agent.Name}' has invalid category {agent.Category}");
        }
    }

    [Fact]
    public void Target_Personas_Are_Valid()
    {
        var validPersonas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Engineer", "Tech Lead", "Architect", "Product", "Executive",
            "Platform Admin", "Auditor"
        };

        foreach (var agent in DefaultAgentCatalog.GetAll())
        {
            validPersonas.Should().Contain(agent.TargetPersona,
                $"Agent '{agent.Name}' has persona '{agent.TargetPersona}' which is not a valid NexTraceOne persona");
        }
    }

    [Fact]
    public void System_Prompts_Are_Not_Too_Short()
    {
        foreach (var agent in DefaultAgentCatalog.GetAll())
        {
            agent.SystemPrompt.Length.Should().BeGreaterThan(50,
                $"Agent '{agent.Name}' system prompt is too short for useful context");
        }
    }

    [Fact]
    public void Agent_Names_Follow_Kebab_Case()
    {
        foreach (var agent in DefaultAgentCatalog.GetAll())
        {
            agent.Name.Should().MatchRegex(@"^[a-z][a-z0-9\-]*$",
                $"Agent name '{agent.Name}' should follow kebab-case convention");
        }
    }

    [Fact]
    public void SortOrders_Are_Positive_And_Increasing()
    {
        var agents = DefaultAgentCatalog.GetAll().OrderBy(a => a.SortOrder).ToList();

        for (var i = 0; i < agents.Count; i++)
        {
            agents[i].SortOrder.Should().BePositive($"Agent '{agents[i].Name}' sort order must be positive");

            if (i > 0)
            {
                agents[i].SortOrder.Should().BeGreaterThan(agents[i - 1].SortOrder,
                    $"Agent '{agents[i].Name}' sort order must be greater than '{agents[i - 1].Name}'");
            }
        }
    }
}
