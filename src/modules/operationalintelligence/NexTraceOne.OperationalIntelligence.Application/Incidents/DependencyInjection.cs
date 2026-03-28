using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateIncidentWithChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetCorrelatedChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Services;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents;

/// <summary>
/// Registra serviços da camada Application do subdomínio Incidents.
/// Inclui: validators para todas as features de correlação de incidentes, mitigação,
/// workflows operacionais, runbooks e validação pós-mitigação.
/// Os handlers são registados automaticamente via MediatR assembly scanning.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os validadores do subdomínio Incidents ao container DI.</summary>
    public static IServiceCollection AddIncidentsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IIncidentCorrelationService, IncidentCorrelationService>();

        // Dynamic correlation engine features
        services.AddTransient<IValidator<CorrelateIncidentWithChanges.Command>, CorrelateIncidentWithChanges.Validator>();
        services.AddTransient<IValidator<GetCorrelatedChanges.Query>, GetCorrelatedChanges.Validator>();

        // Incident features
        services.AddTransient<IValidator<CreateIncident.Command>, CreateIncident.Validator>();
        services.AddTransient<IValidator<ListIncidents.Query>, ListIncidents.Validator>();
        services.AddTransient<IValidator<GetIncidentDetail.Query>, GetIncidentDetail.Validator>();
        services.AddTransient<IValidator<GetIncidentCorrelation.Query>, GetIncidentCorrelation.Validator>();
        services.AddTransient<IValidator<RefreshIncidentCorrelation.Command>, RefreshIncidentCorrelation.Validator>();
        services.AddTransient<IValidator<GetIncidentEvidence.Query>, GetIncidentEvidence.Validator>();
        services.AddTransient<IValidator<GetIncidentMitigation.Query>, GetIncidentMitigation.Validator>();
        services.AddTransient<IValidator<GetIncidentSummary.Query>, GetIncidentSummary.Validator>();
        services.AddTransient<IValidator<ListIncidentsByService.Query>, ListIncidentsByService.Validator>();
        services.AddTransient<IValidator<ListIncidentsByTeam.Query>, ListIncidentsByTeam.Validator>();

        // Mitigation workflow features
        services.AddTransient<IValidator<GetMitigationRecommendations.Query>, GetMitigationRecommendations.Validator>();
        services.AddTransient<IValidator<GetMitigationWorkflow.Query>, GetMitigationWorkflow.Validator>();
        services.AddTransient<IValidator<CreateMitigationWorkflow.Command>, CreateMitigationWorkflow.Validator>();
        services.AddTransient<IValidator<UpdateMitigationWorkflowAction.Command>, UpdateMitigationWorkflowAction.Validator>();
        services.AddTransient<IValidator<GetMitigationHistory.Query>, GetMitigationHistory.Validator>();
        services.AddTransient<IValidator<GetMitigationValidation.Query>, GetMitigationValidation.Validator>();
        services.AddTransient<IValidator<RecordMitigationValidation.Command>, RecordMitigationValidation.Validator>();

        // Runbook features
        services.AddTransient<IValidator<GetRunbookDetail.Query>, GetRunbookDetail.Validator>();
        services.AddTransient<IValidator<ListRunbooks.Query>, ListRunbooks.Validator>();

        return services;
    }
}
