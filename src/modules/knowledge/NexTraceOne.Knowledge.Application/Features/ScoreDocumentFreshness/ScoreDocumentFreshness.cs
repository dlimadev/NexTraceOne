using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Features.ScoreDocumentFreshness;

/// <summary>
/// Feature: ScoreDocumentFreshness — calcula e persiste o FreshnessScore de um documento.
/// Útil para pipelines periódicos de housekeeping de conhecimento.
/// Pilar: Source of Truth. Owner: Knowledge.
/// </summary>
public static class ScoreDocumentFreshness
{
    public sealed record Command(string DocumentId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.DocumentId).NotEmpty().MaximumLength(200);
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository repo,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.DocumentId, out var guid))
                return Error.Validation("INVALID_DOCUMENT_ID", "Document ID is not a valid GUID.");

            var doc = await repo.GetByIdAsync(new KnowledgeDocumentId(guid), cancellationToken);
            if (doc is null)
                return Error.NotFound("DOCUMENT_NOT_FOUND", "Document '{0}' not found.", request.DocumentId);

            doc.ComputeFreshnessScore(clock.UtcNow);
            repo.Update(doc);

            return Result<Response>.Success(new Response(
                DocumentId: request.DocumentId,
                Title: doc.Title,
                FreshnessScore: doc.FreshnessScore,
                LastReviewedAt: doc.LastReviewedAt,
                ComputedAt: clock.UtcNow));
        }
    }

    public sealed record Response(
        string DocumentId, string Title, int FreshnessScore,
        DateTimeOffset? LastReviewedAt, DateTimeOffset ComputedAt);
}
