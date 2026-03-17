using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="ContractRuleEngine"/>.
/// Valida a avaliação de regras determinísticas de conformidade sobre contratos.
/// </summary>
public sealed class ContractRuleEngineTests
{
    [Fact]
    public void Evaluate_Should_DetectMissingDescription_When_OperationHasNoDescription()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("listUsers", "listUsers", null, "GET", "/users", [], [])],
            [], ["bearer"], [], [],
            1, 0, true, true, false);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "OperationDescription");
    }

    [Fact]
    public void Evaluate_Should_DetectMissingSecurity_When_NoSchemesDefined()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [], [], [], [], [],
            0, 0, false, false, false);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "SecurityDefinition" && v.Severity == "Error");
    }

    [Fact]
    public void Evaluate_Should_DetectMissingExamples_When_NoExamplesPresent()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("op", "op", "desc", "GET", "/", [], [])],
            [], ["bearer"], [], [],
            1, 0, true, false, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "ExamplesCoverage");
    }

    [Fact]
    public void Evaluate_Should_DetectNamingViolation_When_NameHasSpaces()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("op", "list users", "desc", "GET", "/", [], [])],
            [], ["bearer"], [], [],
            1, 0, true, true, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "NamingConvention");
    }

    [Fact]
    public void Evaluate_Should_DetectSchemaCompleteness_When_TypeMissing()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [], [new ContractSchemaElement("User", "", false)],
            ["bearer"], [], [],
            0, 1, true, true, false);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "SchemaCompleteness");
    }

    [Fact]
    public void Evaluate_Should_ReturnNoErrors_When_ModelIsFullyCompliant()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0.0", null,
            [new ContractOperation("listUsers", "listUsers", "List all users", "GET", "/users", [], [])],
            [new ContractSchemaElement("User", "object", false)],
            ["bearerAuth"], ["https://api.example.com"], ["users"],
            1, 1, true, true, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Where(v => v.Severity == "Error").Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_DetectDeprecationIssue_When_DeprecatedOperationLacksNotice()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("oldOp", "oldOp", "Some operation", "GET", "/old", [], [], true)],
            [], ["bearer"], [], [],
            1, 0, true, true, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.OpenApi);

        violations.Should().Contain(v => v.RuleName == "DeprecationDocumentation");
    }
}
