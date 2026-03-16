using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;

/// <summary>
/// Feature: GetIncidentSummary — retorna métricas agregadas de incidentes.
/// Permite visão rápida por equipa, ambiente e período.
/// Persona-aware: Tech Lead vê por equipa, Executive vê agregado, Platform Admin vê cobertura.
/// </summary>
public static class GetIncidentSummary
{
    /// <summary>Query para obter o resumo agregado de incidentes.</summary>
    public sealed record Query(
        string? TeamId,
        string? Environment,
        DateTimeOffset? From,
        DateTimeOffset? To) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);
            RuleFor(x => x.Environment).MaximumLength(200).When(x => x.Environment is not null);
        }
    }

    /// <summary>Handler que compõe o resumo agregado a partir do store.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = store.GetIncidentListItems().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                items = items.Where(i => i.OwnerTeam.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Environment))
                items = items.Where(i => i.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase));

            if (request.From.HasValue)
                items = items.Where(i => i.CreatedAt >= request.From.Value);

            if (request.To.HasValue)
                items = items.Where(i => i.CreatedAt <= request.To.Value);

            var materialized = items.ToList();

            var openStatuses = new[]
            {
                IncidentStatus.Open,
                IncidentStatus.Investigating,
                IncidentStatus.Mitigating,
                IncidentStatus.Monitoring,
            };

            var openItems = materialized.Where(i => openStatuses.Contains(i.Status)).ToList();

            var response = new Response(
                TotalOpen: openItems.Count,
                CriticalIncidents: openItems.Count(i => i.Severity == IncidentSeverity.Critical),
                WithCorrelatedChanges: openItems.Count(i => i.HasCorrelatedChanges),
                WithMitigationAvailable: openItems.Count(i =>
                    i.MitigationStatus is MitigationStatus.InProgress or MitigationStatus.Applied or MitigationStatus.Verified),
                ServicesImpacted: openItems.Select(i => i.ServiceId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                SeverityBreakdown: new SeverityBreakdown(
                    Critical: openItems.Count(i => i.Severity == IncidentSeverity.Critical),
                    Major: openItems.Count(i => i.Severity == IncidentSeverity.Major),
                    Minor: openItems.Count(i => i.Severity == IncidentSeverity.Minor),
                    Warning: openItems.Count(i => i.Severity == IncidentSeverity.Warning)),
                StatusBreakdown: new StatusBreakdown(
                    Open: materialized.Count(i => i.Status == IncidentStatus.Open),
                    Investigating: materialized.Count(i => i.Status == IncidentStatus.Investigating),
                    Mitigating: materialized.Count(i => i.Status == IncidentStatus.Mitigating),
                    Monitoring: materialized.Count(i => i.Status == IncidentStatus.Monitoring),
                    Resolved: materialized.Count(i => i.Status == IncidentStatus.Resolved),
                    Closed: materialized.Count(i => i.Status == IncidentStatus.Closed)));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do resumo agregado de incidentes.</summary>
    public sealed record Response(
        int TotalOpen,
        int CriticalIncidents,
        int WithCorrelatedChanges,
        int WithMitigationAvailable,
        int ServicesImpacted,
        SeverityBreakdown SeverityBreakdown,
        StatusBreakdown StatusBreakdown);

    /// <summary>Distribuição por severidade.</summary>
    public sealed record SeverityBreakdown(
        int Critical, int Major, int Minor, int Warning);

    /// <summary>Distribuição por status.</summary>
    public sealed record StatusBreakdown(
        int Open, int Investigating, int Mitigating,
        int Monitoring, int Resolved, int Closed);
}
