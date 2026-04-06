using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate ContractVersion.
/// </summary>
public sealed class ContractVersionTests
{
    private const string ValidSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    [Fact]
    public void Import_Should_CreateContractVersion_When_InputIsValid()
    {
        var apiAssetId = Guid.NewGuid();

        var result = ContractVersion.Import(apiAssetId, "1.0.0", ValidSpec, "json", "https://example.com/spec");

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.SemVer.Should().Be("1.0.0");
        result.Value.Format.Should().Be("json");
        result.Value.ImportedFrom.Should().Be("https://example.com/spec");
        result.Value.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Import_Should_ReturnFailure_When_SpecContentIsEmpty()
    {
        var result = ContractVersion.Import(Guid.NewGuid(), "1.0.0", string.Empty, "json", "upload");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.EmptySpecContent");
    }

    [Fact]
    public void Lock_Should_Succeed_When_VersionIsNotLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var lockedAt = new DateTimeOffset(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

        var result = version.Lock("admin", lockedAt);

        result.IsSuccess.Should().BeTrue();
        version.IsLocked.Should().BeTrue();
        version.LockedBy.Should().Be("admin");
        version.LockedAt.Should().Be(lockedAt);
    }

    [Fact]
    public void Lock_Should_ReturnFailure_When_VersionIsAlreadyLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var lockedAt = new DateTimeOffset(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

        version.Lock("admin", lockedAt);
        var secondResult = version.Lock("admin2", lockedAt);

        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyLocked");
    }

    // ── Assinatura digital ───────────────────────────────────────────────

    [Fact]
    public void Sign_Should_Succeed_When_ContractIsLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        version.Lock("admin", DateTimeOffset.UtcNow);

        var canonical = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(ValidSpec, "json");
        var signature = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        var result = version.Sign(signature);

        result.IsSuccess.Should().BeTrue();
        version.Signature.Should().NotBeNull();
        version.Signature!.SignedBy.Should().Be("admin");
    }

    [Fact]
    public void Sign_Should_CreateValidFingerprint()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        version.Lock("admin", DateTimeOffset.UtcNow);

        var canonical = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(ValidSpec, "json");
        var signature = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        version.Sign(signature);

        version.Signature!.Fingerprint.Should().NotBeNullOrEmpty();
        version.Signature.Fingerprint.Length.Should().BeGreaterThan(32);
        version.Signature.Algorithm.Should().Be("SHA-256");
    }

    [Fact]
    public void Sign_Should_UseCanonicalForm_NotRawContent()
    {
        const string specWithExtraSpaces = """  {"openapi":  "3.0.0", "info": {"title": "Test", "version": "1.0.0"}, "paths": {"/users": {"get": {"responses": {"200": {"description": "OK"}}}}}}  """;
        const string specNormalized = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

        var version1 = ContractVersion.Import(Guid.NewGuid(), "1.0.0", specWithExtraSpaces, "json", "upload").Value;
        var version2 = ContractVersion.Import(Guid.NewGuid(), "1.0.0", specNormalized, "json", "upload").Value;
        version1.Lock("admin", DateTimeOffset.UtcNow);
        version2.Lock("admin", DateTimeOffset.UtcNow);

        var canonical1 = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(specWithExtraSpaces, "json");
        var canonical2 = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(specNormalized, "json");
        var sig1 = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical1, "admin", DateTimeOffset.UtcNow);
        var sig2 = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical2, "admin", DateTimeOffset.UtcNow);

        version1.Sign(sig1);
        version2.Sign(sig2);

        // Ambos os fingerprints devem ser idênticos pois o conteúdo canonicalizado é igual
        version1.Signature!.Fingerprint.Should().Be(version2.Signature!.Fingerprint);
    }

    [Fact]
    public void IsSignedBy_Should_ReturnTrue_ForValidSignature()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        version.Lock("admin", DateTimeOffset.UtcNow);

        var canonical = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(ValidSpec, "json");
        var signature = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);
        version.Sign(signature);

        version.Signature!.SignedBy.Should().Be("admin");
        version.Signature.Verify(canonical).Should().BeTrue();
    }

    [Fact]
    public void IsSignedBy_Should_ReturnFalse_ForWrongUser()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        version.Lock("admin", DateTimeOffset.UtcNow);

        var canonical = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(ValidSpec, "json");
        var sig1 = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);
        var sig2 = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "other-user", DateTimeOffset.UtcNow);
        version.Sign(sig1);

        version.Signature!.SignedBy.Should().NotBe(sig2.SignedBy);
    }

    [Fact]
    public void Sign_Should_Fail_When_ContractIsNotLockedOrApproved()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;

        var canonical = NexTraceOne.Catalog.Domain.Contracts.Services.ContractCanonicalizer.Canonicalize(ValidSpec, "json");
        var signature = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        var result = version.Sign(signature);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Signing.InvalidState");
    }

    // ── Violations ───────────────────────────────────────────────────────

    [Fact]
    public void AddRuleViolation_Should_AddViolationToCollection()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var violation = NexTraceOne.Catalog.Domain.Contracts.Entities.ContractRuleViolation.Create(
            version.Id, null, "naming-convention", "Warning", "Path should use kebab-case", "/paths", DateTimeOffset.UtcNow);

        version.AddRuleViolation(violation);

        version.RuleViolations.Should().HaveCount(1);
        version.RuleViolations[0].RuleName.Should().Be("naming-convention");
        version.RuleViolations[0].Severity.Should().Be("Warning");
    }

    [Fact]
    public void AddRuleViolation_Should_AccumulateMultipleViolations()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var v1 = NexTraceOne.Catalog.Domain.Contracts.Entities.ContractRuleViolation.Create(
            version.Id, null, "rule-a", "Error", "Missing description", "/paths/users", DateTimeOffset.UtcNow);
        var v2 = NexTraceOne.Catalog.Domain.Contracts.Entities.ContractRuleViolation.Create(
            version.Id, null, "rule-b", "Warning", "No examples", "/paths/users/get", DateTimeOffset.UtcNow);

        version.AddRuleViolation(v1);
        version.AddRuleViolation(v2);

        version.RuleViolations.Should().HaveCount(2);
    }

    // ── Artifacts ────────────────────────────────────────────────────────

    [Fact]
    public void AddArtifact_Should_StoreArtifactCorrectly()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var artifact = NexTraceOne.Catalog.Domain.Contracts.Entities.ContractArtifact.Create(
            version.Id,
            NexTraceOne.Catalog.Domain.Contracts.Enums.ContractArtifactType.ServiceScaffold,
            "UserController.cs",
            "public class UserController {}",
            "csharp",
            "engineer@test.com",
            DateTimeOffset.UtcNow);

        version.AddArtifact(artifact);

        version.Artifacts.Should().HaveCount(1);
        version.Artifacts[0].Name.Should().Be("UserController.cs");
        version.Artifacts[0].ContentFormat.Should().Be("csharp");
        version.Artifacts[0].ArtifactType.Should().Be(NexTraceOne.Catalog.Domain.Contracts.Enums.ContractArtifactType.ServiceScaffold);
    }

    // ── Provenance ───────────────────────────────────────────────────────

    [Fact]
    public void SetProvenance_Should_RecordProvenanceCorrectly()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var provenance = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractProvenance.ForImport(
            "url", "openapi-3.0-json", "openapi-parser", "3.0.0", "engineer@test.com");

        version.SetProvenance(provenance);

        version.Provenance.Should().NotBeNull();
        version.Provenance!.Origin.Should().Be("url");
        version.Provenance.ParserUsed.Should().Be("openapi-parser");
        version.Provenance.ImportedBy.Should().Be("engineer@test.com");
        version.Provenance.IsAiGenerated.Should().BeFalse();
    }

    [Fact]
    public void SetProvenance_Should_SupportAiGeneratedContracts()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "ai-generated").Value;
        var provenance = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractProvenance.ForAiGeneration(
            "nto-ai-pipeline", "3.1.0", "engineer@test.com", "llama-3.1");

        version.SetProvenance(provenance);

        version.Provenance!.IsAiGenerated.Should().BeTrue();
        version.Provenance.AiModelVersion.Should().Be("llama-3.1");
        version.Provenance.Origin.Should().Be("ai-generated");
    }

    // ── SLA ──────────────────────────────────────────────────────────────

    [Fact]
    public void ClearSla_Should_RemoveSlaFromContract()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var sla = NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSla.Create(
            availabilityTarget: 99.9m, latencyP99Ms: 500, tier: "Gold");
        version.SetSla(sla);

        version.ClearSla();

        version.Sla.Should().BeNull();
    }

    // ── Import validation ────────────────────────────────────────────────

    [Fact]
    public void Import_Should_Fail_When_SemVerIsInvalid()
    {
        var result = ContractVersion.Import(Guid.NewGuid(), "not-a-semver", ValidSpec, "json", "upload");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.InvalidSemVer");
    }

    [Fact]
    public void Import_Should_SetDefaultProtocolToOpenApi()
    {
        var result = ContractVersion.Import(Guid.NewGuid(), "2.0.0", ValidSpec, "json", "upload");

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(NexTraceOne.Catalog.Domain.Contracts.Enums.ContractProtocol.OpenApi);
    }

    [Fact]
    public void Import_Should_SetProtocol_When_Specified()
    {
        var result = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0", "<definitions/>", "xml", "upload",
            NexTraceOne.Catalog.Domain.Contracts.Enums.ContractProtocol.Wsdl);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(NexTraceOne.Catalog.Domain.Contracts.Enums.ContractProtocol.Wsdl);
    }
}
