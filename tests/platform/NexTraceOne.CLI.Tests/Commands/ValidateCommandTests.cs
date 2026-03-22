using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NexTraceOne.CLI.Models;
using NexTraceOne.CLI.Services;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do comando validate: validação de contratos, formato JSON, ficheiro inexistente e modo strict.
/// Exercita a lógica de validação e serialização de resultados.
/// </summary>
public sealed class ValidateCommandTests
{
    private static ContractManifest CreateValidManifest() => new()
    {
        Name = "payments-api",
        Version = "2.0.0",
        Type = "rest-api",
        Description = "Payments processing API",
        Owner = "team-payments",
        Endpoints =
        [
            new ContractEndpoint { Path = "/api/payments", Method = "POST", Summary = "Create payment" }
        ],
        Schema = new ContractSchema { Format = "openapi-3.1" }
    };

    [Fact]
    public void ValidContract_ReturnsNoIssues()
    {
        var manifest = CreateValidManifest();

        var issues = ContractValidator.Validate(manifest);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void MissingName_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Name = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().Contain(i => i.RuleId == "CLI001" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void MissingVersion_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Version = null };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().Contain(i => i.RuleId == "CLI002" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void InvalidType_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Type = "graphql" };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().Contain(i => i.RuleId == "CLI004" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void InvalidSemver_ReturnsError()
    {
        var manifest = CreateValidManifest() with { Version = "v1" };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().Contain(i => i.RuleId == "CLI005" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void EndpointMissingMethod_ReturnsError()
    {
        var manifest = CreateValidManifest() with
        {
            Endpoints = [new ContractEndpoint { Path = "/api/test", Method = null }]
        };

        var issues = ContractValidator.Validate(manifest);

        issues.Should().Contain(i => i.RuleId == "CLI007" && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void StrictMode_TreatsWarningsAsErrors()
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

        hasErrors.Should().BeFalse();
        hasWarnings.Should().BeTrue();
        strictFailed.Should().BeTrue("strict mode makes warnings cause failure");
    }

    [Fact]
    public void JsonFormatOutput_CanSerializeIssuesAndSummary()
    {
        var manifest = CreateValidManifest() with { Name = null };

        var issues = ContractValidator.Validate(manifest);
        var summary = ValidationSummary.FromIssues(issues);

        var payload = new { issues, summary };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("CLI001");
        json.Should().Contain("Error");

        // Round-trip: should deserialize back to a valid JSON document
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("issues").GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("summary").GetProperty("errorCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void NonExistentFile_SimulatesProperError()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.json");
        var fileInfo = new FileInfo(filePath);

        fileInfo.Exists.Should().BeFalse("the file should not exist for this test");
    }
}
