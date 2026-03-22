using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;

/// <summary>
/// Feature: CaptureExternalAIResponse — captura e persiste resposta de IA externa para
/// governança, auditoria e reutilização futura. Cria um registro de consulta (completada)
/// e uma entrada de conhecimento no estado Pending para revisão humana.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CaptureExternalAIResponse
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para capturar e persistir resposta de IA externa.</summary>
    public sealed record Command(
        Guid ProviderId,
        string Context,
        string Query,
        string AiResponse,
        int TokensUsed,
        decimal Confidence,
        string Title,
        string Category,
        string Tags) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProviderId).NotEmpty();
            RuleFor(x => x.Context).NotEmpty().MaximumLength(5_000);
            RuleFor(x => x.Query).NotEmpty().MaximumLength(10_000);
            RuleFor(x => x.AiResponse).NotEmpty().MaximumLength(50_000);
            RuleFor(x => x.TokensUsed).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Confidence).InclusiveBetween(0m, 1m);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Tags).NotEmpty().MaximumLength(500);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAiProviderRepository providerRepository,
        IExternalAiConsultationRepository consultationRepository,
        IKnowledgeCaptureRepository captureRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var providerId = ExternalAiProviderId.From(request.ProviderId);

            var providerExists = await providerRepository.ExistsAsync(providerId, cancellationToken);
            if (!providerExists)
                return ExternalAiErrors.ProviderNotFound(request.ProviderId.ToString());

            var consultation = ExternalAiConsultation.Create(
                providerId,
                request.Context,
                request.Query,
                currentUser.Email,
                now);

            var recordResult = consultation.RecordResponse(
                request.AiResponse,
                request.TokensUsed,
                request.Confidence,
                now);

            if (!recordResult.IsSuccess)
                return recordResult.Error!;

            var capture = KnowledgeCapture.Capture(
                consultation.Id,
                request.Title,
                request.AiResponse,
                request.Category,
                request.Tags,
                now);

            await consultationRepository.AddAsync(consultation, cancellationToken);
            await captureRepository.AddAsync(capture, cancellationToken);

            logger.LogInformation(
                "Knowledge captured: {CaptureId} from consultation {ConsultationId}. Category={Category}, TokensUsed={Tokens}",
                capture.Id.Value, consultation.Id.Value, request.Category, request.TokensUsed);

            return new Response(
                capture.Id.Value,
                consultation.Id.Value,
                request.Title,
                request.Category,
                capture.Status.ToString(),
                now);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da captura de conhecimento de IA externa.</summary>
    public sealed record Response(
        Guid CaptureId,
        Guid ConsultationId,
        string Title,
        string Category,
        string Status,
        DateTimeOffset CapturedAt);
}
