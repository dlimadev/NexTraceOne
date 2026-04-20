using System.Linq;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

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
            Enum.IsDefined(guardrail.Category).Should().BeTrue($"Guardrail '{guardrail.Name}' must have a valid category");
            Enum.IsDefined(guardrail.GuardType).Should().BeTrue($"Guardrail '{guardrail.Name}' must have a valid guard type");
            guardrail.Pattern.Should().NotBeNullOrWhiteSpace($"Guardrail '{guardrail.Name}' must have a pattern");
            Enum.IsDefined(guardrail.PatternType).Should().BeTrue($"Guardrail '{guardrail.Name}' must have a valid pattern type");
            Enum.IsDefined(guardrail.Severity).Should().BeTrue($"Guardrail '{guardrail.Name}' must have a valid severity");
            Enum.IsDefined(guardrail.Action).Should().BeTrue($"Guardrail '{guardrail.Name}' must have a valid action");
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
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            Enum.GetValues<GuardrailCategory>().Should().Contain(guardrail.Category,
                $"Guardrail '{guardrail.Name}' has invalid category '{guardrail.Category}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_GuardTypes()
    {
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            Enum.GetValues<GuardrailType>().Should().Contain(guardrail.GuardType,
                $"Guardrail '{guardrail.Name}' has invalid guard type '{guardrail.GuardType}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_Severity()
    {
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            Enum.GetValues<GuardrailSeverity>().Should().Contain(guardrail.Severity,
                $"Guardrail '{guardrail.Name}' has invalid severity '{guardrail.Severity}'");
        }
    }

    [Fact]
    public void All_Guardrails_Have_Valid_Actions()
    {
        foreach (var guardrail in DefaultGuardrailCatalog.GetAll())
        {
            Enum.GetValues<GuardrailAction>().Should().Contain(guardrail.Action,
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
            .Should().Contain(g => g.Category == GuardrailCategory.Security,
                "catalog must include security guardrails");
    }

    [Fact]
    public void Contains_Privacy_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == GuardrailCategory.Privacy,
                "catalog must include privacy guardrails");
    }

    [Fact]
    public void Contains_Compliance_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == GuardrailCategory.Compliance,
                "catalog must include compliance guardrails");
    }

    [Fact]
    public void Contains_Quality_Guardrails()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.Category == GuardrailCategory.Quality,
                "catalog must include quality guardrails");
    }

    [Fact]
    public void Contains_Input_Guards()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.GuardType == GuardrailType.Input,
                "catalog must include input guards");
    }

    [Fact]
    public void Contains_Output_Guards()
    {
        DefaultGuardrailCatalog.GetAll()
            .Should().Contain(g => g.GuardType == GuardrailType.Output,
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
        DefaultGuardrailCatalog.GetAll().Count.Should().Be(11,
            "catalog should contain exactly 11 official guardrails (Wave A.4 added 3 PII/secret redaction guardrails)");
    }

    [Fact]
    public void Contains_Pii_Redaction_Guardrails_With_Sanitize_Action()
    {
        var catalog = DefaultGuardrailCatalog.GetAll();

        catalog.Should().Contain(g => g.Name == "pii-credit-card-redaction" && g.Action == GuardrailAction.Sanitize,
            "Wave A.4 requires credit-card PAN masking, not only detection");
        catalog.Should().Contain(g => g.Name == "pii-national-id-redaction" && g.Action == GuardrailAction.Sanitize,
            "Wave A.4 requires national-ID masking, not only detection");
        catalog.Should().Contain(g => g.Name == "secret-bearer-token-redaction" && g.Action == GuardrailAction.Sanitize,
            "Wave A.4 requires bearer-token masking, not only detection");
        catalog.Single(g => g.Name == "pii-email-detection").Action
            .Should().Be(GuardrailAction.Sanitize, "email PII must be masked, not only warned");
        catalog.Single(g => g.Name == "pii-phone-detection").Action
            .Should().Be(GuardrailAction.Sanitize, "phone PII must be masked, not only warned");
    }
}
