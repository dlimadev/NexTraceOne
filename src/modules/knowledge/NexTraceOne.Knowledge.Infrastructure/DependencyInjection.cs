using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;
using NexTraceOne.Knowledge.Infrastructure.Search;

namespace NexTraceOne.Knowledge.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Knowledge.
/// Inclui: DbContext, Repositórios, UnitOfWork, Search Provider.
///
/// P10.1: Criação do módulo backend dedicado de Knowledge Hub.
/// P10.2: Adição do KnowledgeSearchProvider para search cross-module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Knowledge.</summary>
    public static IServiceCollection AddKnowledgeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("KnowledgeDatabase", "NexTraceOne");

        services.AddDbContext<KnowledgeDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // Repositories
        services.AddScoped<IKnowledgeDocumentRepository, KnowledgeDocumentRepository>();
        services.AddScoped<IOperationalNoteRepository, OperationalNoteRepository>();
        services.AddScoped<IKnowledgeRelationRepository, KnowledgeRelationRepository>();

        // Cross-module search provider
        services.AddScoped<IKnowledgeSearchProvider, KnowledgeSearchProvider>();

        return services;
    }
}
