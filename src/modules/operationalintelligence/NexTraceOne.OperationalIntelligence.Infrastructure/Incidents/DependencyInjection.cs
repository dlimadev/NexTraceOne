using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.EventHandlers;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Incidents.
/// Utiliza IncidentDbContext com Npgsql e EfIncidentStore como implementação de IIncidentStore.
/// Inclui o motor de correlação dinâmica: IIncidentCorrelationRepository e IChangeIntelligenceReader.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura de Incidents ao container DI.</summary>
    public static IServiceCollection AddIncidentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("IncidentDatabase", "NexTraceOne");

        services.AddDbContext<IncidentDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IIncidentStore, EfIncidentStore>();
        services.AddScoped<IIncidentContextSurface, IncidentContextSurface>();
        services.AddScoped<IOperationalAlertHandler, IncidentAlertHandler>();
        services.AddScoped<IIncidentCorrelationRepository, EfIncidentCorrelationRepository>();
        services.AddScoped<IRunbookRepository, EfRunbookRepository>();
        services.AddScoped<IMitigationWorkflowRepository, EfMitigationWorkflowRepository>();
        services.AddScoped<IMitigationValidationRepository, EfMitigationValidationRepository>();
        services.AddScoped<IChangeIntelligenceReader, EfChangeIntelligenceReader>();
        services.AddScoped<IIntegrationEventHandler<DeploymentEventReceivedEvent>, DeploymentEventReceivedHandler>();

        return services;
    }
}
