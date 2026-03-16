using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;

/// <summary>
/// Feature: ListRunbooks — retorna a lista de runbooks operacionais disponíveis,
/// com suporte a filtragem por serviço, tipo de incidente e pesquisa textual.
/// </summary>
public static class ListRunbooks
{
    /// <summary>Query para listar runbooks com filtros opcionais.</summary>
    public sealed record Query(string? ServiceId, string? IncidentType, string? Search) : IQuery<Response>;

    /// <summary>Valida os parâmetros opcionais da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.IncidentType).MaximumLength(200).When(x => x.IncidentType is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>Handler que retorna a lista de runbooks via store.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filtered = store.GetRunbooks().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(r =>
                    r.LinkedServiceId is not null
                    && r.LinkedServiceId.Contains(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.IncidentType))
                filtered = filtered.Where(r =>
                    r.LinkedIncidentType is not null
                    && r.LinkedIncidentType.Equals(request.IncidentType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(r =>
                    r.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                    || r.Summary.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            var response = new Response(Runbooks: filtered.ToList().AsReadOnly());
            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com a lista de runbooks.</summary>
    public sealed record Response(IReadOnlyList<RunbookSummaryDto> Runbooks);

    /// <summary>Resumo de um runbook operacional.</summary>
    public sealed record RunbookSummaryDto(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        int StepCount,
        DateTimeOffset CreatedAt);
}
