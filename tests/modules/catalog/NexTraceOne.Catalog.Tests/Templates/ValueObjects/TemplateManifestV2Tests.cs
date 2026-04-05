using NexTraceOne.Catalog.Domain.Templates.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Templates.ValueObjects;

public class TemplateManifestV2Tests
{
    private static string ValidManifestJson() => """
        {
            "Version": "2.0",
            "Architecture": {
                "Pattern": "Clean Architecture",
                "Style": "Modular Monolith",
                "Layers": ["Domain", "Application", "Infrastructure", "API"],
                "Description": "DDD-based clean architecture"
            },
            "Stack": {
                "Runtime": ".NET 10",
                "Language": "C#",
                "Framework": "ASP.NET Core"
            },
            "Folders": [
                { "Path": "src/Domain", "Purpose": "Domain entities and value objects", "IsRequired": true }
            ],
            "RequiredDependencies": [
                { "Name": "Ardalis.GuardClauses", "MinVersion": "4.0.0", "Ecosystem": "NuGet", "Reason": "Guard clauses for domain validation" }
            ],
            "QualityGates": {
                "TestCoverageMinimum": 80,
                "RequireUnitTests": true,
                "RequireIntegrationTests": false,
                "RequireOpenApiSpec": true
            }
        }
        """;

    [Fact]
    public void Parse_ValidJson_ReturnsManifest()
    {
        var manifest = TemplateManifestV2.Parse(ValidManifestJson());

        manifest.Should().NotBeNull();
        manifest.Version.Should().Be("2.0");
        manifest.Architecture.Pattern.Should().Be("Clean Architecture");
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsInvalidOperationException()
    {
        var action = () => TemplateManifestV2.Parse("{invalid json}");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_NullJson_ThrowsArgumentException()
    {
        var action = () => TemplateManifestV2.Parse(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_EmptyJson_ThrowsArgumentException()
    {
        var action = () => TemplateManifestV2.Parse(string.Empty);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_MissingArchitecturePattern_ThrowsInvalidOperationException()
    {
        var json = """{"Version":"2.0","Architecture":{"Pattern":"","Style":""},"Stack":{},"QualityGates":{"TestCoverageMinimum":80}}""";
        var action = () => TemplateManifestV2.Parse(json);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Architecture.Pattern*");
    }

    [Fact]
    public void TryParse_ValidJson_ReturnsManifest()
    {
        var manifest = TemplateManifestV2.TryParse(ValidManifestJson());

        manifest.Should().NotBeNull();
        manifest!.Architecture.Pattern.Should().Be("Clean Architecture");
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsNull()
    {
        var manifest = TemplateManifestV2.TryParse("{not valid json{{");
        manifest.Should().BeNull();
    }

    [Fact]
    public void TryParse_NullInput_ReturnsNull()
    {
        var manifest = TemplateManifestV2.TryParse(null);
        manifest.Should().BeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsNull()
    {
        var manifest = TemplateManifestV2.TryParse(string.Empty);
        manifest.Should().BeNull();
    }

    [Fact]
    public void IsValid_ValidJson_ReturnsTrue()
    {
        TemplateManifestV2.IsValid(ValidManifestJson()).Should().BeTrue();
    }

    [Fact]
    public void IsValid_InvalidJson_ReturnsFalse()
    {
        TemplateManifestV2.IsValid("{bad}").Should().BeFalse();
    }

    [Fact]
    public void IsValid_NullInput_ReturnsFalse()
    {
        TemplateManifestV2.IsValid(null).Should().BeFalse();
    }

    [Fact]
    public void QualityGates_TestCoverageMinimum_BelowZero_FailsValidation()
    {
        var json = """{"Version":"2.0","Architecture":{"Pattern":"Clean"},"Stack":{},"QualityGates":{"TestCoverageMinimum":-1}}""";
        TemplateManifestV2.IsValid(json).Should().BeFalse();
    }

    [Fact]
    public void QualityGates_TestCoverageMinimum_Above100_FailsValidation()
    {
        var json = """{"Version":"2.0","Architecture":{"Pattern":"Clean"},"Stack":{},"QualityGates":{"TestCoverageMinimum":101}}""";
        TemplateManifestV2.IsValid(json).Should().BeFalse();
    }

    [Fact]
    public void QualityGates_TestCoverageMinimum_Zero_IsValid()
    {
        var json = """{"Version":"2.0","Architecture":{"Pattern":"Clean"},"Stack":{},"QualityGates":{"TestCoverageMinimum":0}}""";
        TemplateManifestV2.IsValid(json).Should().BeTrue();
    }

    [Fact]
    public void QualityGates_TestCoverageMinimum_100_IsValid()
    {
        var json = """{"Version":"2.0","Architecture":{"Pattern":"Clean"},"Stack":{},"QualityGates":{"TestCoverageMinimum":100}}""";
        TemplateManifestV2.IsValid(json).Should().BeTrue();
    }

    [Fact]
    public void ToJson_RoundTrip_PreservesValues()
    {
        var manifest = TemplateManifestV2.Parse(ValidManifestJson());
        var json = manifest.ToJson();
        var reparsed = TemplateManifestV2.Parse(json);

        reparsed.Version.Should().Be(manifest.Version);
        reparsed.Architecture.Pattern.Should().Be(manifest.Architecture.Pattern);
        reparsed.QualityGates.TestCoverageMinimum.Should().Be(manifest.QualityGates.TestCoverageMinimum);
    }

    [Fact]
    public void ValidateOrThrow_ValidManifest_DoesNotThrow()
    {
        var manifest = TemplateManifestV2.Parse(ValidManifestJson());
        var action = () => TemplateManifestV2.ValidateOrThrow(manifest);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrThrow_NullManifest_ThrowsArgumentNullException()
    {
        var action = () => TemplateManifestV2.ValidateOrThrow(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_Folders_MapsCorrectly()
    {
        var manifest = TemplateManifestV2.Parse(ValidManifestJson());
        manifest.Folders.Should().HaveCount(1);
        manifest.Folders[0].Path.Should().Be("src/Domain");
        manifest.Folders[0].IsRequired.Should().BeTrue();
    }

    [Fact]
    public void Parse_RequiredDependencies_MapsCorrectly()
    {
        var manifest = TemplateManifestV2.Parse(ValidManifestJson());
        manifest.RequiredDependencies.Should().HaveCount(1);
        manifest.RequiredDependencies[0].Name.Should().Be("Ardalis.GuardClauses");
        manifest.RequiredDependencies[0].Ecosystem.Should().Be("NuGet");
    }
}
