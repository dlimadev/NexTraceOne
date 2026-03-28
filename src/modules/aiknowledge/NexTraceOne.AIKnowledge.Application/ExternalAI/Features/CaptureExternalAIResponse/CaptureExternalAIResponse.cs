using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;

/// <summary>
/// TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
/// This handler will implement capture persistence from external AI responses in Phase 03.
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

    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
            return await Task.FromResult<Result<Response>>(
                ExternalAiErrors.NotImplemented("Feature pending Phase 03"));
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
