using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ReviewArtifact;

/// <summary>
/// Feature: ReviewArtifact — aprova ou rejeita um artefacto de agent.
/// Suporta notas de justificação e registo explícito de quem fez a review.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class ReviewArtifact
{
    /// <summary>Comando de review de artefacto.</summary>
    public sealed record Command(
        Guid ArtifactId,
        string Decision,
        string? Notes) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de review.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArtifactId).NotEmpty();
            RuleFor(x => x.Decision).NotEmpty()
                .Must(d => d is "Approve" or "Reject")
                .WithMessage("Decision must be 'Approve' or 'Reject'.");
            RuleFor(x => x.Notes).MaximumLength(2000);
        }
    }

    /// <summary>Handler que executa a review de um artefacto.</summary>
    public sealed class Handler(
        IAiAgentArtifactRepository artifactRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var artifact = await artifactRepository.GetByIdAsync(
                AiAgentArtifactId.From(request.ArtifactId), cancellationToken);

            if (artifact is null)
                return AiGovernanceErrors.ArtifactNotFound(request.ArtifactId.ToString());

            var reviewResult = request.Decision == "Approve"
                ? artifact.Approve(currentUser.Id, dateTimeProvider.UtcNow, request.Notes)
                : artifact.Reject(currentUser.Id, dateTimeProvider.UtcNow, request.Notes);

            if (reviewResult.IsFailure)
                return reviewResult.Error;

            await artifactRepository.UpdateAsync(artifact, cancellationToken);

            return new Response(artifact.Id.Value, artifact.ReviewStatus.ToString());
        }
    }

    /// <summary>Resposta da review de artefacto.</summary>
    public sealed record Response(Guid ArtifactId, string ReviewStatus);
}
