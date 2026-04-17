using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Infrastructure.Persistence;
using NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;
using NexTraceOne.Configuration.Infrastructure.Seed;
using NexTraceOne.Configuration.Infrastructure.Services;

namespace NexTraceOne.Configuration.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Configuration.
/// Inclui: DbContext, Repositórios, UnitOfWork, serviços de resolução, cache e segurança.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Configuration.</summary>
    public static IServiceCollection AddConfigurationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ConfigurationDatabase", "NexTraceOne");

        services.AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ConfigurationDbContext>());

        // Repositories
        services.AddScoped<IConfigurationDefinitionRepository, ConfigurationDefinitionRepository>();
        services.AddScoped<IConfigurationEntryRepository, ConfigurationEntryRepository>();
        services.AddScoped<IConfigurationAuditRepository, ConfigurationAuditRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IUserSavedViewRepository, UserSavedViewRepository>();
        services.AddScoped<IUserBookmarkRepository, UserBookmarkRepository>();

        // Phase 3: Watch Lists & Alert Rules — PostgreSQL persistence
        services.AddScoped<IUserWatchRepository, UserWatchRepository>();
        services.AddScoped<IUserAlertRuleRepository, UserAlertRuleRepository>();

        // Phase 4: Tags, Custom Fields & Taxonomies — PostgreSQL persistence
        services.AddScoped<IEntityTagRepository, EntityTagRepository>();
        services.AddScoped<IServiceCustomFieldRepository, ServiceCustomFieldRepository>();
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();

        // Phase 5: Automation, Checklists & Contract Templates — PostgreSQL persistence
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IChangeChecklistRepository, ChangeChecklistRepository>();
        services.AddScoped<IContractTemplateRepository, ContractTemplateRepository>();

        // Phase 6: Scheduled Reports — PostgreSQL persistence
        services.AddScoped<IScheduledReportRepository, ScheduledReportRepository>();

        // Phase 7: Saved Prompts — PostgreSQL persistence
        services.AddScoped<ISavedPromptRepository, SavedPromptRepository>();

        // Phase 8: Webhook Templates — PostgreSQL persistence
        services.AddScoped<IWebhookTemplateRepository, WebhookTemplateRepository>();

        // Phase 9: Contract Compliance Policies — PostgreSQL persistence
        services.AddScoped<IContractCompliancePolicyRepository, ContractCompliancePolicyRepository>();

        // Seeders — Scoped porque dependem do DbContext (Scoped)
        services.AddScoped<IConfigurationDefinitionSeeder, ConfigurationDefinitionSeeder>();
        services.AddScoped<IFeatureFlagDefinitionSeeder, FeatureFlagDefinitionSeeder>();

        // Services
        services.AddScoped<IConfigurationResolutionService, ConfigurationResolutionService>();
        services.AddScoped<IEnvironmentBehaviorService, EnvironmentBehaviorService>();
        services.AddSingleton<IConfigurationCacheService, ConfigurationCacheService>();
        services.AddSingleton<IConfigurationSecurityService, ConfigurationSecurityService>();

        // Feature Flag Runtime — in-process evaluation with TTL cache
        services.AddScoped<IFeatureFlagRuntime, FeatureFlagRuntime>();

        return services;
    }
}
