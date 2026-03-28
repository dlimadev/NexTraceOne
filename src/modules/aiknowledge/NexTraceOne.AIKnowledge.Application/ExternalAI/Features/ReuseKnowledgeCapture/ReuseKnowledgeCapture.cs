using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;

/// <summary>
/// Feature: ReuseKnowledgeCapture — regista a reutilização de um capture aprovado num
/// novo contexto, incrementando o contador de reuso e rastreando a intenção de uso.
/// </summary>
public static class ReuseKnowledgeCapture
{
    /// <summary>Comando para reutilizar um capture aprovado num novo contexto.</summary>
    public sealed record Command(
        Guid CaptureId,
        string NewContext,
        string? Purpose) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CaptureId).NotEmpty();
            RuleFor(x => x.NewContext).NotEmpty().MaximumLength(2_000);
            RuleFor(x => x.Purpose).MaximumLength(500).When(x => x.Purpose is not null);
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

            var reuseResult = capture.IncrementReuse();
            if (reuseResult.IsFailure)
                return reuseResult.Error;

            await captureRepository.UpdateAsync(capture, cancellationToken);

            return new Response(
                capture.Id.Value,
                capture.Title,
                capture.Content,
                capture.Category,
                capture.ReuseCount,
                request.NewContext,
                request.Purpose,
                currentUser.Id,
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resultado da reutilização do capture de conhecimento.</summary>
    public sealed record Response(
        Guid CaptureId,
        string Title,
        string Content,
        string Category,
        int UpdatedReuseCount,
        string NewContext,
        string? Purpose,
        string ReusedBy,
        DateTimeOffset ReusedAt);
}
