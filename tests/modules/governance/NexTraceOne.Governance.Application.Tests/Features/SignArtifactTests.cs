using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.SignArtifact;

namespace NexTraceOne.Governance.ArtifactSigning.Tests.Features;

/// <summary>
/// Testes unitários para a feature SignArtifact.
/// Cobre assinatura digital de artefatos usando Cosign.
/// </summary>
public sealed class SignArtifactTests
{
    private readonly IArtifactSigner _artifactSigner = Substitute.For<IArtifactSigner>();
    private readonly ISbomGenerator _sbomGenerator = Substitute.For<ISbomGenerator>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2026, 5, 13, 12, 0, 0, TimeSpan.Zero);

    public SignArtifactTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldSignArtifact_WhenValidRequest()
    {
        // Arrange
        var handler = new SignArtifact.Handler(_artifactSigner, _sbomGenerator, _clock);
        var command = new SignArtifact.Command(
            ArtifactPath: "/artifacts/myapp-v1.0.0.tar.gz",
            ArtifactType: "docker-image",
            Version: "1.0.0"
        );

        var sbomDoc = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: "https://spdx.org/spdxdocs/test",
            Package: new SbomPackage("SPDXRef-Package", "myapp", "1.0.0", "NOASSERTION", "false", "NOASSERTION", "NOASSERTION"),
            Dependencies: new List<SbomPackage>(),
            Relationships: new List<SbomRelationship>(),
            Created: FixedNow.UtcDateTime,
            Creator: new SbomCreator("NexTraceOne-SBOM-Generator", "MyCompany"),
            Metadata: new Dictionary<string, string>()
        );

        var signedArtifact = new SignedArtifactResult(
            ArtifactId: "artifact-123",
            ArtifactName: "myapp",
            ArtifactType: "docker-image",
            Version: "1.0.0",
            Checksum: "sha256:abc123...",
            Signature: "MEUCIQDxyz...",
            SignedAt: FixedNow.UtcDateTime,
            SignerIdentity: "cosign-signer@example.com",
            CertificateSubject: "CN=Cosign Signing Key",
            IsValid: true,
            ExpiryDate: FixedNow.AddYears(1).UtcDateTime,
            Metadata: new Dictionary<string, string>(),
            SbomDocument: sbomDoc,
            TransparencyLogEntry: "rekor-entry-456"
        );

        _artifactSigner.SignArtifactAsync(Arg.Any<SigningRequest>()).Returns(signedArtifact);
        _sbomGenerator.ExportSbomToJsonAsync(Arg.Any<SbomDocument>()).Returns("{\"spdxVersion\":\"SPDX-2.3\"}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ArtifactId.Should().Be("artifact-123");
        result.Value.ArtifactName.Should().Be("myapp");
        result.Value.Checksum.Should().Be("sha256:abc123...");
        result.Value.Signature.Should().Be("MEUCIQDxyz...");
        result.Value.SignedAt.Should().Be(FixedNow.UtcDateTime);
        result.Value.SignerIdentity.Should().Be("cosign-signer@example.com");
        result.Value.SbomJson.Should().Contain("SPDX-2.3");
        result.Value.TransparencyLogEntry.Should().Be("rekor-entry-456");

        await _artifactSigner.Received(1).SignArtifactAsync(Arg.Any<SigningRequest>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSigningFails()
    {
        // Arrange
        var handler = new SignArtifact.Handler(_artifactSigner, _sbomGenerator, _clock);
        var command = new SignArtifact.Command(
            ArtifactPath: "/artifacts/myapp-v1.0.0.tar.gz",
            ArtifactType: "docker-image",
            Version: "1.0.0"
        );

        _artifactSigner.SignArtifactAsync(Arg.Any<SigningRequest>())
            .Returns(Task.FromException<SignedArtifactResult>(new InvalidOperationException("chave privada inválida")));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("artifact.signing_failed");
        result.Error.Message.Should().Contain("Falha ao assinar artefato");
    }

    [Theory]
    [InlineData("docker-image")]
    [InlineData("nuget-package")]
    [InlineData("binary")]
    public async Task Handle_ShouldSupportMultipleArtifactTypes(string artifactType)
    {
        // Arrange
        var handler = new SignArtifact.Handler(_artifactSigner, _sbomGenerator, _clock);
        var command = new SignArtifact.Command(
            ArtifactPath: $"/artifacts/test.{artifactType}",
            ArtifactType: artifactType,
            Version: "1.0.0"
        );

        var sbomDoc = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: "https://spdx.org/spdxdocs/test",
            Package: new SbomPackage("SPDXRef-Package", "test", "1.0.0", "NOASSERTION", "false", "NOASSERTION", "NOASSERTION"),
            Dependencies: new List<SbomPackage>(),
            Relationships: new List<SbomRelationship>(),
            Created: FixedNow.UtcDateTime,
            Creator: new SbomCreator("NexTraceOne-SBOM-Generator", "TestOrg"),
            Metadata: new Dictionary<string, string>()
        );

        var signedArtifact = new SignedArtifactResult(
            ArtifactId: "artifact-456",
            ArtifactName: "test",
            ArtifactType: artifactType,
            Version: "1.0.0",
            Checksum: "sha256:def456...",
            Signature: "SIG123",
            SignedAt: FixedNow.UtcDateTime,
            SignerIdentity: "test-signer",
            CertificateSubject: "CN=Test Key",
            IsValid: true,
            ExpiryDate: null,
            Metadata: new Dictionary<string, string>(),
            SbomDocument: sbomDoc,
            TransparencyLogEntry: null
        );

        _artifactSigner.SignArtifactAsync(Arg.Any<SigningRequest>()).Returns(signedArtifact);
        _sbomGenerator.ExportSbomToJsonAsync(Arg.Any<SbomDocument>()).Returns("{}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ArtifactName.Should().Be("test");
    }
}
