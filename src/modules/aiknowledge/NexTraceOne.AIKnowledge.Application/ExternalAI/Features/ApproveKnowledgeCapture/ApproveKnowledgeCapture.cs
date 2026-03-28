using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;

/// <summary>
/// TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
/// This handler will implement capture approval workflow when knowledge capture is prioritized.
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

    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
            return await Task.FromResult<Result<Response>>(
                ExternalAiErrors.NotImplemented("Feature pending Phase 03"));
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
