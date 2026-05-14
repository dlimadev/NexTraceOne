using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.VerifyArtifact;

/// <summary>
/// Feature: VerifyArtifact — verifica a assinatura digital de um artefato usando Cosign.
/// Valida integridade do checksum, autenticidade da assinatura e entrada no transparency log (Rekor).
/// Essencial para gate de segurança em pipelines CI/CD e auditoria de compliance.
/// </summary>
public static class VerifyArtifact
{
    /// <summary>Comando para verificar a assinatura de um artefato.</summary>
    public sealed record Command(
        string ArtifactId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de verificação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArtifactId).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Resposta com o resultado da verificação de assinatura.</summary>
    public sealed record Response(
        bool IsValid,
        string ArtifactId,
        DateTime? VerifiedAt,
        string SignerIdentity,
        List<string> Errors,
        List<string> Warnings);

    /// <summary>Handler que executa a verificação via IArtifactSigner.</summary>
    public sealed class Handler(
        IArtifactSigner artifactSigner) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var verificationResult = await artifactSigner.VerifyArtifactAsync(request.ArtifactId);

                return new Response(
                    IsValid: verificationResult.IsValid,
                    ArtifactId: verificationResult.ArtifactId,
                    VerifiedAt: verificationResult.VerifiedAt,
                    SignerIdentity: verificationResult.SignerIdentity,
                    Errors: verificationResult.Errors,
                    Warnings: verificationResult.Warnings);
            }
            catch (Exception ex)
            {
                return Error.Business("artifact.verification_failed", $"Falha ao verificar artefato: {ex.Message}");
            }
        }
    }
}
