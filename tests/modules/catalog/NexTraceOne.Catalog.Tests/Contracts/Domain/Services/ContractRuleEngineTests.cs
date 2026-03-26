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

    [Fact]
    public void Evaluate_Should_NotRaiseSecurityViolation_For_WorkerServiceProtocol()
    {
        // WorkerService does not require security definitions — should not raise SecurityDefinition error
        var model = new ContractCanonicalModel(
            ContractProtocol.WorkerService, "OrderExpirationJob", "Cron", "0 * * * *",
            [new ContractOperation("OrderExpirationJob", "OrderExpirationJob", "Cron background service", "Cron", "OrderExpirationJob", [], [])],
            [], // No security schemes — expected for worker services
            [], [], ["Job"],
            1, 0, false, true, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.WorkerService);

        violations.Should().NotContain(v => v.RuleName == "SecurityDefinition");
    }

    [Fact]
    public void Evaluate_Should_RaiseWorkerOperationMissing_When_NoOperationsDeclared()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.WorkerService, "EmptyWorker", "OnDemand", null,
            [],
            [], [], [], [],
            0, 0, false, false, false);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.WorkerService);

        violations.Should().Contain(v => v.RuleName == "WorkerOperationMissing");
    }

    [Fact]
    public void Evaluate_Should_NotRaiseExtraViolations_For_FullyCompliantWorkerService()
    {
        // A well-defined WorkerService should have very low violation count
        var model = new ContractCanonicalModel(
            ContractProtocol.WorkerService, "OrderExpirationJob", "Cron", "0 * * * *",
            [new ContractOperation("OrderExpirationJob", "OrderExpirationJob", "Cron background service", "Cron", "OrderExpirationJob", [], [])],
            [new ContractSchemaElement("expiredCount", "int", false)],
            [], [], ["Job"],
            1, 1, false, true, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.WorkerService);

        violations.Where(v => v.Severity == "Error").Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_NotRaiseSecurityViolation_For_WsdlProtocol()
    {
        // WSDL also does not require OAuth/API Key security definitions — handled by WS-Security
        var model = new ContractCanonicalModel(
            ContractProtocol.Wsdl, "OrderService", "1.1", null,
            [new ContractOperation("GetOrder", "GetOrder", "Get order by id", "request-response", "OrderPort", [], [])],
            [], [], [], [],
            1, 0, false, false, true);

        var violations = ContractRuleEngine.Evaluate(ContractVersionId.New(), model, "1.0.0", ContractProtocol.Wsdl);

        violations.Should().NotContain(v => v.RuleName == "SecurityDefinition");
    }
}
