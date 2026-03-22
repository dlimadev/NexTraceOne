using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NexTraceOne.CLI.Models;
using NexTraceOne.CLI.Services;

namespace NexTraceOne.CLI.Tests.Services;

/// <summary>
/// Testes abrangentes do ContractValidator.
/// Valida todas as regras de validação, cálculo de sumário e modo strict.
/// </summary>
public sealed class ContractValidatorTests
{
    private static ContractManifest CreateValidManifest() => new()
    {
        Name = "orders-api",
        Version = "1.0.0",
        Type = "rest-api",
        Description = "Order management REST API",
        Owner = "team-commerce",
        Endpoints =
        [
            new ContractEndpoint { Path = "/api/orders", Method = "GET", Summary = "List orders" },
            new ContractEndpoint { Path = "/api/orders/{id}", Method = "POST", Summary = "Create order" }
        ],
        Schema = new ContractSchema { Format = "openapi-3.1", Content = "{}" }
    };

    // --- Required field validations ---

    [Fact]
    public void Validate_ValidManifest_ReturnsNoIssues()
    {
        var manifest = CreateValidManifest();

        var issues = ContractValidator.Validate(manifest);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Name = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI001" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Name = "  " };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI001");
    }

    [Fact]
    public void Validate_MissingVersion_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Version = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI002" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_MissingType_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Type = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI003" && i.Severity == ValidationSeverity.Error);
    }

    // --- Type validation ---

    [Theory]
    [InlineData("rest-api")]
    [InlineData("soap")]
    [InlineData("event-contract")]
    [InlineData("background-service")]
    public void Validate_ValidType_ReturnsNoTypeError(string type)
    {
        var manifest = CreateValidManifest() with { Type = type };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().NotContain(i => i.RuleId == "CLI004");
    }

    [Fact]
    public void Validate_InvalidType_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Type = "grpc" };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI004" && i.Severity == ValidationSeverity.Error)
            .Which.Message.Should().Contain("grpc");
    }

    // --- Version / semver validation ---

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.0-beta")]
    [InlineData("0.0.1+build.123")]
    [InlineData("10.20.30-alpha.1+sha.abc")]
    public void Validate_ValidSemver_ReturnsNoVersionError(string version)
    {
        var manifest = CreateValidManifest() with { Version = version };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().NotContain(i => i.RuleId == "CLI005");
    }

    [Theory]
    [InlineData("v1.0")]
    [InlineData("1.0")]
    [InlineData("latest")]
    [InlineData("1")]
    public void Validate_InvalidSemver_ReturnsError(string version)
    {
        var manifest = CreateValidManifest() with { Version = version };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI005" && i.Severity == ValidationSeverity.Error);
    }

    // --- Endpoint validations ---

    [Fact]
    public void Validate_EndpointMissingPath_ReturnsError()
    {
        var manifest = CreateValidManifest() with
        {
            Endpoints = [new ContractEndpoint { Path = null, Method = "GET" }]
        };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI006" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_EndpointMissingMethod_ReturnsError()
    {
        var manifest = CreateValidManifest() with
        {
            Endpoints = [new ContractEndpoint { Path = "/api/test", Method = null }]
        };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI007" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_EndpointNonStandardMethod_ReturnsWarning()
    {
        var manifest = CreateValidManifest() with
        {
            Endpoints = [new ContractEndpoint { Path = "/api/test", Method = "PURGE" }]
        };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI008" && i.Severity == ValidationSeverity.Warning);
    }

    // --- Schema validation ---

    [Fact]
    public void Validate_SchemaMissingFormat_ReturnsWarning()
    {
        var manifest = CreateValidManifest() with
        {
            Schema = new ContractSchema { Format = null, Content = "{}" }
        };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().ContainSingle(i => i.RuleId == "CLI009" && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_NoSchema_ReturnsNoSchemaWarning()
    {
        var manifest = CreateValidManifest() with { Schema = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().NotContain(i => i.RuleId == "CLI009");
    }

    // --- Null manifest ---

    [Fact]
    public void Validate_NullManifest_ThrowsArgumentNullException()
    {
        var act = () => ContractValidator.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // --- No endpoints ---

    [Fact]
    public void Validate_NoEndpoints_ReturnsNoEndpointErrors()
    {
        var manifest = CreateValidManifest() with { Endpoints = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().NotContain(i =>
            i.RuleId == "CLI006" || i.RuleId == "CLI007" || i.RuleId == "CLI008");
    }

    // --- Summary calculation ---

    [Fact]
    public void ValidationSummary_FromIssues_CalculatesCorrectCounts()
    {
        var manifest = new ContractManifest
        {
            Name = null,
            Version = "bad",
            Type = "invalid",
            Endpoints = [new ContractEndpoint { Path = "/api", Method = "PURGE" }],
            Schema = new ContractSchema { Format = null }
        };

        var issues = ContractValidator.Validate(manifest);
        var summary = ValidationSummary.FromIssues(issues);

        summary.TotalIssues.Should().Be(issues.Count);
        summary.ErrorCount.Should().Be(issues.Where(i => i.Severity == ValidationSeverity.Error).Count());
        summary.WarningCount.Should().Be(issues.Where(i => i.Severity == ValidationSeverity.Warning).Count());
        summary.ErrorCount.Should().BeGreaterThan(0);
        summary.WarningCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidationSummary_FromEmptyIssues_ReturnsAllZero()
    {
        var issues = ContractValidator.Validate(CreateValidManifest());
        var summary = ValidationSummary.FromIssues(issues);

        summary.TotalIssues.Should().Be(0);
        summary.ErrorCount.Should().Be(0);
        summary.WarningCount.Should().Be(0);
        summary.InfoCount.Should().Be(0);
        summary.HintCount.Should().Be(0);
        summary.BlockedCount.Should().Be(0);
    }

    // --- Strict mode behavior ---

    [Fact]
    public void StrictMode_WarningsOnly_ShouldBeConsideredFailure()
    {
        var manifest = CreateValidManifest() with
        {
            Schema = new ContractSchema { Format = null }
        };

        var issues = ContractValidator.Validate(manifest);
        var summary = ValidationSummary.FromIssues(issues);

        var hasErrors = summary.ErrorCount > 0 || summary.BlockedCount > 0;
        var hasWarnings = summary.WarningCount > 0;
        var strictFailed = hasErrors || hasWarnings;

        hasErrors.Should().BeFalse("only warnings expected");
        hasWarnings.Should().BeTrue("schema missing format produces a warning");
        strictFailed.Should().BeTrue("strict mode treats warnings as errors");
    }

    [Fact]
    public void StrictMode_NoIssues_ShouldPass()
    {
        var issues = ContractValidator.Validate(CreateValidManifest());
        var summary = ValidationSummary.FromIssues(issues);

        var strictFailed = summary.ErrorCount > 0 || summary.BlockedCount > 0 || summary.WarningCount > 0;

        strictFailed.Should().BeFalse();
    }

    // --- Multiple issues ---

    [Fact]
    public void Validate_MultipleFieldsMissing_ReturnsMultipleErrors()
    {
        var manifest = new ContractManifest();

        var issues = ContractValidator.Validate(manifest);

        issues.Should().HaveCountGreaterThanOrEqualTo(3, "name, version, and type are all required");
        issues.Should().Contain(i => i.RuleId == "CLI001");
        issues.Should().Contain(i => i.RuleId == "CLI002");
        issues.Should().Contain(i => i.RuleId == "CLI003");
    }
}
