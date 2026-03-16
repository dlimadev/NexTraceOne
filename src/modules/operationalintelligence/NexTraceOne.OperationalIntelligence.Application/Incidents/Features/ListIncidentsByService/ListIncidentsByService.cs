using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;

/// <summary>
/// Feature: ListIncidentsByService — lista incidentes filtrados por serviço.
/// Visão focada para Engineer e Tech Lead que precisam ver incidentes do seu serviço.
/// Reutiliza o modelo de listagem com filtro de serviceId pré-aplicado.
/// </summary>
public static class ListIncidentsByService
{
    /// <summary>Query para listar incidentes de um serviço específico.</summary>
    public sealed record Query(
        string ServiceId,
        IncidentStatus? Status,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que compõe a listagem de incidentes por serviço.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filtered = store.GetIncidentListItems()
                .Where(i => i.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase))
                .Select(i => new ServiceIncidentItem(
                    i.IncidentId, i.Reference, i.Title, i.IncidentType,
                    i.Severity, i.Status, i.ServiceId, i.CreatedAt,
                    i.HasCorrelatedChanges, i.MitigationStatus))
                .AsEnumerable();

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.Status == request.Status.Value);

            var items = filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var response = new Response(
                ServiceId: request.ServiceId,
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Item de incidente de um serviço.</summary>
    public sealed record ServiceIncidentItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        string ServiceId,
        DateTimeOffset CreatedAt,
        bool HasCorrelatedChanges,
        MitigationStatus MitigationStatus);

    /// <summary>Resposta paginada de incidentes por serviço.</summary>
    public sealed record Response(
        string ServiceId,
        IReadOnlyList<ServiceIncidentItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
