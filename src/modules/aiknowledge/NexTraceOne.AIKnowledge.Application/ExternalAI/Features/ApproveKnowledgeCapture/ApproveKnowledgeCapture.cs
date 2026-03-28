using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;

/// <summary>
/// Feature: ApproveKnowledgeCapture — aprova uma captura de conhecimento pendente,
/// tornando-a disponível para reutilização. Rejeita se já tiver sido revisada.
/// </summary>
public static class ApproveKnowledgeCapture
{
    /// <summary>Comando para aprovar um capture de conhecimento de IA externa.</summary>
    public sealed record Command(
        Guid CaptureId,
        string? ReviewNotes) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CaptureId).NotEmpty();
            RuleFor(x => x.ReviewNotes).MaximumLength(2_000).When(x => x.ReviewNotes is not null);
        }
    }

    public sealed class Handler(
        IKnowledgeCaptureRepository captureRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var captureId = KnowledgeCaptureId.From(request.CaptureId);
            var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken);
            if (capture is null)
                return ExternalAiErrors.KnowledgeCaptureNotFound(request.CaptureId.ToString());

            var approveResult = capture.Approve(currentUser.Id, dateTimeProvider.UtcNow);
            if (approveResult.IsFailure)
                return approveResult.Error;

            await captureRepository.UpdateAsync(capture, cancellationToken);

            return new Response(
                capture.Id.Value,
                capture.Status.ToString(),
                capture.ReviewedBy!,
                capture.ReviewedAt!.Value,
                request.ReviewNotes);
        }
    }

    /// <summary>Resultado da aprovação do capture de conhecimento.</summary>
    public sealed record Response(
        Guid CaptureId,
        string Status,
        string ApprovedBy,
        DateTimeOffset ApprovedAt,
        string? ReviewNotes);
}
