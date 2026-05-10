using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Services;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience;

/// <summary>
/// Registra serviços de infraestrutura do subdomínio Developer Experience (surveys e NPS).
/// Inclui: DbContext, UoW, Repositório de surveys.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDeveloperExperienceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<DeveloperExperienceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DeveloperExperienceDbContext>());
        services.AddScoped<IDeveloperExperienceUnitOfWork>(sp => sp.GetRequiredService<DeveloperExperienceDbContext>());
        services.AddScoped<IDeveloperSurveyRepository, EfDeveloperSurveyRepository>();
        services.AddScoped<IIdeContextReader, NullIdeContextReader>();
        services.AddScoped<IIDEUsageRepository, EfIdeUsageRepository>();

        return services;
    }
}
