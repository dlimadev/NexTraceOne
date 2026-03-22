using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;

/// <summary>
/// Feature: ReuseKnowledgeCapture — reutiliza um capture aprovado num novo contexto de trabalho.
/// Valida elegibilidade (somente captures aprovados), incrementa o contador de reutilização,
/// e regista o novo contexto de forma auditável.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ReuseKnowledgeCapture
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para reutilizar um capture aprovado num novo contexto.</summary>
    public sealed record Command(
        Guid CaptureId,
        string NewContext,
        string? Purpose) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CaptureId).NotEmpty();
            RuleFor(x => x.NewContext).NotEmpty().MaximumLength(2_000);
            RuleFor(x => x.Purpose).MaximumLength(500).When(x => x.Purpose is not null);
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

            var reuseResult = capture.IncrementReuse();
            if (!reuseResult.IsSuccess)
                return reuseResult.Error!;

            await captureRepository.UpdateAsync(capture, cancellationToken);

            logger.LogInformation(
                "Knowledge capture {CaptureId} reused by {User} in context '{Context}'. ReuseCount={Count}",
                capture.Id.Value, currentUser.Email, request.NewContext, capture.ReuseCount);

            return new Response(
                capture.Id.Value,
                capture.Title,
                capture.Content,
                capture.Category,
                capture.ReuseCount,
                request.NewContext,
                request.Purpose,
                currentUser.Email,
                dateTimeProvider.UtcNow);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

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
