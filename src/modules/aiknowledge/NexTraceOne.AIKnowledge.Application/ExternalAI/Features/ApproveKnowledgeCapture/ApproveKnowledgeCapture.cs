using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;

/// <summary>
/// Feature: ApproveKnowledgeCapture — aprova formalmente um capture de conhecimento
/// para reutilização confiável. Valida pré-condições e registra quem aprovou e quando.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ApproveKnowledgeCapture
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para aprovar um capture de conhecimento de IA externa.</summary>
    public sealed record Command(
        Guid CaptureId,
        string? ReviewNotes) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CaptureId).NotEmpty();
            RuleFor(x => x.ReviewNotes).MaximumLength(2_000).When(x => x.ReviewNotes is not null);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IKnowledgeCaptureRepository captureRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var capture = await captureRepository.GetByIdAsync(
                KnowledgeCaptureId.From(request.CaptureId), cancellationToken);

            if (capture is null)
                return ExternalAiErrors.KnowledgeCaptureNotFound(request.CaptureId.ToString());

            var now = dateTimeProvider.UtcNow;
            var reviewer = currentUser.Email;

            var approvalResult = capture.Approve(reviewer, now);
            if (!approvalResult.IsSuccess)
                return approvalResult.Error!;

            await captureRepository.UpdateAsync(capture, cancellationToken);

            logger.LogInformation(
                "Knowledge capture {CaptureId} approved by {Reviewer} at {ApprovedAt}",
                capture.Id.Value, reviewer, now);

            return new Response(
                capture.Id.Value,
                capture.Status.ToString(),
                reviewer,
                now,
                request.ReviewNotes);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da aprovação do capture de conhecimento.</summary>
    public sealed record Response(
        Guid CaptureId,
        string Status,
        string ApprovedBy,
        DateTimeOffset ApprovedAt,
        string? ReviewNotes);
}
