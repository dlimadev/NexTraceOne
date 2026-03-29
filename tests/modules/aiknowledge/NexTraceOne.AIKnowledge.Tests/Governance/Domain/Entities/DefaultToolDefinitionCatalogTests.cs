using System.Linq;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class DefaultToolDefinitionCatalogTests
{
    [Fact]
    public void GetAll_Returns_NonEmpty_List()
    {
        var tools = DefaultToolDefinitionCatalog.GetAll();

        tools.Should().NotBeEmpty();
        tools.Count.Should().Be(DefaultToolDefinitionCatalog.GetAll().Count);
    }

    [Fact]
    public void All_Tools_Have_Required_Fields()
    {
        foreach (var tool in DefaultToolDefinitionCatalog.GetAll())
        {
            tool.Name.Should().NotBeNullOrWhiteSpace($"Tool must have a name");
            tool.DisplayName.Should().NotBeNullOrWhiteSpace($"Tool '{tool.Name}' must have a display name");
            tool.Description.Should().NotBeNullOrWhiteSpace($"Tool '{tool.Name}' must have a description");
            tool.Category.Should().NotBeNullOrWhiteSpace($"Tool '{tool.Name}' must have a category");
            tool.ParametersSchema.Should().NotBeNullOrWhiteSpace($"Tool '{tool.Name}' must have a parameters schema");
            tool.TimeoutMs.Should().BeGreaterThan(0, $"Tool '{tool.Name}' must have a positive timeout");
        }
    }

    [Fact]
    public void Tool_Names_Are_Unique()
    {
        var names = DefaultToolDefinitionCatalog.GetAll().Select(t => t.Name).ToList();

        names.Should().OnlyHaveUniqueItems("tool names must be unique in the catalog");
    }

    [Fact]
    public void All_Tools_Have_Valid_RiskLevel()
    {
        foreach (var tool in DefaultToolDefinitionCatalog.GetAll())
        {
            tool.RiskLevel.Should().BeInRange(0, 3,
                $"Tool '{tool.Name}' risk level must be between 0 and 3");
        }
    }

    [Fact]
    public void All_Tools_Have_Valid_JSON_ParametersSchema()
    {
        foreach (var tool in DefaultToolDefinitionCatalog.GetAll())
        {
            // Basic JSON validation: starts with { and ends with }
            tool.ParametersSchema.Should().StartWith("{",
                $"Tool '{tool.Name}' parameters schema must be valid JSON");
            tool.ParametersSchema.Should().EndWith("}",
                $"Tool '{tool.Name}' parameters schema must be valid JSON");
        }
    }

    [Fact]
    public void Contains_Service_Catalog_Tools()
    {
        DefaultToolDefinitionCatalog.GetAll()
            .Should().Contain(t => t.Category == "service_catalog",
                "catalog must include service catalog tools");
    }

    [Fact]
    public void Contains_Change_Governance_Tools()
    {
        DefaultToolDefinitionCatalog.GetAll()
            .Should().Contain(t => t.Category == "change_governance",
                "catalog must include change governance tools");
    }

    [Fact]
    public void Contains_Operations_Tools()
    {
        DefaultToolDefinitionCatalog.GetAll()
            .Should().Contain(t => t.Category == "operations",
                "catalog must include operations tools");
    }

    [Fact]
    public void No_Official_Tools_Require_Approval_By_Default()
    {
        // Official tools that are read-only should not require approval
        var readOnlyTools = DefaultToolDefinitionCatalog.GetAll()
            .Where(t => t.RiskLevel == 0);

        foreach (var tool in readOnlyTools)
        {
            tool.RequiresApproval.Should().BeFalse(
                $"Read-only tool '{tool.Name}' should not require approval");
        }
    }

    [Fact]
    public void Catalog_Has_Expected_Count()
    {
        DefaultToolDefinitionCatalog.GetAll().Count.Should().Be(6,
            "catalog should contain exactly 6 official tool definitions");
    }

    [Fact]
    public void All_Timeouts_Are_Reasonable()
    {
        foreach (var tool in DefaultToolDefinitionCatalog.GetAll())
        {
            tool.TimeoutMs.Should().BeGreaterThanOrEqualTo(5000,
                $"Tool '{tool.Name}' timeout should be at least 5 seconds");
            tool.TimeoutMs.Should().BeLessThanOrEqualTo(60000,
                $"Tool '{tool.Name}' timeout should not exceed 60 seconds");
        }
    }
}
