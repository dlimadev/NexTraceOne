using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Repositories;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI;

/// <summary>
/// Registra serviços de infraestrutura do módulo ExternalAi.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddExternalAiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ExternalAiDatabase", "NexTraceOne");

        services.AddDbContext<ExternalAiDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);

            if (string.Equals(
                Environment.GetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            {
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<TenantRlsInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ExternalAiDbContext>());
        services.AddScoped<IKnowledgeCaptureRepository, KnowledgeCaptureRepository>();
        services.AddScoped<IExternalAiConsultationRepository, ExternalAiConsultationRepository>();
        services.AddScoped<IExternalAiPolicyRepository, ExternalAiPolicyRepository>();
        services.AddScoped<IExternalAiProviderRepository, ExternalAiProviderRepository>();

        return services;
    }
}
