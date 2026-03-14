using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Tests.Domain.ValueObjects;

/// <summary>
/// Testes unitários para os value objects do modelo canônico:
/// ContractOperation, ContractSchemaElement, CompatibilityAssessment,
/// SchemaRegistryBinding, InteroperabilityProfile, SchemaEvolutionRule.
/// </summary>
public sealed class CanonicalModelValueObjectTests
{
    [Fact]
    public void ContractOperation_Should_StoreAllProperties()
    {
        var op = new ContractOperation(
            "listUsers", "listUsers", "List all users", "GET", "/users",
            [new ContractSchemaElement("page", "integer", false)],
            [new ContractSchemaElement("users", "array", true)],
            false, ["users"]);

        op.OperationId.Should().Be("listUsers");
        op.Method.Should().Be("GET");
        op.Path.Should().Be("/users");
        op.InputParameters.Should().HaveCount(1);
        op.OutputFields.Should().HaveCount(1);
        op.IsDeprecated.Should().BeFalse();
        op.Tags.Should().Contain("users");
    }

    [Fact]
    public void ContractSchemaElement_Should_SupportNestedChildren()
    {
        var child = new ContractSchemaElement("email", "string", true, "User email", "email");
        var parent = new ContractSchemaElement("user", "object", true, Children: [child]);

        parent.Children.Should().HaveCount(1);
        parent.Children![0].Name.Should().Be("email");
        parent.Children![0].Format.Should().Be("email");
    }

    [Fact]
    public void CompatibilityAssessment_Should_StoreAllProperties()
    {
        var assessment = new CompatibilityAssessment(
            NexTraceOne.BuildingBlocks.Domain.Enums.ChangeLevel.Breaking,
            false, "2.0.0", 0.7m, 3, 1, 0,
            true, true, "Breaking change detected",
            ContractProtocol.OpenApi);

        assessment.IsBackwardCompatible.Should().BeFalse();
        assessment.RequiresWorkflowApproval.Should().BeTrue();
        assessment.RequiresChangeNotification.Should().BeTrue();
        assessment.RiskScore.Should().Be(0.7m);
    }

    [Fact]
    public void SchemaRegistryBinding_Should_StoreKafkaMetadata()
    {
        var binding = new SchemaRegistryBinding(
            "orders-value", 3, 42, "AVRO", "BACKWARD",
            "orders", ["OrderService"], ["PaymentService", "ShippingService"],
            "http://registry:8081");

        binding.Subject.Should().Be("orders-value");
        binding.SchemaVersion.Should().Be(3);
        binding.SchemaId.Should().Be(42);
        binding.SchemaFormat.Should().Be("AVRO");
        binding.CompatibilityMode.Should().Be("BACKWARD");
        binding.Producers.Should().HaveCount(1);
        binding.Consumers.Should().HaveCount(2);
        binding.RegistryUrl.Should().Be("http://registry:8081");
    }

    [Fact]
    public void InteroperabilityProfile_Should_StoreCapabilities()
    {
        var profile = new InteroperabilityProfile(
            ContractProtocol.OpenApi,
            ["openapi-json", "openapi-yaml"],
            [ContractProtocol.AsyncApi],
            true, false,
            ["parsing", "diff", "validation", "scorecard"]);

        profile.SourceProtocol.Should().Be(ContractProtocol.OpenApi);
        profile.SupportedExportFormats.Should().HaveCount(2);
        profile.ConvertibleTo.Should().Contain(ContractProtocol.AsyncApi);
        profile.SupportsRoundTrip.Should().BeTrue();
        profile.HasSchemaRegistryBinding.Should().BeFalse();
        profile.Capabilities.Should().HaveCount(4);
    }

    [Fact]
    public void SchemaEvolutionRule_Should_StoreRuleDetails()
    {
        var rule = new SchemaEvolutionRule(
            "backward-compatibility",
            "Fields cannot be removed",
            "FieldRemoved", false, "Error",
            "Add a default value instead of removing the field");

        rule.RuleName.Should().Be("backward-compatibility");
        rule.IsAllowed.Should().BeFalse();
        rule.Severity.Should().Be("Error");
        rule.SuggestedFix.Should().NotBeNull();
    }

    [Fact]
    public void ContractCanonicalModel_Should_StoreFullModel()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "Users API", "3.1.0", "User management",
            [new ContractOperation("list", "list", "desc", "GET", "/users", [], [])],
            [new ContractSchemaElement("User", "object", false)],
            ["bearer"], ["https://api.example.com"], ["users"],
            1, 1, true, true, true);

        model.Protocol.Should().Be(ContractProtocol.OpenApi);
        model.Title.Should().Be("Users API");
        model.OperationCount.Should().Be(1);
        model.SchemaCount.Should().Be(1);
        model.HasSecurityDefinitions.Should().BeTrue();
        model.HasExamples.Should().BeTrue();
        model.HasDescriptions.Should().BeTrue();
    }

    [Fact]
    public void KafkaSchemaCompatibility_Should_HaveExpectedValues()
    {
        KafkaSchemaCompatibility.None.Should().Be((KafkaSchemaCompatibility)0);
        KafkaSchemaCompatibility.Backward.Should().Be((KafkaSchemaCompatibility)1);
        KafkaSchemaCompatibility.Full.Should().Be((KafkaSchemaCompatibility)5);
        KafkaSchemaCompatibility.FullTransitive.Should().Be((KafkaSchemaCompatibility)6);
    }

    [Fact]
    public void ContractOperation_Should_DefaultOptionalFields()
    {
        var op = new ContractOperation("op1", "op1", null, "GET", "/test", [], []);

        op.IsDeprecated.Should().BeFalse();
        op.Tags.Should().BeNull();
        op.Description.Should().BeNull();
    }

    [Fact]
    public void SchemaRegistryBinding_Should_DefaultRegistryUrl()
    {
        var binding = new SchemaRegistryBinding(
            "test-value", 1, null, "JSON", "NONE", "test-topic", [], []);

        binding.RegistryUrl.Should().BeNull();
        binding.SchemaId.Should().BeNull();
    }
}
