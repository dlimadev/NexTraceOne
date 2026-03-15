using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents;

/// <summary>
/// Registra serviços da camada Application do subdomínio Incidents.
/// Inclui: validators para todas as features de correlação de incidentes e mitigação.
/// Os handlers são registados automaticamente via MediatR assembly scanning.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os validadores do subdomínio Incidents ao container DI.</summary>
    public static IServiceCollection AddIncidentsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IValidator<ListIncidents.Query>, ListIncidents.Validator>();
        services.AddTransient<IValidator<GetIncidentDetail.Query>, GetIncidentDetail.Validator>();
        services.AddTransient<IValidator<GetIncidentCorrelation.Query>, GetIncidentCorrelation.Validator>();
        services.AddTransient<IValidator<GetIncidentEvidence.Query>, GetIncidentEvidence.Validator>();
        services.AddTransient<IValidator<GetIncidentMitigation.Query>, GetIncidentMitigation.Validator>();
        services.AddTransient<IValidator<GetIncidentSummary.Query>, GetIncidentSummary.Validator>();
        services.AddTransient<IValidator<ListIncidentsByService.Query>, ListIncidentsByService.Validator>();
        services.AddTransient<IValidator<ListIncidentsByTeam.Query>, ListIncidentsByTeam.Validator>();

        return services;
    }
}
