using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GenerateSbom;

namespace NexTraceOne.Governance.ArtifactSigning.Tests.Features;

/// <summary>
/// Testes unitários para a feature GenerateSbom.
/// Cobre geração de Software Bill of Materials (SBOM) em formato SPDX 2.3.
/// </summary>
public sealed class GenerateSbomTests
{
    private readonly ISbomGenerator _sbomGenerator = Substitute.For<ISbomGenerator>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2026, 5, 13, 12, 0, 0, TimeSpan.Zero);

    public GenerateSbomTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldGenerateSpdxSbom_ForProject()
    {
        // Arrange
        var handler = new GenerateSbom.Handler(_sbomGenerator, _clock);
        var command = new GenerateSbom.Command(ProjectPath: "/projects/myapp");

        var sbomDoc = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: "https://spdx.org/spdxdocs/myapp-1.0.0-abc123",
            Package: new SbomPackage("SPDXRef-Package", "myapp", "1.0.0", "NOASSERTION", "false", "MIT", "Copyright 2026"),
            Dependencies: new List<SbomPackage>
            {
                new SbomPackage("SPDXRef-Dep1", "Newtonsoft.Json", "13.0.3", "https://nuget.org/packages/Newtonsoft.Json/13.0.3", "false", "MIT", ""),
                new SbomPackage("SPDXRef-Dep2", "FluentValidation", "11.9.0", "https://nuget.org/packages/FluentValidation/11.9.0", "false", "Apache-2.0", "")
            },
            Relationships: new List<SbomRelationship>
            {
                new SbomRelationship("SPDXRef-Package", "SPDXRef-Dep1", "DEPENDS_ON"),
                new SbomRelationship("SPDXRef-Package", "SPDXRef-Dep2", "DEPENDS_ON")
            },
            Created: FixedNow.UtcDateTime,
            Creator: new SbomCreator("NexTraceOne-SBOM-Generator-v1.0", "MyCompany"),
            Metadata: new Dictionary<string, string> { { "tool", "cosign" } }
        );

        var sbomJson = "{\"spdxVersion\":\"SPDX-2.3\",\"name\":\"myapp\"}";

        _sbomGenerator.GenerateSbomAsync("/projects/myapp").Returns(sbomDoc);
        _sbomGenerator.ExportSbomToJsonAsync(sbomDoc).Returns(sbomJson);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SbomJson.Should().Contain("\"spdxVersion\":\"SPDX-2.3\"");
        result.Value.SbomJson.Should().Contain("\"name\":\"myapp\"");
        result.Value.GeneratedAt.Should().Be(FixedNow.UtcDateTime);
        result.Value.DependencyCount.Should().Be(2); // 2 dependências no array Dependencies

        await _sbomGenerator.Received(1).GenerateSbomAsync("/projects/myapp");
        await _sbomGenerator.Received(1).ExportSbomToJsonAsync(Arg.Any<SbomDocument>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSbomGenerationFails()
    {
        // Arrange
        var handler = new GenerateSbom.Handler(_sbomGenerator, _clock);
        var command = new GenerateSbom.Command(ProjectPath: "/invalid/path");

        _sbomGenerator.GenerateSbomAsync("/invalid/path")
            .Returns(Task.FromException<SbomDocument>(new InvalidOperationException("projeto não encontrado")));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("sbom.generation_failed");
        result.Error.Message.Should().Contain("Falha ao gerar SBOM");
    }

    [Fact]
    public async Task Handle_ShouldIncludeDependencies_WhenPresent()
    {
        // Arrange
        var handler = new GenerateSbom.Handler(_sbomGenerator, _clock);
        var command = new GenerateSbom.Command(ProjectPath: "/projects/myapp");

        var sbomDoc = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: "https://spdx.org/spdxdocs/test",
            Package: new SbomPackage("SPDXRef-Package", "test-app", "1.0.0", "NOASSERTION", "false", "MIT", ""),
            Dependencies: new List<SbomPackage>
            {
                new SbomPackage("SPDXRef-Dep1", "Dependency1", "1.0.0", "NOASSERTION", "false", "MIT", ""),
                new SbomPackage("SPDXRef-Dep2", "Dependency2", "2.0.0", "NOASSERTION", "false", "Apache-2.0", ""),
                new SbomPackage("SPDXRef-Dep3", "Dependency3", "3.0.0", "NOASSERTION", "false", "GPL-3.0", "")
            },
            Relationships: new List<SbomRelationship>(),
            Created: FixedNow.UtcDateTime,
            Creator: new SbomCreator("TestTool", "TestOrg"),
            Metadata: new Dictionary<string, string>()
        );

        var sbomJson = "{\"packages\":[...],\"relationships\":[...]}";

        _sbomGenerator.GenerateSbomAsync("/projects/myapp").Returns(sbomDoc);
        _sbomGenerator.ExportSbomToJsonAsync(sbomDoc).Returns(sbomJson);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DependencyCount.Should().Be(3); // 3 dependências no array Dependencies
        result.Value.SbomJson.Should().Contain("packages");
    }

    [Fact]
    public async Task Handle_ShouldIncludeTimestamps()
    {
        // Arrange
        var handler = new GenerateSbom.Handler(_sbomGenerator, _clock);
        var command = new GenerateSbom.Command(ProjectPath: "/projects/myapp");

        var sbomDoc = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: "https://spdx.org/spdxdocs/timestamp-test",
            Package: new SbomPackage("SPDXRef-Package", "timestamp-app", "1.0.0", "NOASSERTION", "false", "MIT", ""),
            Dependencies: new List<SbomPackage>(),
            Relationships: new List<SbomRelationship>(),
            Created: FixedNow.UtcDateTime,
            Creator: new SbomCreator("TimestampTool", "TestOrg"),
            Metadata: new Dictionary<string, string>()
        );

        var sbomJson = $"{{\"created\":\"{FixedNow.UtcDateTime:O}\"}}";

        _sbomGenerator.GenerateSbomAsync("/projects/myapp").Returns(sbomDoc);
        _sbomGenerator.ExportSbomToJsonAsync(sbomDoc).Returns(sbomJson);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SbomJson.Should().Contain("created");
        result.Value.GeneratedAt.Should().Be(FixedNow.UtcDateTime);
    }
}
