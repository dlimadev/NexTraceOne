using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.Workflow.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Services;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow;

/// <summary>
/// Registra serviços de infraestrutura do módulo Workflow.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Workflow ao container DI.</summary>
    public static IServiceCollection AddWorkflowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("WorkflowDatabase", "NexTraceOne");

        services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WorkflowDbContext>());
        services.AddScoped<IWorkflowUnitOfWork>(sp => sp.GetRequiredService<WorkflowDbContext>());
        services.AddScoped<IWorkflowTemplateRepository, WorkflowTemplateRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowStageRepository, WorkflowStageRepository>();
        services.AddScoped<IEvidencePackRepository, EvidencePackRepository>();
        services.AddScoped<IApprovalDecisionRepository, ApprovalDecisionRepository>();
        services.AddScoped<ISlaPolicyRepository, SlaPolicyRepository>();
        services.AddScoped<IWorkflowModule, WorkflowModuleService>();

        return services;
    }
}
