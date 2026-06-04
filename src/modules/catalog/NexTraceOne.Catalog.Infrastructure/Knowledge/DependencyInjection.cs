using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.Knowledge.Abstractions;
using NexTraceOne.Catalog.Contracts;
using NexTraceOne.Catalog.Infrastructure.Knowledge.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Knowledge.Search;

namespace NexTraceOne.Catalog.Infrastructure.Knowledge;

/// <summary>
/// Registra serviços de infraestrutura do módulo Knowledge.
/// Inclui: Repositórios, Search Provider.
/// DbContext consolidado em ServiceCatalogDbContext (Phase 7).
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
        // Repositories
        services.AddScoped<IKnowledgeDocumentRepository, KnowledgeDocumentRepository>();
        services.AddScoped<IOperationalNoteRepository, OperationalNoteRepository>();
        services.AddScoped<IKnowledgeRelationRepository, KnowledgeRelationRepository>();
        services.AddScoped<IKnowledgeGraphSnapshotRepository, KnowledgeGraphSnapshotRepository>();

        // Cross-module search provider
        services.AddScoped<IKnowledgeSearchProvider, KnowledgeSearchProvider>();
        services.AddScoped<IRunbookKnowledgeLinkingService, RunbookKnowledgeLinkingService>();
        services.AddScoped<IProposedRunbookRepository, ProposedRunbookRepository>();

        // Cross-module contract: IKnowledgeModule — consumido pelo Governance e AI para métricas de conhecimento
        services.AddScoped<IKnowledgeModule, Services.KnowledgeModuleService>();

        return services;
    }
}
