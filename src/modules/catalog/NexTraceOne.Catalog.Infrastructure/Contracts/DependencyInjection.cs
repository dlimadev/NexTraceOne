using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.Catalog.Infrastructure.Contracts.EventHandlers;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Contracts.Services;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;

namespace NexTraceOne.Catalog.Infrastructure.Contracts;

/// <summary>
/// Registra serviços de infraestrutura do módulo Contracts.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Contracts ao container DI.</summary>
    public static IServiceCollection AddContractsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ContractsDatabase", "NexTraceOne");

        services.AddDbContext<ContractsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ContractsDbContext>());
        services.AddScoped<IContractsUnitOfWork>(sp => sp.GetRequiredService<ContractsDbContext>());
        services.AddScoped<IContractVersionRepository, ContractVersionRepository>();
        services.AddScoped<IContractDraftRepository, ContractDraftRepository>();
        services.AddScoped<IContractReviewRepository, ContractReviewRepository>();
        services.AddScoped<ISoapContractDetailRepository, SoapContractDetailRepository>();
        services.AddScoped<ISoapDraftMetadataRepository, SoapDraftMetadataRepository>();
        services.AddScoped<IEventContractDetailRepository, EventContractDetailRepository>();
        services.AddScoped<IEventDraftMetadataRepository, EventDraftMetadataRepository>();
        services.AddScoped<IBackgroundServiceContractDetailRepository, BackgroundServiceContractDetailRepository>();
        services.AddScoped<IBackgroundServiceDraftMetadataRepository, BackgroundServiceDraftMetadataRepository>();
        services.AddScoped<IContractDeploymentRepository, ContractDeploymentRepository>();
        services.AddScoped<ICanonicalEntityRepository, CanonicalEntityRepository>();
        services.AddScoped<ICanonicalEntityVersionRepository, CanonicalEntityVersionRepository>();
        services.AddScoped<IConsumerExpectationRepository, ConsumerExpectationRepository>();
        services.AddScoped<IContractHealthScoreRepository, ContractHealthScoreRepository>();
        services.AddScoped<IPipelineExecutionRepository, PipelineExecutionRepository>();
        services.AddScoped<IContractNegotiationRepository, ContractNegotiationRepository>();
        services.AddScoped<INegotiationCommentRepository, NegotiationCommentRepository>();
        services.AddScoped<ISchemaEvolutionAdviceRepository, SchemaEvolutionAdviceRepository>();
        services.AddScoped<ISemanticDiffResultRepository, SemanticDiffResultRepository>();
        services.AddScoped<IContractComplianceGateRepository, ContractComplianceGateRepository>();
        services.AddScoped<IContractComplianceResultRepository, ContractComplianceResultRepository>();
        services.AddScoped<IContractListingRepository, ContractListingRepository>();
        services.AddScoped<IMarketplaceReviewRepository, MarketplaceReviewRepository>();
        services.AddScoped<IImpactSimulationRepository, ImpactSimulationRepository>();
        services.AddScoped<IContractVerificationRepository, ContractVerificationRepository>();
        services.AddScoped<IContractChangelogRepository, ContractChangelogRepository>();
        services.AddScoped<IGraphQlSchemaSnapshotRepository, GraphQlSchemaSnapshotRepository>();
        services.AddScoped<IProtobufSchemaSnapshotRepository, ProtobufSchemaSnapshotRepository>();

        // AI Draft Generator — uses IChatCompletionProvider from AIKnowledge module
        services.AddScoped<IAiDraftGenerator, AiDraftGeneratorService>();
        services.AddScoped<IContractsModule, ContractsModuleService>();

        // ── Integration event handler — Change-to-Contract Impact (INOVACAO-ROADMAP §1.2) ──
        // Regista automaticamente deployments de contrato quando um deploy é concluído com sucesso.
        // Alimenta a detecção de drift entre ambientes (GetContractEnvironmentVersionDrift).
        services.AddScoped<IIntegrationEventHandler<DeploymentCompletedIntegrationEvent>,
            DeploymentCompletedContractImpactHandler>();

        return services;
    }
}
