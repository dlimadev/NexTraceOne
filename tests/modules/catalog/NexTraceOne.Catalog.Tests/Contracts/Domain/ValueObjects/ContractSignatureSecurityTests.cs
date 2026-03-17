using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.ValueObjects;

public sealed class ContractSignatureSecurityTests
{
    private const string SampleSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{}}""";

    [Fact]
    public void Verify_WithCorrectContent_ReturnsTrue()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleSpec, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        signature.Verify(canonical).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectContent_ReturnsFalse()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleSpec, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        signature.Verify("completely different content").Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyContent_ReturnsFalse()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleSpec, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        signature.Verify("").Should().BeFalse();
    }

    [Fact]
    public void Verify_WithNullContent_ReturnsFalse()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleSpec, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        signature.Verify(null!).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithSimilarContent_ReturnsFalse()
    {
        var canonical = ContractCanonicalizer.Canonicalize(SampleSpec, "json");
        var signature = ContractSignature.Create(canonical, "admin@test.com", DateTimeOffset.UtcNow);

        // Altera um único caractere — "Test" → "Tesu"
        var similar = canonical.Length > 0
            ? canonical[..^1] + (char)(canonical[^1] + 1)
            : "x";

        signature.Verify(similar).Should().BeFalse();
    }
}
