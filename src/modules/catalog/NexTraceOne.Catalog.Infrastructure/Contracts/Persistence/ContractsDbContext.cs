using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;

/// <summary>
/// DbContext do módulo Contracts.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Suporta multi-protocolo: OpenAPI, Swagger, WSDL, AsyncAPI e formatos futuros.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ContractsDbContext(
    DbContextOptions<ContractsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IContractsUnitOfWork
{
    /// <summary>Versões de contrato multi-protocolo persistidas no módulo Contracts.</summary>
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    /// <summary>Diffs semânticos entre versões de contrato persistidos no módulo Contracts.</summary>
    public DbSet<ContractDiff> ContractDiffs => Set<ContractDiff>();

    /// <summary>Violações de ruleset detectadas em versões de contrato.</summary>
    public DbSet<ContractRuleViolation> ContractRuleViolations => Set<ContractRuleViolation>();

    /// <summary>Artefatos gerados a partir de versões de contrato (testes, scaffolds, evidências).</summary>
    public DbSet<ContractArtifact> ContractArtifacts => Set<ContractArtifact>();

    /// <summary>Drafts de contrato em edição no Contract Studio.</summary>
    public DbSet<ContractDraft> Drafts => Set<ContractDraft>();

    /// <summary>Revisões de drafts de contrato para rastreabilidade do fluxo de aprovação.</summary>
    public DbSet<ContractReview> Reviews => Set<ContractReview>();

    /// <summary>Exemplos associados a drafts ou versões publicadas de contrato.</summary>
    public DbSet<ContractExample> Examples => Set<ContractExample>();

    /// <summary>Rulesets de linting para governança de contratos (independente de vendor).</summary>
    public DbSet<ContractLintRuleset> ContractLintRulesets => Set<ContractLintRuleset>();

    /// <summary>Entidades canónicas reutilizáveis (schemas/modelos padrão).</summary>
    public DbSet<CanonicalEntity> CanonicalEntities => Set<CanonicalEntity>();

    /// <summary>Versões imutáveis de entidades canónicas para histórico e diff.</summary>
    public DbSet<CanonicalEntityVersion> CanonicalEntityVersions => Set<CanonicalEntityVersion>();

    /// <summary>Scorecards de avaliação técnica de versões de contrato.</summary>
    public DbSet<ContractScorecard> ContractScorecards => Set<ContractScorecard>();

    /// <summary>Pacotes de evidência associados a mudanças contratuais.</summary>
    public DbSet<ContractEvidencePack> ContractEvidencePacks => Set<ContractEvidencePack>();

    /// <summary>Detalhes SOAP/WSDL específicos de versões de contrato publicadas (Protocol = Wsdl).</summary>
    public DbSet<SoapContractDetail> SoapContractDetails => Set<SoapContractDetail>();

    /// <summary>Metadados SOAP/WSDL específicos de drafts de contrato em edição (ContractType = Soap).</summary>
    public DbSet<SoapDraftMetadata> SoapDraftMetadata => Set<SoapDraftMetadata>();

    /// <summary>Detalhes AsyncAPI específicos de versões de contrato publicadas (Protocol = AsyncApi).</summary>
    public DbSet<EventContractDetail> EventContractDetails => Set<EventContractDetail>();

    /// <summary>Metadados AsyncAPI específicos de drafts de contrato em edição (ContractType = Event).</summary>
    public DbSet<EventDraftMetadata> EventDraftMetadata => Set<EventDraftMetadata>();

    /// <summary>Detalhes de Background Service Contracts publicados (ContractType = BackgroundService).</summary>
    public DbSet<BackgroundServiceContractDetail> BackgroundServiceContractDetails => Set<BackgroundServiceContractDetail>();

    /// <summary>Metadados de Background Service para drafts de contrato em edição (ContractType = BackgroundService).</summary>
    public DbSet<BackgroundServiceDraftMetadata> BackgroundServiceDraftMetadata => Set<BackgroundServiceDraftMetadata>();

    /// <summary>Deployments de versões de contrato por ambiente para rastreabilidade de mudanças.</summary>
    public DbSet<ContractDeployment> ContractDeployments => Set<ContractDeployment>();

    /// <summary>Expectativas de consumidores para Consumer-Driven Contract Testing (CDCT).</summary>
    public DbSet<ConsumerExpectation> ConsumerExpectations => Set<ConsumerExpectation>();

    /// <summary>Scores de saúde contínuos de contratos (API Assets).</summary>
    public DbSet<ContractHealthScore> ContractHealthScores => Set<ContractHealthScore>();

    /// <summary>Execuções de pipeline de geração de código a partir de contratos.</summary>
    public DbSet<PipelineExecution> PipelineExecutions => Set<PipelineExecution>();

    /// <summary>Negociações cross-team de contratos para aprovação colaborativa.</summary>
    public DbSet<ContractNegotiation> ContractNegotiations => Set<ContractNegotiation>();

    /// <summary>Comentários em negociações de contratos para revisão colaborativa.</summary>
    public DbSet<NegotiationComment> NegotiationComments => Set<NegotiationComment>();

    /// <summary>Análises de evolução de schema entre versões de contratos (API Assets).</summary>
    public DbSet<SchemaEvolutionAdvice> SchemaEvolutionAdvices => Set<SchemaEvolutionAdvice>();

    /// <summary>Resultados de diff semântico assistido por IA entre versões de contrato.</summary>
    public DbSet<SemanticDiffResult> SemanticDiffResults => Set<SemanticDiffResult>();

    /// <summary>Listagens de contratos publicados no marketplace interno.</summary>
    public DbSet<ContractListing> ContractListings => Set<ContractListing>();

    /// <summary>Avaliações de contratos publicados no marketplace interno.</summary>
    public DbSet<MarketplaceReview> MarketplaceReviews => Set<MarketplaceReview>();

    /// <summary>Gates de compliance contratual configuráveis por organização, equipa ou ambiente.</summary>
    public DbSet<ContractComplianceGate> ContractComplianceGates => Set<ContractComplianceGate>();

    /// <summary>Resultados de avaliação de compliance contratual contra gates configurados.</summary>
    public DbSet<ContractComplianceResult> ContractComplianceResults => Set<ContractComplianceResult>();

    /// <summary>Simulações de impacto de dependências entre serviços para cenários what-if.</summary>
    public DbSet<ImpactSimulation> ImpactSimulations => Set<ImpactSimulation>();

    /// <summary>Registos de verificação de contrato provenientes de CI/CD.</summary>
    public DbSet<ContractVerification> ContractVerifications => Set<ContractVerification>();

    /// <summary>Entradas de changelog de evolução contratual.</summary>
    public DbSet<ContractChangelog> ContractChangelogs => Set<ContractChangelog>();

    /// <summary>Snapshots analisados de schemas GraphQL para diff semântico e auditoria. Wave G.3.</summary>
    public DbSet<GraphQlSchemaSnapshot> GraphQlSchemaSnapshots => Set<GraphQlSchemaSnapshot>();

    /// <summary>Snapshots analisados de schemas Protobuf para diff semântico e auditoria. Wave H.1.</summary>
    public DbSet<ProtobufSchemaSnapshot> ProtobufSchemaSnapshots => Set<ProtobufSchemaSnapshot>();

    /// <summary>Schemas de Data Contracts (tabelas/vistas/streams analíticos) com classificação PII. CC-03.</summary>
    public DbSet<DataContractSchema> DataContractSchemas => Set<DataContractSchema>();

    /// <summary>Inventário de consumidores reais de contratos derivado de traces OTel. CC-04.</summary>
    public DbSet<ContractConsumerInventory> ContractConsumerInventories => Set<ContractConsumerInventory>();

    /// <summary>Propostas de breaking change com workflow de consulta de consumidores. CC-06.</summary>
    public DbSet<BreakingChangeProposal> BreakingChangeProposals => Set<BreakingChangeProposal>();

    /// <summary>Registos de SBOM de serviços para análise de supply chain. Wave AO.1.</summary>
    public DbSet<SbomRecord> SbomRecords => Set<SbomRecord>();

    /// <summary>Registos de data contracts de serviços para compliance analítico. Wave AQ.1.</summary>
    public DbSet<DataContractRecord> DataContractRecords => Set<DataContractRecord>();

    /// <summary>Agendamentos de deprecação de contratos com guia de migração. Wave AV.3.</summary>
    public DbSet<IDeprecationScheduleRepository.DeprecationScheduleRecord> DeprecationSchedules
        => Set<IDeprecationScheduleRepository.DeprecationScheduleRecord>();

    /// <summary>Estado actual de feature flags por serviço e tenant. Wave AS.1.</summary>
    public DbSet<FeatureFlagRecord> FeatureFlagRecords => Set<FeatureFlagRecord>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ContractsDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "ctr_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
