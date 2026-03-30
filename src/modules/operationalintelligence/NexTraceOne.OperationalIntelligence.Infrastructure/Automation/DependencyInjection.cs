using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Automation.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Services;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Automation.
/// Utiliza AutomationDbContext com Npgsql e repositórios EF Core.
/// Decisão arquitetural: AutomationDbContext próprio para isolamento de boundary.
/// Motivo: Automation tem ciclo de vida independente de Incidents e Reliability.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura de Automation ao container DI.</summary>
    public static IServiceCollection AddAutomationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("AutomationDatabase", "NexTraceOne");

        services.AddDbContext<AutomationDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IAutomationWorkflowRepository, AutomationWorkflowRepository>();
        services.AddScoped<IAutomationValidationRepository, AutomationValidationRepository>();
        services.AddScoped<IAutomationAuditRepository, AutomationAuditRepository>();
        services.AddScoped<IAutomationUnitOfWork>(sp => sp.GetRequiredService<AutomationDbContext>());

        // P03.2 — contrato cross-module de Automation
        services.AddScoped<IAutomationModule, AutomationModuleService>();

        return services;
    }
}
