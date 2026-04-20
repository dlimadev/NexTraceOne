using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.GetServiceOperationalTimeline;

/// <summary>
/// Feature: GetServiceOperationalTimeline — timeline operacional de notas associadas a um serviço.
///
/// Retorna as notas operacionais ligadas a um serviço específico (ContextType="Service",
/// ContextEntityId=serviceId), ordenadas por CreatedAt descendente, com suporte a paginação
/// e filtro opcional por severidade e estado de resolução.
///
/// Pilar: Source of Truth &amp; Operational Knowledge.
/// Serve o painel de timeline do Knowledge Hub por serviço (OPS-01).
/// </summary>
public static class GetServiceOperationalTimeline
{
    /// <summary>Query para obter a timeline operacional de um serviço.</summary>
    public sealed record Query(
        Guid ServiceId,
        NoteSeverity? Severity = null,
        bool? IsResolved = null,
        int Page = 1,
        int PageSize = 25) : IQuery<Response>;

    /// <summary>Validação de entrada.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que consulta notas operacionais por serviço.</summary>
    public sealed class Handler(IOperationalNoteRepository noteRepository)
        : IQueryHandler<Query, Response>
    {
        private const string ServiceContextType = "Service";

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await noteRepository.ListAsync(
                request.Severity,
                ServiceContextType,
                request.ServiceId,
                request.IsResolved,
                request.Page,
                request.PageSize,
                cancellationToken);

            var dtos = items.Select(n => new TimelineEntryDto(
                NoteId: n.Id.Value,
                Title: n.Title,
                Content: n.Content,
                Severity: n.Severity.ToString(),
                NoteType: n.NoteType.ToString(),
                Origin: n.Origin,
                AuthorId: n.AuthorId,
                Tags: n.Tags,
                IsResolved: n.IsResolved,
                OccurredAt: n.CreatedAt,
                UpdatedAt: n.UpdatedAt,
                ResolvedAt: n.ResolvedAt)).ToArray();

            return new Response(
                ServiceId: request.ServiceId,
                Items: dtos,
                TotalCount: totalCount,
                Page: request.Page,
                PageSize: request.PageSize,
                TotalPages: (int)Math.Ceiling((double)totalCount / request.PageSize));
        }
    }

    /// <summary>Entrada da timeline operacional.</summary>
    public sealed record TimelineEntryDto(
        Guid NoteId,
        string Title,
        string Content,
        string Severity,
        string NoteType,
        string Origin,
        Guid AuthorId,
        IReadOnlyList<string> Tags,
        bool IsResolved,
        DateTimeOffset OccurredAt,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? ResolvedAt);

    /// <summary>Resposta paginada da timeline operacional.</summary>
    public sealed record Response(
        Guid ServiceId,
        IReadOnlyList<TimelineEntryDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);
}
