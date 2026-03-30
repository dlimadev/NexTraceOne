using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.UpdateOperationalNote;

/// <summary>
/// Atualiza título, conteúdo, severidade, tipo, tags e/ou estado de resolução de uma nota operacional.
/// Suporta operações de Resolve/Reopen para gestão de ciclo de vida.
/// </summary>
public static class UpdateOperationalNote
{
    public sealed record Command(
        Guid NoteId,
        string? Title,
        string? Content,
        NoteSeverity? Severity,
        OperationalNoteType? NoteType,
        IReadOnlyList<string>? Tags,
        bool? Resolve,
        Guid EditorId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).NotEqual(Guid.Empty);
            RuleFor(x => x.EditorId).NotEqual(Guid.Empty);
            RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title is not null);
            RuleFor(x => x.Content).NotEmpty().When(x => x.Content is not null);
        }
    }

    public sealed class Handler(
        IOperationalNoteRepository noteRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var note = await noteRepository.GetByIdAsync(
                new OperationalNoteId(request.NoteId),
                cancellationToken);

            if (note is null)
                return Error.NotFound("knowledge.note.not_found", "Operational note not found.");

            var now = clock.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Content))
            {
                note.UpdateContent(
                    request.Title ?? note.Title,
                    request.Content ?? note.Content,
                    now);
            }

            if (request.Severity.HasValue)
                note.UpdateSeverity(request.Severity.Value, now);

            if (request.NoteType.HasValue)
                note.UpdateType(request.NoteType.Value, now);

            if (request.Tags is not null)
                note.UpdateTags(request.Tags, now);

            if (request.Resolve == true && !note.IsResolved)
                note.Resolve(now);
            else if (request.Resolve == false && note.IsResolved)
                note.Reopen(now);

            noteRepository.Update(note);

            return new Response(note.Id.Value, note.IsResolved);
        }
    }

    public sealed record Response(Guid NoteId, bool IsResolved);
}
