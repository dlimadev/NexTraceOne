using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Infrastructure.Persistence;
using NexTraceOne.AiGovernance.Infrastructure.Persistence.Repositories;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AiGovernance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo AiGovernance.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("AiGovernanceDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<AiGovernanceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AiGovernanceDbContext>());
        services.AddScoped<IAiAccessPolicyRepository, AiAccessPolicyRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IAiBudgetRepository, AiBudgetRepository>();
        services.AddScoped<IAiAssistantConversationRepository, AiAssistantConversationRepository>();
        services.AddScoped<IAiMessageRepository, AiMessageRepository>();
        services.AddScoped<IAiUsageEntryRepository, AiUsageEntryRepository>();
        services.AddScoped<IAiKnowledgeSourceRepository, AiKnowledgeSourceRepository>();
        services.AddScoped<IAiIdeClientRegistrationRepository, AiIdeClientRegistrationRepository>();
        services.AddScoped<IAiIdeCapabilityPolicyRepository, AiIdeCapabilityPolicyRepository>();
        services.AddScoped<IAiRoutingDecisionRepository, AiRoutingDecisionRepository>();
        services.AddScoped<IAiRoutingStrategyRepository, AiRoutingStrategyRepository>();

        return services;
    }
}
