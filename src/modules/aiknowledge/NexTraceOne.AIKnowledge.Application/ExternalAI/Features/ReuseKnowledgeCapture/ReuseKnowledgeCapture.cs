using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;

/// <summary>
/// TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
/// This handler will implement capture reuse workflow when knowledge capture is prioritized.
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

    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
            return await Task.FromResult<Result<Response>>(
                ExternalAiErrors.NotImplemented("Feature pending Phase 03"));
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
