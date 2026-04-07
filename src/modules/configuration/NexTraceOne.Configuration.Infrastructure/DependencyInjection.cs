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
using NexTraceOne.Configuration.Infrastructure.Repositories;
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

        // In-memory repositories for Phase 3 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<IUserWatchRepository, InMemoryUserWatchRepository>();
        services.AddSingleton<IUserAlertRuleRepository, InMemoryUserAlertRuleRepository>();

        // In-memory repositories for Phase 4 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<IEntityTagRepository, InMemoryEntityTagRepository>();
        services.AddSingleton<IServiceCustomFieldRepository, InMemoryServiceCustomFieldRepository>();
        services.AddSingleton<ITaxonomyRepository, InMemoryTaxonomyRepository>();

        // In-memory repositories for Phase 5 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<IAutomationRuleRepository, InMemoryAutomationRuleRepository>();
        services.AddSingleton<IChangeChecklistRepository, InMemoryChangeChecklistRepository>();
        services.AddSingleton<IContractTemplateRepository, InMemoryContractTemplateRepository>();

        // In-memory repositories for Phase 6 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<IScheduledReportRepository, InMemoryScheduledReportRepository>();

        // In-memory repositories for Phase 7 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<ISavedPromptRepository, InMemorySavedPromptRepository>();

        // In-memory repositories for Phase 8 (MVP1 — persistência PostgreSQL a adicionar futuramente)
        services.AddSingleton<IWebhookTemplateRepository, InMemoryWebhookTemplateRepository>();

        // Seeders — Scoped porque dependem do DbContext (Scoped)
        services.AddScoped<IConfigurationDefinitionSeeder, ConfigurationDefinitionSeeder>();

        // Services
        services.AddScoped<IConfigurationResolutionService, ConfigurationResolutionService>();
        services.AddSingleton<IConfigurationCacheService, ConfigurationCacheService>();
        services.AddSingleton<IConfigurationSecurityService, ConfigurationSecurityService>();

        return services;
    }
}
