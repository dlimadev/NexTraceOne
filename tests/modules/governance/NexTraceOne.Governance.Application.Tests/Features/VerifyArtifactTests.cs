using FluentAssertions;
using NSubstitute;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.VerifyArtifact;

namespace NexTraceOne.Governance.ArtifactSigning.Tests.Features;

/// <summary>
/// Testes unitários para a feature VerifyArtifact.
/// Cobre verificação de assinaturas digitais de artefatos.
/// </summary>
public sealed class VerifyArtifactTests
{
    private readonly IArtifactSigner _artifactSigner = Substitute.For<IArtifactSigner>();

    [Fact]
    public async Task Handle_ShouldVerifyValidSignature()
    {
        // Arrange
        var handler = new VerifyArtifact.Handler(_artifactSigner);
        var command = new VerifyArtifact.Command(ArtifactId: "artifact-123");

        var verificationResult = new VerificationResult(
            IsValid: true,
            ArtifactId: "artifact-123",
            VerifiedAt: DateTime.UtcNow,
            SignerIdentity: "cosign-signer@example.com",
            Errors: new List<string>(),
            Warnings: new List<string>()
        );

        _artifactSigner.VerifyArtifactAsync("artifact-123").Returns(verificationResult);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.SignerIdentity.Should().Be("cosign-signer@example.com");
        result.Value.VerifiedAt.Should().NotBeNull();
        result.Value.Errors.Should().BeEmpty();

        await _artifactSigner.Received(1).VerifyArtifactAsync("artifact-123");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSignatureIsInvalid()
    {
        // Arrange
        var handler = new VerifyArtifact.Handler(_artifactSigner);
        var command = new VerifyArtifact.Command(ArtifactId: "artifact-456");

        var verificationResult = new VerificationResult(
            IsValid: false,
            ArtifactId: "artifact-456",
            VerifiedAt: DateTime.UtcNow,
            SignerIdentity: "",
            Errors: new List<string> { "Assinatura não corresponde ao artefato" },
            Warnings: new List<string>()
        );

        _artifactSigner.VerifyArtifactAsync("artifact-456").Returns(verificationResult);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Errors.Should().Contain("Assinatura não corresponde ao artefato");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVerificationFails()
    {
        // Arrange
        var handler = new VerifyArtifact.Handler(_artifactSigner);
        var command = new VerifyArtifact.Command(ArtifactId: "artifact-789");

        _artifactSigner.VerifyArtifactAsync("artifact-789")
            .Returns(Task.FromException<VerificationResult>(new InvalidOperationException("certificado expirado")));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("artifact.verification_failed");
        result.Error.Message.Should().Contain("Falha ao verificar artefato");
    }

    [Fact]
    public async Task Handle_ShouldIncludeWarnings_WhenPresent()
    {
        // Arrange
        var handler = new VerifyArtifact.Handler(_artifactSigner);
        var command = new VerifyArtifact.Command(ArtifactId: "artifact-warn");

        var verificationResult = new VerificationResult(
            IsValid: true,
            ArtifactId: "artifact-warn",
            VerifiedAt: DateTime.UtcNow,
            SignerIdentity: "test-signer",
            Errors: new List<string>(),
            Warnings: new List<string> { "Certificado expira em 7 dias" }
        );

        _artifactSigner.VerifyArtifactAsync("artifact-warn").Returns(verificationResult);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Warnings.Should().Contain("Certificado expira em 7 dias");
    }
}
