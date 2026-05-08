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

        // CC-03: Data Contract Schema Analysis
        services.AddScoped<IDataContractSchemaRepository, DataContractSchemaRepository>();

        // CC-04: Contract Consumer Inventory (OTel-derived)
        services.AddScoped<IContractConsumerInventoryRepository, ContractConsumerInventoryRepository>();

        // CC-06: Breaking Change Proposal Workflow
        services.AddScoped<IBreakingChangeProposalRepository, BreakingChangeProposalRepository>();

        // AI Draft Generator — uses IChatCompletionProvider from AIKnowledge module
        services.AddScoped<IAiDraftGenerator, AiDraftGeneratorService>();
        services.AddScoped<IContractsModule, ContractsModuleService>();

        // ── Integration event handler — Change-to-Contract Impact (INOVACAO-ROADMAP §1.2) ──
        // Regista automaticamente deployments de contrato quando um deploy é concluído com sucesso.
        // Alimenta a detecção de drift entre ambientes (GetContractEnvironmentVersionDrift).
        services.AddScoped<IIntegrationEventHandler<DeploymentCompletedIntegrationEvent>,
            DeploymentCompletedContractImpactHandler>();

        // ── Wave AB.2 — Contract Lineage Report (null reader) ─────────────
        services.AddScoped<IContractVersionHistoryReader, NullContractVersionHistoryReader>();

        // ── Wave AE.1 — Contract Test Coverage Report (null reader) ───────
        services.AddScoped<IContractTestReader, NexTraceOne.Catalog.Application.Contracts.NullContractTestReader>();

        // ── Wave AE.2 — Schema Breaking Change Impact Report (null reader) ─
        services.AddScoped<IBreakingChangeImpactReader, NexTraceOne.Catalog.Application.Contracts.NullBreakingChangeImpactReader>();

        // ── Wave AE.3 — API Backward Compatibility Report (null reader) ───
        services.AddScoped<IContractCompatibilityReader, NexTraceOne.Catalog.Application.Contracts.NullContractCompatibilityReader>();

        // ── Wave AS — Event Contract Analytics (null readers) ─────────────
        services.AddScoped<IEventSchemaEvolutionReader, NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventSchemaEvolutionReader>();
        services.AddScoped<IEventProducerConsumerReader, NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventProducerConsumerReader>();
        services.AddScoped<IEventComplianceReader, NexTraceOne.Catalog.Application.Contracts.Abstractions.NullEventComplianceReader>();

        return services;
    }
}
