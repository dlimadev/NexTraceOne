using System.Linq;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class DefaultPromptTemplateCatalogTests
{
    [Fact]
    public void GetAll_Returns_NonEmpty_List()
    {
        var templates = DefaultPromptTemplateCatalog.GetAll();

        templates.Should().NotBeEmpty();
        templates.Count.Should().Be(DefaultPromptTemplateCatalog.GetAll().Count);
    }

    [Fact]
    public void All_Templates_Have_Required_Fields()
    {
        foreach (var template in DefaultPromptTemplateCatalog.GetAll())
        {
            template.Name.Should().NotBeNullOrWhiteSpace($"Template must have a name");
            template.DisplayName.Should().NotBeNullOrWhiteSpace($"Template '{template.Name}' must have a display name");
            template.Description.Should().NotBeNullOrWhiteSpace($"Template '{template.Name}' must have a description");
            template.Category.Should().NotBeNullOrWhiteSpace($"Template '{template.Name}' must have a category");
            template.Content.Should().NotBeNullOrWhiteSpace($"Template '{template.Name}' must have content");
            template.Relevance.Should().NotBeNullOrWhiteSpace($"Template '{template.Name}' must have relevance");
        }
    }

    [Fact]
    public void Template_Names_Are_Unique()
    {
        var names = DefaultPromptTemplateCatalog.GetAll().Select(t => t.Name).ToList();

        names.Should().OnlyHaveUniqueItems("template names must be unique in the catalog");
    }

    [Fact]
    public void All_Templates_Have_Valid_Categories()
    {
        var validCategories = new[] { "analysis", "operations", "engineering", "management", "troubleshooting", "governance" };

        foreach (var template in DefaultPromptTemplateCatalog.GetAll())
        {
            validCategories.Should().Contain(template.Category,
                $"Template '{template.Name}' has invalid category '{template.Category}'");
        }
    }

    [Fact]
    public void All_Templates_Have_Valid_Relevance()
    {
        var validRelevance = new[] { "high", "medium", "low" };

        foreach (var template in DefaultPromptTemplateCatalog.GetAll())
        {
            validRelevance.Should().Contain(template.Relevance,
                $"Template '{template.Name}' has invalid relevance '{template.Relevance}'");
        }
    }

    [Fact]
    public void Templates_With_Variables_Have_Matching_Placeholders()
    {
        foreach (var template in DefaultPromptTemplateCatalog.GetAll())
        {
            if (string.IsNullOrWhiteSpace(template.Variables))
                continue;

            var variables = template.Variables.Split(',', StringSplitOptions.TrimEntries);

            foreach (var variable in variables)
            {
                template.Content.Should().Contain($"{{{{{variable}}}}}",
                    $"Template '{template.Name}' declares variable '{variable}' but content doesn't use it");
            }
        }
    }

    [Fact]
    public void All_Templates_Have_Target_Personas()
    {
        foreach (var template in DefaultPromptTemplateCatalog.GetAll())
        {
            template.TargetPersonas.Should().NotBeNullOrWhiteSpace(
                $"Template '{template.Name}' must have target personas");
        }
    }

    [Fact]
    public void Contains_At_Least_One_Analysis_Template()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.Category == "analysis",
                "catalog must include at least one analysis template");
    }

    [Fact]
    public void Contains_At_Least_One_Operations_Template()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.Category == "operations",
                "catalog must include at least one operations template");
    }

    [Fact]
    public void Contains_At_Least_One_Governance_Template()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.Category == "governance",
                "catalog must include at least one governance template");
    }

    [Fact]
    public void Contains_At_Least_One_Engineering_Template()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.Category == "engineering",
                "catalog must include at least one engineering template");
    }

    [Fact]
    public void Contains_Templates_For_Engineer_Persona()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.TargetPersonas.Contains("Engineer"),
                "catalog must include templates targeting Engineer persona");
    }

    [Fact]
    public void Contains_Templates_For_Executive_Persona()
    {
        DefaultPromptTemplateCatalog.GetAll()
            .Should().Contain(t => t.TargetPersonas.Contains("Executive"),
                "catalog must include templates targeting Executive persona");
    }

    [Fact]
    public void Catalog_Has_Expected_Count()
    {
        DefaultPromptTemplateCatalog.GetAll().Count.Should().Be(10,
            "catalog should contain exactly 10 official prompt templates");
    }
}
