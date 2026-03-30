using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.ListOperationalNotes;

/// <summary>
/// Lista notas operacionais com paginação e filtros opcionais.
/// P10.4: Endpoint de listagem para o Knowledge Hub frontend.
/// </summary>
public static class ListOperationalNotes
{
    public sealed record Query(
        NoteSeverity? Severity,
        string? ContextType,
        Guid? ContextEntityId,
        bool? IsResolved,
        int Page,
        int PageSize) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IOperationalNoteRepository noteRepository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await noteRepository.ListAsync(
                request.Severity,
                request.ContextType,
                request.ContextEntityId,
                request.IsResolved,
                request.Page,
                request.PageSize,
                cancellationToken);

            var dtos = items.Select(n => new OperationalNoteSummaryDto(
                n.Id.Value,
                n.Title,
                n.Content,
                n.Severity.ToString(),
                n.NoteType.ToString(),
                n.Origin,
                n.AuthorId,
                n.ContextEntityId,
                n.ContextType,
                n.Tags,
                n.IsResolved,
                n.CreatedAt,
                n.UpdatedAt,
                n.ResolvedAt)).ToArray();

            return new Response(dtos, totalCount, request.Page, request.PageSize);
        }
    }

    public sealed record OperationalNoteSummaryDto(
        Guid NoteId,
        string Title,
        string Content,
        string Severity,
        string NoteType,
        string Origin,
        Guid AuthorId,
        Guid? ContextEntityId,
        string? ContextType,
        IReadOnlyList<string> Tags,
        bool IsResolved,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? ResolvedAt);

    public sealed record Response(
        IReadOnlyList<OperationalNoteSummaryDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
