using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;

/// <summary>
/// Feature: GetTeamReliabilitySummary — resumo agregado de confiabilidade dos serviços de uma equipa.
/// Baseia-se em dados reais de incidentes associados ao teamId.
/// </summary>
public static class GetTeamReliabilitySummary
{
    public sealed record Query(string TeamId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilityIncidentSurface incidentSurface,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var incidents = await incidentSurface.GetTeamIncidentsAsync(request.TeamId, tenant.Id, cancellationToken);

            var serviceIds = incidents.Select(i => i.ServiceId).Distinct().ToList();
            var totalServices = serviceIds.Count;

            var criticalServices = incidents
                .Where(i => i.Severity == IncidentSeverity.Critical.ToString())
                .Select(i => i.ServiceId)
                .Distinct()
                .Count();

            // Classificação simplificada por serviço baseada em incidentes activos.
            var unavailableCount = incidents
                .Where(i => i.Severity == IncidentSeverity.Critical.ToString() &&
                    (i.Status == IncidentStatus.Open.ToString() || i.Status == IncidentStatus.Investigating.ToString()))
                .Select(i => i.ServiceId).Distinct().Count();

            var degradedCount = incidents
                .Where(i => i.Severity == IncidentSeverity.Major.ToString())
                .Select(i => i.ServiceId).Distinct().Count();

            var needsAttentionCount = incidents
                .Where(i => i.Severity == IncidentSeverity.Minor.ToString() || i.Severity == IncidentSeverity.Warning.ToString())
                .Select(i => i.ServiceId).Distinct().Count();

            var healthyCount = Math.Max(0, totalServices - unavailableCount - degradedCount - needsAttentionCount);

            var response = new Response(
                request.TeamId,
                totalServices,
                healthyCount,
                degradedCount,
                unavailableCount,
                needsAttentionCount,
                criticalServices);

            return Result<Response>.Success(response);
        }
    }

    public sealed record Response(
        string TeamId,
        int TotalServices,
        int HealthyServices,
        int DegradedServices,
        int UnavailableServices,
        int NeedsAttentionServices,
        int CriticalServicesImpacted);
}
