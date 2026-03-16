using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;

/// <summary>
/// Feature: ListIncidentsByTeam — lista incidentes filtrados por equipa.
/// Visão focada para Tech Lead que precisa ver incidentes da sua equipa.
/// Reutiliza o modelo de listagem com filtro de teamId pré-aplicado.
/// </summary>
public static class ListIncidentsByTeam
{
    /// <summary>Query para listar incidentes de uma equipa específica.</summary>
    public sealed record Query(
        string TeamId,
        IncidentStatus? Status,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que compõe a listagem de incidentes por equipa.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filtered = store.GetIncidentListItems()
                .Where(i => i.OwnerTeam.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase))
                .Select(i => new TeamIncidentItem(
                    i.IncidentId, i.Reference, i.Title, i.IncidentType,
                    i.Severity, i.Status, i.ServiceId, i.ServiceDisplayName,
                    i.OwnerTeam, i.CreatedAt, i.HasCorrelatedChanges, i.MitigationStatus))
                .AsEnumerable();

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.Status == request.Status.Value);

            var items = filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var response = new Response(
                TeamId: request.TeamId,
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Item de incidente de uma equipa.</summary>
    public sealed record TeamIncidentItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        string ServiceId,
        string ServiceDisplayName,
        string OwnerTeam,
        DateTimeOffset CreatedAt,
        bool HasCorrelatedChanges,
        MitigationStatus MitigationStatus);

    /// <summary>Resposta paginada de incidentes por equipa.</summary>
    public sealed record Response(
        string TeamId,
        IReadOnlyList<TeamIncidentItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
