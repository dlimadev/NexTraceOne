using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Infrastructure.Persistence;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;
using NexTraceOne.Catalog.Domain.Entities;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Knowledge.Entities;
using NexTraceOne.Catalog.Domain.ProductAnalytics.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence;

/// <summary>
/// DbContext consolidado do módulo ServiceCatalog.
/// Unifica ServiceCatalogDbContext + ServiceCatalogDbContext + ServiceCatalogDbContext +
/// ServiceCatalogDbContext + ServiceCatalogDbContext + ServiceCatalogDbContext + ServiceCatalogDbContext.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ServiceCatalogDbContext(
    DbContextOptions<ServiceCatalogDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock),
      IUnitOfWork,
      ICatalogGraphUnitOfWork,
      IContractsUnitOfWork,
      IDependencyGovernanceUnitOfWork,
      IDeveloperExperienceUnitOfWork,
      ILegacyAssetsUnitOfWork,
      ITemplatesUnitOfWork,
      IPortalUnitOfWork
{
    // ── Catalog Graph ─────────────────────────────────────────────────────────
    public DbSet<ApiAsset> ApiAssets => Set<ApiAsset>();
    public DbSet<ServiceAsset> ServiceAssets => Set<ServiceAsset>();
    public DbSet<ConsumerRelationship> ConsumerRelationships => Set<ConsumerRelationship>();
    public DbSet<ConsumerAsset> ConsumerAssets => Set<ConsumerAsset>();
    public DbSet<DiscoverySource> DiscoverySources => Set<DiscoverySource>();
    public DbSet<GraphSnapshot> GraphSnapshots => Set<GraphSnapshot>();
    public DbSet<NodeHealthRecord> NodeHealthRecords => Set<NodeHealthRecord>();
    public DbSet<SavedGraphView> SavedGraphViews => Set<SavedGraphView>();
    public DbSet<LinkedReference> LinkedReferences => Set<LinkedReference>();
    public DbSet<ServiceLink> ServiceLinks => Set<ServiceLink>();
    public DbSet<DiscoveredService> DiscoveredServices => Set<DiscoveredService>();
    public DbSet<DiscoveryRun> DiscoveryRuns => Set<DiscoveryRun>();
    public DbSet<DiscoveryMatchRule> DiscoveryMatchRules => Set<DiscoveryMatchRule>();
    public DbSet<FrameworkAssetDetail> FrameworkAssetDetails => Set<FrameworkAssetDetail>();
    public DbSet<ServiceInterface> ServiceInterfaces => Set<ServiceInterface>();
    public DbSet<ContractBinding> ContractBindings => Set<ContractBinding>();
    public DbSet<AssetDeploymentState> AssetDeploymentStates => Set<AssetDeploymentState>();
    public DbSet<DxScore> DxScores => Set<DxScore>();
    public DbSet<ProductivitySnapshot> ProductivitySnapshots => Set<ProductivitySnapshot>();

    // ── Contracts ─────────────────────────────────────────────────────────────
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();
    public DbSet<ContractDiff> ContractDiffs => Set<ContractDiff>();
    public DbSet<ContractRuleViolation> ContractRuleViolations => Set<ContractRuleViolation>();
    public DbSet<ContractArtifact> ContractArtifacts => Set<ContractArtifact>();
    public DbSet<ContractDraft> Drafts => Set<ContractDraft>();
    public DbSet<ContractReview> Reviews => Set<ContractReview>();
    public DbSet<ContractExample> Examples => Set<ContractExample>();
    public DbSet<ContractLintRuleset> ContractLintRulesets => Set<ContractLintRuleset>();
    public DbSet<CanonicalEntity> CanonicalEntities => Set<CanonicalEntity>();
    public DbSet<CanonicalEntityVersion> CanonicalEntityVersions => Set<CanonicalEntityVersion>();
    public DbSet<ContractScorecard> ContractScorecards => Set<ContractScorecard>();
    public DbSet<ContractEvidencePack> ContractEvidencePacks => Set<ContractEvidencePack>();
    public DbSet<SoapContractDetail> SoapContractDetails => Set<SoapContractDetail>();
    public DbSet<SoapDraftMetadata> SoapDraftMetadata => Set<SoapDraftMetadata>();
    public DbSet<EventContractDetail> EventContractDetails => Set<EventContractDetail>();
    public DbSet<EventDraftMetadata> EventDraftMetadata => Set<EventDraftMetadata>();
    public DbSet<BackgroundServiceContractDetail> BackgroundServiceContractDetails => Set<BackgroundServiceContractDetail>();
    public DbSet<BackgroundServiceDraftMetadata> BackgroundServiceDraftMetadata => Set<BackgroundServiceDraftMetadata>();
    public DbSet<ContractDeployment> ContractDeployments => Set<ContractDeployment>();
    public DbSet<ConsumerExpectation> ConsumerExpectations => Set<ConsumerExpectation>();
    public DbSet<ContractHealthScore> ContractHealthScores => Set<ContractHealthScore>();
    public DbSet<PipelineExecution> PipelineExecutions => Set<PipelineExecution>();
    public DbSet<ContractNegotiation> ContractNegotiations => Set<ContractNegotiation>();
    public DbSet<NegotiationComment> NegotiationComments => Set<NegotiationComment>();
    public DbSet<SchemaEvolutionAdvice> SchemaEvolutionAdvices => Set<SchemaEvolutionAdvice>();
    public DbSet<SemanticDiffResult> SemanticDiffResults => Set<SemanticDiffResult>();
    public DbSet<ContractListing> ContractListings => Set<ContractListing>();
    public DbSet<MarketplaceReview> MarketplaceReviews => Set<MarketplaceReview>();
    public DbSet<ContractComplianceGate> ContractComplianceGates => Set<ContractComplianceGate>();
    public DbSet<ContractComplianceResult> ContractComplianceResults => Set<ContractComplianceResult>();
    public DbSet<ImpactSimulation> ImpactSimulations => Set<ImpactSimulation>();
    public DbSet<ContractVerification> ContractVerifications => Set<ContractVerification>();
    public DbSet<ContractChangelog> ContractChangelogs => Set<ContractChangelog>();
    public DbSet<GraphQlSchemaSnapshot> GraphQlSchemaSnapshots => Set<GraphQlSchemaSnapshot>();
    public DbSet<ProtobufSchemaSnapshot> ProtobufSchemaSnapshots => Set<ProtobufSchemaSnapshot>();
    public DbSet<DataContractSchema> DataContractSchemas => Set<DataContractSchema>();
    public DbSet<ContractConsumerInventory> ContractConsumerInventories => Set<ContractConsumerInventory>();
    public DbSet<BreakingChangeProposal> BreakingChangeProposals => Set<BreakingChangeProposal>();
    public DbSet<SbomRecord> SbomRecords => Set<SbomRecord>();
    public DbSet<DataContractRecord> DataContractRecords => Set<DataContractRecord>();
    public DbSet<DeprecationScheduleRecord> DeprecationSchedules => Set<DeprecationScheduleRecord>();
    public DbSet<FeatureFlagRecord> FeatureFlagRecords => Set<FeatureFlagRecord>();
    public DbSet<CodeQualityRecord> CodeQualityRecords => Set<CodeQualityRecord>();

    // ── Dependency Governance ─────────────────────────────────────────────────
    public DbSet<ServiceDependencyProfile> ServiceDependencyProfiles => Set<ServiceDependencyProfile>();
    public DbSet<PackageDependency> PackageDependencies => Set<PackageDependency>();
    public DbSet<VulnerabilityAdvisoryRecord> VulnerabilityAdvisoryRecords => Set<VulnerabilityAdvisoryRecord>();

    // ── Legacy Assets ─────────────────────────────────────────────────────────
    public DbSet<MainframeSystem> MainframeSystems => Set<MainframeSystem>();
    public DbSet<CobolProgram> CobolPrograms => Set<CobolProgram>();
    public DbSet<Copybook> Copybooks => Set<Copybook>();
    public DbSet<CopybookField> CopybookFields => Set<CopybookField>();
    public DbSet<CicsTransaction> CicsTransactions => Set<CicsTransaction>();
    public DbSet<ImsTransaction> ImsTransactions => Set<ImsTransaction>();
    public DbSet<Db2Artifact> Db2Artifacts => Set<Db2Artifact>();
    public DbSet<ZosConnectBinding> ZosConnectBindings => Set<ZosConnectBinding>();
    public DbSet<CopybookProgramUsage> CopybookProgramUsages => Set<CopybookProgramUsage>();
    public DbSet<LegacyDependency> LegacyDependencies => Set<LegacyDependency>();
    public DbSet<CopybookVersion> CopybookVersions => Set<CopybookVersion>();
    public DbSet<CopybookDiffRecord> CopybookDiffs => Set<CopybookDiffRecord>();
    public DbSet<MqMessageContract> MqMessageContracts => Set<MqMessageContract>();
    public DbSet<CopybookContractMapping> CopybookContractMappings => Set<CopybookContractMapping>();

    // ── Templates ─────────────────────────────────────────────────────────────
    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();

    // ── Developer Portal ──────────────────────────────────────────────────────
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PlaygroundSession> PlaygroundSessions => Set<PlaygroundSession>();
    public DbSet<CodeGenerationRecord> CodeGenerationRecords => Set<CodeGenerationRecord>();
    public DbSet<PortalAnalyticsEvent> PortalAnalyticsEvents => Set<PortalAnalyticsEvent>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<ContractPublicationEntry> ContractPublications => Set<ContractPublicationEntry>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RateLimitPolicy> RateLimitPolicies => Set<RateLimitPolicy>();

    // ── Developer Experience ──────────────────────────────────────────────────
    public DbSet<DeveloperSurvey> DeveloperSurveys => Set<DeveloperSurvey>();
    public DbSet<IIDEUsageRepository.IdeUsageRecord> IdeUsageRecords
        => Set<IIDEUsageRepository.IdeUsageRecord>();

    // ── Knowledge (consolidated from KnowledgeDbContext) ─────────────────────
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<OperationalNote> OperationalNotes => Set<OperationalNote>();
    public DbSet<KnowledgeRelation> KnowledgeRelations => Set<KnowledgeRelation>();
    public DbSet<KnowledgeGraphSnapshot> KnowledgeGraphSnapshots => Set<KnowledgeGraphSnapshot>();
    public DbSet<ProposedRunbook> ProposedRunbooks => Set<ProposedRunbook>();

    // ── ProductAnalytics (consolidated from ProductAnalyticsDbContext) ────────
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<JourneyDefinition> JourneyDefinitions => Set<JourneyDefinition>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => !entityType.IsOwned() && entityType.ClrType != typeof(OutboxMessage))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Todo método de leitura multi-tenant filtra por TenantId (defesa em
            // profundidade); sem índice essas consultas degradam para seq scan.
            var tenantId = entityType.FindProperty("TenantId");
            if (tenantId is not null && !entityType.GetIndexes().Any(i => i.Properties.Contains(tenantId)))
                entityType.AddIndex(tenantId);

            // As entidades documentam RowVersion como token xmin, mas sem IsRowVersion
            // o EF persistia uma coluna bigint inerte sem detecção de concorrência.
            var rowVersion = entityType.FindProperty("RowVersion");
            if (rowVersion is not null && rowVersion.ClrType == typeof(uint) && !rowVersion.IsConcurrencyToken)
                modelBuilder.Entity(entityType.ClrType).Property("RowVersion").IsRowVersion();
        }
    }

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ServiceCatalogDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace => null;

    /// <inheritdoc />
    protected override string OutboxTableName => "cat_hub_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
