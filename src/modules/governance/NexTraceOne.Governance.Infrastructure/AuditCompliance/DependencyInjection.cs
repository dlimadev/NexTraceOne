using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Governance.Application.AuditCompliance.Abstractions;
using NexTraceOne.Governance.Application.AuditCompliance.Retention;
using NexTraceOne.Governance.Contracts.AuditCompliance.ServiceInterfaces;
using NexTraceOne.Governance.Infrastructure.AuditCompliance.Persistence.Repositories;
using NexTraceOne.Governance.Infrastructure.AuditCompliance.Retention;
using NexTraceOne.Governance.Infrastructure.AuditCompliance.Services;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Infrastructure.AuditCompliance;

/// <summary>
/// Registra serviços de infraestrutura do módulo Audit.
/// O DbContext está consolidado em PlatformGovernanceDbContext (governance Infrastructure).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        services.Configure<AuditRetentionOptions>(
            configuration.GetSection(AuditRetentionOptions.SectionName));

        // IAuditComplianceUnitOfWork satisfied by PlatformGovernanceDbContext (registered in governance Infrastructure)
        services.AddScoped<IAuditComplianceUnitOfWork>(sp => sp.GetRequiredService<PlatformGovernanceDbContext>());
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IAuditChainRepository, AuditChainRepository>();
        services.AddScoped<ICompliancePolicyRepository, CompliancePolicyRepository>();
        services.AddScoped<IAuditCampaignRepository, AuditCampaignRepository>();
        services.AddScoped<IComplianceResultRepository, ComplianceResultRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddScoped<IAuditModule, AuditModuleService>();
        services.AddScoped<IReportRenderer, CompositeReportRenderer>();
        services.AddHostedService<AuditRetentionJob>();

        return services;
    }
}
