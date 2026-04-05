using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Retention;
using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence.Repositories;
using NexTraceOne.AuditCompliance.Infrastructure.Retention;
using NexTraceOne.AuditCompliance.Infrastructure.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AuditCompliance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Audit.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("AuditDatabase", "NexTraceOne");

        services.AddDbContext<AuditDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.Configure<AuditRetentionOptions>(
            configuration.GetSection(AuditRetentionOptions.SectionName));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AuditDbContext>());
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IAuditChainRepository, AuditChainRepository>();
        services.AddScoped<ICompliancePolicyRepository, CompliancePolicyRepository>();
        services.AddScoped<IAuditCampaignRepository, AuditCampaignRepository>();
        services.AddScoped<IComplianceResultRepository, ComplianceResultRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddScoped<IAuditModule, AuditModuleService>();
        services.AddScoped<IReportRenderer, JsonReportRenderer>();
        services.AddHostedService<AuditRetentionJob>();

        return services;
    }
}
