using System.Linq;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class DefaultGuardrailCatalogTests
{
    [Fact]
    public void GetAll_Returns_NonEmpty_List()
    {
        var guardrails = DefaultGuardrailCatalog.GetAll();

        guardrails.Should().NotBeEmpty();
        guardrails.Count.Should().Be(DefaultGuardrailCatalog.GetAll().Count);
    }

    [Fact]
    public void All_Guardrails_Have_Required_Fields()
    {
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            guardrail.Name.Should().NotBeNullOrWhiteSpace($"Guardrail must have a name");
            guardrail.DisplayName.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a display name");
            guardrail.Description.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a description");
            guardrail.Category.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a category");
            guardrail.GuardType.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a guard type");
            guardrail.Pattern.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a pattern");
            guardrail.PatternType.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a pattern type");
            guardrail.Severity.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a severity");
            guardrail.Action.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have an action");
        }
    }

    [Fact]
    public void Guardrail_Names_Are_Unique()
    {
        var names = DefaultGuardrailCatalog.GetAll().Select(g => g.Name).ToList();

        names.Should().OnlyHaveUniqueItems("guardrail names must be unique in the catalog");
    }

    [Fact]
    public void All_Guardrails_Have_Valid_Categories()
    {
        var validCategories = new[] { "security", "privacy", "compliance", "quality" };

        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            validCategories.Should().Contain(guardrail.Category,
                $"Guardrail '{guardrail.Name}' has invalid category '{guardrail.Category}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_GuardTypes()
    {
        var validTypes = new[] { "input", "output", "both" };

        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            validTypes.Should().Contain(guardrail.GuardType,
                $"Guardrail '{guardrail.Name}' has invalid guard type '{guardrail.GuardType}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_Severity()
    {
        var validSeverity = new[] { "critical", "high", "medium", "low", "info" };

        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            validSeverity.Should().Contain(guardrail.Severity,
                $"Guardrail '{guardrail.Name}' has invalid severity '{guardrail.Severity}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_Actions()
    {
        var validActions = new[] { "block", "sanitize", "warn", "log" };

        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            validActions.Should().Contain(guardrail.Action,
                $"Guardrail '{guardrail.Name}' has invalid action '{guardrail.Action}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Positive_Priority()
    {
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            guardrail.Priority.Should().BeGreaterThan(0,
                $"Guardrail '{guardrail.Name}' must have positive priority");
        }
    }

    [Fact]
    public void Contains_Security_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == "security",
                "catalog must include security guardrails");
    }

    [Fact]
    public void Contains_Privacy_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == "privacy",
                "catalog must include privacy guardrails");
    }

    [Fact]
    public void Contains_Compliance_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == "compliance",
                "catalog must include compliance guardrails");
    }

    [Fact]
    public void Contains_Quality_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == "quality",
                "catalog must include quality guardrails");
    }

    [Fact]
    public void Contains_Input_Guards()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.GuardType == "input",
                "catalog must include input guards");
    }

    [Fact]
    public void Contains_Output_Guards()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.GuardType == "output",
                "catalog must include output guards");
    }

    [Fact]
    public void Contains_Prompt_Injection_Guard()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Name == "prompt-injection-detection",
                "catalog must include prompt injection detection guard");
    }

    [Fact]
    public void Catalog_Has_Expected_Count()
    {
        DefaultGuardrailCatalog.GetAll().Count.Should().Be(8,
            "catalog should contain exactly 8 official guardrails");
    }
}
