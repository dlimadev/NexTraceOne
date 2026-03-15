using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
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

    /// <summary>Handler que compõe o resumo agregado.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Simulated aggregation — será calculado via repositório em produção.
            var response = new Response(
                TotalOpen: 3,
                CriticalIncidents: 1,
                WithCorrelatedChanges: 2,
                WithMitigationAvailable: 2,
                ServicesImpacted: 3,
                SeverityBreakdown: new SeverityBreakdown(
                    Critical: 1,
                    Major: 1,
                    Minor: 0,
                    Warning: 1),
                StatusBreakdown: new StatusBreakdown(
                    Open: 0,
                    Investigating: 1,
                    Mitigating: 1,
                    Monitoring: 1,
                    Resolved: 0,
                    Closed: 0));

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
