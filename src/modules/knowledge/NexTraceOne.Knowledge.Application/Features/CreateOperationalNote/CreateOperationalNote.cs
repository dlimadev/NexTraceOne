using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.CreateOperationalNote;

/// <summary>
/// Cria nota operacional contextual para serviço, incidente, mudança ou outra entidade relevante.
/// </summary>
public static class CreateOperationalNote
{
    public sealed record Command(
        string Title,
        string Content,
        NoteSeverity Severity,
        OperationalNoteType NoteType,
        string Origin,
        Guid AuthorId,
        Guid? ContextEntityId,
        string? ContextType,
        IReadOnlyList<string>? Tags) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.Origin).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AuthorId).NotEqual(Guid.Empty);
            RuleFor(x => x.ContextType).MaximumLength(100);
        }
    }

    public sealed class Handler(
        IOperationalNoteRepository noteRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var note = OperationalNote.Create(
                request.Title,
                request.Content,
                request.Severity,
                request.NoteType,
                request.Origin,
                request.AuthorId,
                request.ContextEntityId,
                request.ContextType,
                request.Tags,
                clock.UtcNow);

            await noteRepository.AddAsync(note, cancellationToken);
            return new Response(note.Id.Value);
        }
    }

    public sealed record Response(Guid NoteId);
}
