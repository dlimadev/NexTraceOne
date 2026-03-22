using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;

/// <summary>
/// Feature: GetDomainReliabilitySummary — resumo agregado de confiabilidade por domínio de negócio.
/// Baseia-se em dados reais de incidentes com ImpactedDomain == domainId.
/// </summary>
public static class GetDomainReliabilitySummary
{
    public sealed record Query(string DomainId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DomainId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilityIncidentSurface incidentSurface,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var incidents = await incidentSurface.GetDomainIncidentsAsync(request.DomainId, tenant.Id, cancellationToken);

            var serviceIds = incidents.Select(i => i.ServiceId).Distinct().ToList();
            var totalServices = serviceIds.Count;

            var criticalServices = incidents
                .Where(i => i.Severity == IncidentSeverity.Critical.ToString())
                .Select(i => i.ServiceId)
                .Distinct()
                .Count();

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
                request.DomainId,
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
        string DomainId,
        int TotalServices,
        int HealthyServices,
        int DegradedServices,
        int UnavailableServices,
        int NeedsAttentionServices,
        int CriticalServicesImpacted);
}
