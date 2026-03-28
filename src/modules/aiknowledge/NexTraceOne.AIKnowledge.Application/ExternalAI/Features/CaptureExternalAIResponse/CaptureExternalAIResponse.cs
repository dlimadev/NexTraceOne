using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;

/// <summary>
/// Feature: CaptureExternalAIResponse — persiste uma consulta de IA externa e o conhecimento
/// organizacional capturado a partir da resposta. Cria ExternalAiConsultation no estado
/// Completed e KnowledgeCapture no estado Pending aguardando revisão.
/// </summary>
public static class CaptureExternalAIResponse
{
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

    public sealed class Handler(
        IExternalAiProviderRepository providerRepository,
        IExternalAiConsultationRepository consultationRepository,
        IKnowledgeCaptureRepository captureRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var providerId = ExternalAiProviderId.From(request.ProviderId);
            var providerExists = await providerRepository.ExistsAsync(providerId, cancellationToken);
            if (!providerExists)
                return ExternalAiErrors.ProviderNotFound(request.ProviderId.ToString());

            var now = dateTimeProvider.UtcNow;

            var consultation = ExternalAiConsultation.Create(
                providerId,
                request.Context,
                request.Query,
                currentUser.Id,
                now);

            var recordResult = consultation.RecordResponse(
                request.AiResponse,
                request.TokensUsed,
                request.Confidence,
                now);

            if (recordResult.IsFailure)
                return recordResult.Error;

            await consultationRepository.AddAsync(consultation, cancellationToken);

            var capture = KnowledgeCapture.Capture(
                consultation.Id,
                request.Title,
                request.AiResponse,
                request.Category,
                request.Tags,
                now);

            await captureRepository.AddAsync(capture, cancellationToken);

            return new Response(
                capture.Id.Value,
                consultation.Id.Value,
                capture.Title,
                capture.Category,
                capture.Status.ToString(),
                capture.CapturedAt);
        }
    }

    /// <summary>Resultado da captura de conhecimento de IA externa.</summary>
    public sealed record Response(
        Guid CaptureId,
        Guid ConsultationId,
        string Title,
        string Category,
        string Status,
        DateTimeOffset CapturedAt);
}
