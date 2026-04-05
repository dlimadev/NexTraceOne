using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes de ContractRuleViolation e ContractArtifact.
/// Valida criação, guarda de parâmetros e imutabilidade dos artefatos gerados.
/// </summary>
public sealed class ContractEntitiesTests
{
    [Fact]
    public void ContractRuleViolation_Create_Should_SetAllFields()
    {
        var versionId = ContractVersionId.New();
        var rulesetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var violation = ContractRuleViolation.Create(
            versionId, rulesetId, "naming-convention", "Error",
            "Path names must use kebab-case", "/paths/getUsers", now,
            "Rename to /paths/get-users");

        violation.ContractVersionId.Should().Be(versionId);
        violation.RulesetId.Should().Be(rulesetId);
        violation.RuleName.Should().Be("naming-convention");
        violation.Severity.Should().Be("Error");
        violation.Message.Should().Be("Path names must use kebab-case");
        violation.Path.Should().Be("/paths/getUsers");
        violation.SuggestedFix.Should().Be("Rename to /paths/get-users");
        violation.DetectedAt.Should().Be(now);
    }

    [Fact]
    public void ContractRuleViolation_Create_Should_AllowNullSuggestedFix()
    {
        var violation = ContractRuleViolation.Create(
            ContractVersionId.New(), Guid.NewGuid(), "required-examples", "Warning",
            "Operation should have examples", "/paths/~1users/get", DateTimeOffset.UtcNow);

        violation.SuggestedFix.Should().BeNull();
    }

    [Fact]
    public void ContractArtifact_Create_Should_SetAllFields()
    {
        var versionId = ContractVersionId.New();
        var now = DateTimeOffset.UtcNow;

        var artifact = ContractArtifact.Create(
            versionId,
            ContractArtifactType.ProviderConformanceTest,
            "user-service-tests.cs",
            "public class UserServiceTests { }",
            "csharp",
            "admin@test.com",
            now,
            isAiGenerated: true);

        artifact.ContractVersionId.Should().Be(versionId);
        artifact.ArtifactType.Should().Be(ContractArtifactType.ProviderConformanceTest);
        artifact.Name.Should().Be("user-service-tests.cs");
        artifact.Content.Should().Contain("UserServiceTests");
        artifact.ContentFormat.Should().Be("csharp");
        artifact.IsAiGenerated.Should().BeTrue();
        artifact.GeneratedBy.Should().Be("admin@test.com");
        artifact.GeneratedAt.Should().Be(now);
    }

    [Fact]
    public void ContractArtifact_Create_Should_DefaultIsAiGeneratedToFalse()
    {
        var artifact = ContractArtifact.Create(
            ContractVersionId.New(),
            ContractArtifactType.Documentation,
            "api-docs.md",
            "# API Docs",
            "markdown",
            "admin",
            DateTimeOffset.UtcNow);

        artifact.IsAiGenerated.Should().BeFalse();
    }

    [Fact]
    public void ContractVersion_AddRuleViolation_Should_AddToCollection()
    {
        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0"}""", "json", "upload").Value;

        var violation = ContractRuleViolation.Create(
            contract.Id, Guid.NewGuid(), "test-rule", "Warning",
            "Test message", "/test", DateTimeOffset.UtcNow);

        contract.AddRuleViolation(violation);
        contract.RuleViolations.Should().HaveCount(1);
        contract.RuleViolations[0].Should().Be(violation);
    }

    [Fact]
    public void ContractVersion_AddArtifact_Should_AddToCollection()
    {
        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0"}""", "json", "upload").Value;

        var artifact = ContractArtifact.Create(
            contract.Id,
            ContractArtifactType.Changelog,
            "changelog.md",
            "# v1.0.0 Initial release",
            "markdown",
            "system",
            DateTimeOffset.UtcNow);

        contract.AddArtifact(artifact);
        contract.Artifacts.Should().HaveCount(1);
    }

    [Fact]
    public void ContractVersion_Sign_Should_SetSignature_WhenApproved()
    {
        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0"}""", "json", "upload").Value;

        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);

        var canonical = ContractCanonicalizer.Canonicalize(
            contract.SpecContent, contract.Format);
        var signature = ContractSignature.Create(
            canonical, "admin", DateTimeOffset.UtcNow);

        var result = contract.Sign(signature);
        result.IsSuccess.Should().BeTrue();
        contract.Signature.Should().NotBeNull();
        contract.Signature!.Fingerprint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ContractVersion_Sign_Should_Fail_WhenDraft()
    {
        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0"}""", "json", "upload").Value;

        var signature = ContractSignature.Create(
            "content", "admin", DateTimeOffset.UtcNow);

        var result = contract.Sign(signature);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Signing.InvalidState");
    }

    [Fact]
    public void ContractVersion_SetProvenance_Should_SetProvenanceField()
    {
        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0"}""", "json", "upload").Value;

        var provenance = ContractProvenance.ForImport(
            "upload", "openapi-3.1-json", "OpenApiSpecParser", "3.1.0", "admin");

        contract.SetProvenance(provenance);
        contract.Provenance.Should().NotBeNull();
        contract.Provenance!.Origin.Should().Be("upload");
    }
}
