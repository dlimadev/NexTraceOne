using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;

/// <summary>
/// Feature: ListIncidents — lista incidentes com filtros contextualizados.
/// Retorna resumo de incidentes com correlação, severidade, status, serviço e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListIncidents
{
    /// <summary>Query para listar incidentes com filtros.</summary>
    public sealed record Query(
        string? TeamId,
        string? ServiceId,
        string? Environment,
        IncidentSeverity? Severity,
        IncidentStatus? Status,
        IncidentType? IncidentType,
        string? Search,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Environment).MaximumLength(200).When(x => x.Environment is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>
    /// Handler que compõe a listagem de incidentes.
    /// Delega ao IIncidentStore para obter os dados.
    /// </summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filtered = store.GetIncidentListItems().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(i => i.OwnerTeam.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(i => i.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Environment))
                filtered = filtered.Where(i => i.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase));

            if (request.Severity.HasValue)
                filtered = filtered.Where(i => i.Severity == request.Severity.Value);

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.Status == request.Status.Value);

            if (request.IncidentType.HasValue)
                filtered = filtered.Where(i => i.IncidentType == request.IncidentType.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(i =>
                    i.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.Reference.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.ServiceDisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            if (request.From.HasValue)
                filtered = filtered.Where(i => i.CreatedAt >= request.From.Value);

            if (request.To.HasValue)
                filtered = filtered.Where(i => i.CreatedAt <= request.To.Value);

            var items = filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var response = new Response(
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Item resumido de incidente na listagem.</summary>
    public sealed record IncidentListItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        string ServiceId,
        string ServiceDisplayName,
        string OwnerTeam,
        string Environment,
        DateTimeOffset CreatedAt,
        bool HasCorrelatedChanges,
        CorrelationConfidence CorrelationConfidence,
        MitigationStatus MitigationStatus);

    /// <summary>Resposta paginada da listagem de incidentes.</summary>
    public sealed record Response(
        IReadOnlyList<IncidentListItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
