using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.ValueObjects;

/// <summary>
/// Testes da assinatura digital e verificação de integridade de contratos.
/// Valida a criação, verificação e resistência a adulteração do conteúdo.
/// </summary>
public sealed class ContractSignatureTests
{
    private const string SampleJson = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{}}""";

    [Fact]
    public void Create_Should_GenerateSha256Fingerprint()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleJson, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        signature.Fingerprint.Should().NotBeNullOrWhiteSpace();
        signature.Algorithm.Should().Be("SHA-256");
        signature.SignedBy.Should().Be("admin@test.com");
        signature.Fingerprint.Should().HaveLength(64); // SHA-256 hex = 64 chars
    }

    [Fact]
    public void Verify_Should_ReturnTrue_WhenContentUnchanged()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleJson, "json");
        var signature = ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        var isValid = signature.Verify(canonical);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_WhenContentTampered()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleJson, "json");
        var signature = ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        var tampered = canonical.Replace("Test", "Hacked");
        var isValid = signature.Verify(tampered);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_WhenContentEmpty()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleJson, "json");
        var signature = ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        signature.Verify("").Should().BeFalse();
        signature.Verify("  ").Should().BeFalse();
    }

    [Fact]
    public void Create_Should_ProduceDeterministicFingerprint()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleJson, "json");
        var sig1 = ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);
        var sig2 = ContractSignature.Create(canonical, "admin", DateTimeOffset.UtcNow);

        sig1.Fingerprint.Should().Be(sig2.Fingerprint);
    }
}

/// <summary>
/// Testes da canonicalização de contratos para assinatura.
/// Garante que conteúdo semanticamente igual produz o mesmo hash,
/// eliminando diferenças de formatação, espaços e ordem de chaves.
/// </summary>
public sealed class ContractCanonicalizerTests
{
    [Fact]
    public void CanonicalizeJson_Should_SortKeys()
    {
        var json1 = """{"b":"2","a":"1"}""";
        var json2 = """{"a":"1","b":"2"}""";

        var canonical1 = ContractCanonicalizer.Canonicalize(json1, "json");
        var canonical2 = ContractCanonicalizer.Canonicalize(json2, "json");

        canonical1.Should().Be(canonical2);
    }

    [Fact]
    public void CanonicalizeJson_Should_RemoveWhitespace()
    {
        var compact = """{"a":"1","b":"2"}""";
        var pretty = """
        {
            "a": "1",
            "b": "2"
        }
        """;

        var canonical1 = ContractCanonicalizer.Canonicalize(compact, "json");
        var canonical2 = ContractCanonicalizer.Canonicalize(pretty, "json");

        canonical1.Should().Be(canonical2);
    }

    [Fact]
    public void CanonicalizeYaml_Should_NormalizeNewlines()
    {
        var unix = "openapi: 3.1.0\ninfo:\n  title: Test\n";
        var windows = "openapi: 3.1.0\r\ninfo:\r\n  title: Test\r\n";

        var canonical1 = ContractCanonicalizer.Canonicalize(unix, "yaml");
        var canonical2 = ContractCanonicalizer.Canonicalize(windows, "yaml");

        canonical1.Should().Be(canonical2);
    }

    [Fact]
    public void CanonicalizeXml_Should_TrimLines()
    {
        var xml = "<root>  \n  <child/>  \n</root>  ";
        var canonical = ContractCanonicalizer.Canonicalize(xml, "xml");

        canonical.Should().NotContain("  \n");
    }

    [Fact]
    public void Canonicalize_Should_ReturnEmpty_WhenContentIsEmpty()
    {
        ContractCanonicalizer.Canonicalize("", "json").Should().BeEmpty();
        ContractCanonicalizer.Canonicalize("  ", "json").Should().BeEmpty();
    }

    [Fact]
    public void CanonicalizeJson_Should_HandleNestedObjects()
    {
        var json1 = """{"b":{"d":"4","c":"3"},"a":"1"}""";
        var json2 = """{"a":"1","b":{"c":"3","d":"4"}}""";

        var canonical1 = ContractCanonicalizer.Canonicalize(json1, "json");
        var canonical2 = ContractCanonicalizer.Canonicalize(json2, "json");

        canonical1.Should().Be(canonical2);
    }

    [Fact]
    public void CanonicalizeJson_Should_FallbackToText_WhenInvalidJson()
    {
        var invalid = "not { valid json";
        var result = ContractCanonicalizer.Canonicalize(invalid, "json");
        result.Should().NotBeNullOrWhiteSpace();
    }
}

/// <summary>
/// Testes da proveniência (lineage) de contratos.
/// Valida a criação de proveniência para importações manuais e geradas por IA.
/// </summary>
public sealed class ContractProvenanceTests
{
    [Fact]
    public void ForImport_Should_SetFieldsCorrectly()
    {
        var provenance = ContractProvenance.ForImport(
            "upload", "openapi-3.1-json", "OpenApiSpecParser", "3.1.0", "admin@test.com");

        provenance.Origin.Should().Be("upload");
        provenance.OriginalFormat.Should().Be("openapi-3.1-json");
        provenance.ParserUsed.Should().Be("OpenApiSpecParser");
        provenance.StandardVersion.Should().Be("3.1.0");
        provenance.ImportedBy.Should().Be("admin@test.com");
        provenance.IsAiGenerated.Should().BeFalse();
        provenance.AiModelVersion.Should().BeNull();
    }

    [Fact]
    public void ForAiGeneration_Should_SetAiFields()
    {
        var provenance = ContractProvenance.ForAiGeneration(
            "OpenApiSpecParser", "3.1.0", "admin@test.com", "nexai-v1.0");

        provenance.Origin.Should().Be("ai-generated");
        provenance.IsAiGenerated.Should().BeTrue();
        provenance.AiModelVersion.Should().Be("nexai-v1.0");
    }

    [Fact]
    public void ForImport_Should_ThrowOnEmptyOrigin()
    {
        var act = () => ContractProvenance.ForImport("", "fmt", "parser", "1.0", "user");
        act.Should().Throw<ArgumentException>();
    }
}
