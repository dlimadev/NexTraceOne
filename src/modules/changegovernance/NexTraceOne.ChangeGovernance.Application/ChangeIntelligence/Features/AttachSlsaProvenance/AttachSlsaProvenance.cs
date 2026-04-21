using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachSlsaProvenance;

/// <summary>
/// Feature: AttachSlsaProvenance — anexa evidência SLSA Level 3 a uma release existente.
///
/// SLSA (Supply-chain Levels for Software Artifacts) Level 3 requer:
/// - Proveniência verificável gerada pelo build system (não pelo developer)
/// - Digest imutável do artefacto publicado
/// - SBOM publicado e acessível
///
/// Esta feature permite ingestão dessas evidências em releases já registadas no NexTraceOne,
/// tornando-as elegíveis para o gate <c>SignedArtifactGate</c> em pipelines de promoção.
/// Wave D backlog — SLSA Level 3 evidence capture.
/// </summary>
public static class AttachSlsaProvenance
{
    /// <summary>Comando para anexar evidência SLSA Level 3 a uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string? SlsaProvenanceUri,
        string? ArtifactDigest,
        string? SbomUri) : ICommand<Response>;

    /// <summary>Valida que pelo menos um campo de evidência foi fornecido.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.SlsaProvenanceUri).MaximumLength(2000).When(x => x.SlsaProvenanceUri is not null);
            RuleFor(x => x.ArtifactDigest).MaximumLength(200).When(x => x.ArtifactDigest is not null);
            RuleFor(x => x.SbomUri).MaximumLength(2000).When(x => x.SbomUri is not null);
            RuleFor(x => x).Must(x =>
                !string.IsNullOrWhiteSpace(x.SlsaProvenanceUri) ||
                !string.IsNullOrWhiteSpace(x.ArtifactDigest) ||
                !string.IsNullOrWhiteSpace(x.SbomUri))
                .WithMessage("At least one SLSA evidence field (SlsaProvenanceUri, ArtifactDigest, or SbomUri) must be provided.");
        }
    }

    /// <summary>Handler que localiza a release e anexa a evidência SLSA.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await releaseRepository.GetByIdAsync(
                ReleaseId.From(request.ReleaseId), cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var result = release.AttachSlsaProvenance(
                request.SlsaProvenanceUri,
                request.ArtifactDigest,
                request.SbomUri);

            if (!result.IsSuccess)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ReleaseId: request.ReleaseId,
                SlsaProvenanceUri: release.SlsaProvenanceUri,
                ArtifactDigest: release.ArtifactDigest,
                SbomUri: release.SbomUri,
                HasArtifactAttestation: release.ArtifactDigest is not null));
        }
    }

    /// <summary>Resposta com os campos SLSA atualizados.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string? SlsaProvenanceUri,
        string? ArtifactDigest,
        string? SbomUri,
        bool HasArtifactAttestation);
}
