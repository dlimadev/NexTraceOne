using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.SignArtifact;

/// <summary>
/// Feature: SignArtifact — assina digitalmente um artefato (docker image, nuget package, binary) usando Cosign.
/// Gera SBOM (Software Bill of Materials), envia para transparency log (Rekor) e retorna assinatura verificável.
/// Integração com governança de compliance e auditoria de segurança.
/// </summary>
public static class SignArtifact
{
    /// <summary>Comando para assinar um artefato digitalmente.</summary>
    public sealed record Command(
        string ArtifactPath,
        string ArtifactType, // docker-image, nuget-package, binary
        string Version,
        Dictionary<string, string>? Metadata = null) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de assinatura.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArtifactPath).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ArtifactType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        }
    }

    /// <summary>Resposta com o artefato assinado e metadados de assinatura.</summary>
    public sealed record Response(
        string ArtifactId,
        string ArtifactName,
        string Checksum,
        string Signature,
        DateTime SignedAt,
        string SignerIdentity,
        string? SbomJson,
        string? TransparencyLogEntry);

    /// <summary>Handler que executa a assinatura do artefato via CosignArtifactSigner.</summary>
    public sealed class Handler(
        IArtifactSigner artifactSigner,
        ISbomGenerator sbomGenerator,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var signingRequest = new SigningRequest(
                    ArtifactPath: request.ArtifactPath,
                    ArtifactType: request.ArtifactType,
                    Version: request.Version,
                    Metadata: request.Metadata ?? new Dictionary<string, string>());

                var signedArtifact = await artifactSigner.SignArtifactAsync(signingRequest);

                var sbomJson = signedArtifact.SbomDocument != null
                    ? await sbomGenerator.ExportSbomToJsonAsync(signedArtifact.SbomDocument)
                    : null;

                return new Response(
                    ArtifactId: signedArtifact.ArtifactId,
                    ArtifactName: signedArtifact.ArtifactName,
                    Checksum: signedArtifact.Checksum,
                    Signature: signedArtifact.Signature,
                    SignedAt: signedArtifact.SignedAt,
                    SignerIdentity: signedArtifact.SignerIdentity,
                    SbomJson: sbomJson,
                    TransparencyLogEntry: signedArtifact.TransparencyLogEntry);
            }
            catch (Exception ex)
            {
                return Error.Business("artifact.signing_failed", $"Falha ao assinar artefato: {ex.Message}");
            }
        }
    }
}
