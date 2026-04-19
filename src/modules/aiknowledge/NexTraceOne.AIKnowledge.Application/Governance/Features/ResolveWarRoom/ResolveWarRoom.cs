using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ResolveWarRoom;

/// <summary>
/// Feature: ResolveWarRoom — marca uma War Room como resolvida com post-mortem.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ResolveWarRoom
{
    public sealed record Command(Guid WarRoomSessionId, string PostMortemDraft) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WarRoomSessionId).NotEmpty();
        }
    }

    public sealed class Handler(
        IAiWarRoomRepository warRoomRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var session = await warRoomRepository.GetByIdAsync(
                WarRoomSessionId.From(request.WarRoomSessionId), ct);

            if (session is null)
                return AiGovernanceErrors.WarRoomNotFound(request.WarRoomSessionId.ToString());

            session.Resolve(request.PostMortemDraft, DateTimeOffset.UtcNow);
            await unitOfWork.CommitAsync(ct);

            return new Response(session.Id.Value, session.Status, session.ResolvedAt);
        }
    }

    public sealed record Response(Guid WarRoomSessionId, string Status, DateTimeOffset? ResolvedAt);
}
