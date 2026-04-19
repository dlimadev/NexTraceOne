using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateWarRoom;

/// <summary>
/// Feature: CreateWarRoom — cria uma War Room para coordenação de incidentes P0/P1.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateWarRoom
{
    public sealed record Command(
        string IncidentId,
        string IncidentTitle,
        string Severity,
        string ServiceAffected,
        string CreatedByAgentId,
        string SkillUsed,
        Guid TenantId,
        string CreatedByUserId,
        IReadOnlyList<string> InitialParticipantIds) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidSeverities = ["P0", "P1", "P2", "P3"];

        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.IncidentTitle).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Severity).NotEmpty().Must(s => ValidSeverities.Contains(s))
                .WithMessage("Severity must be P0, P1, P2 or P3.");
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.CreatedByUserId).NotEmpty();
        }
    }

    public sealed class Handler(
        IAiWarRoomRepository warRoomRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var session = WarRoomSession.Create(
                request.IncidentId,
                request.IncidentTitle,
                request.Severity,
                request.ServiceAffected,
                request.CreatedByAgentId,
                request.SkillUsed,
                request.TenantId,
                DateTimeOffset.UtcNow);

            session.AddParticipant(request.CreatedByUserId);
            foreach (var participantId in request.InitialParticipantIds)
                session.AddParticipant(participantId);

            warRoomRepository.Add(session);
            await unitOfWork.CommitAsync(ct);

            return new Response(session.Id.Value, session.IncidentId, session.Status, session.OpenedAt);
        }
    }

    public sealed record Response(Guid WarRoomSessionId, string IncidentId, string Status, DateTimeOffset OpenedAt);
}
