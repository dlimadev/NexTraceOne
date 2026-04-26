using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Governance.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Governance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class GovernanceDbContext(
    DbContextOptions<GovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IGovernanceUnitOfWork
{
    /// <summary>Equipas da organização.</summary>
    public DbSet<Team> Teams => Set<Team>();

    /// <summary>Domínios de governança (áreas de negócio/técnicas).</summary>
    public DbSet<GovernanceDomain> Domains => Set<GovernanceDomain>();

    /// <summary>Pacotes de regras de governança.</summary>
    public DbSet<GovernancePack> Packs => Set<GovernancePack>();

    /// <summary>Versões de pacotes de governança.</summary>
    public DbSet<GovernancePackVersion> PackVersions => Set<GovernancePackVersion>();

    /// <summary>Waivers (exceções) de regras de governança.</summary>
    public DbSet<GovernanceWaiver> Waivers => Set<GovernanceWaiver>();

    /// <summary>Delegações de administração.</summary>
    public DbSet<DelegatedAdministration> DelegatedAdministrations => Set<DelegatedAdministration>();

    /// <summary>Associações equipa-domínio.</summary>
    public DbSet<TeamDomainLink> TeamDomainLinks => Set<TeamDomainLink>();

    /// <summary>Registos de rollout de pacotes de governança.</summary>
    public DbSet<GovernanceRolloutRecord> RolloutRecords => Set<GovernanceRolloutRecord>();

    /// <summary>Pacotes de evidência de governança.</summary>
    public DbSet<EvidencePackage> EvidencePackages => Set<EvidencePackage>();

    /// <summary>Itens de evidência de um pacote de governança.</summary>
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();

    /// <summary>Gaps de compliance persistidos.</summary>
    public DbSet<ComplianceGap> ComplianceGaps => Set<ComplianceGap>();

    /// <summary>Definições de política como código (YAML/JSON) com gradual enforcement.</summary>
    public DbSet<PolicyAsCodeDefinition> PolicyAsCodeDefinitions => Set<PolicyAsCodeDefinition>();

    /// <summary>Resultados de scans de segurança (SAST, contrato, template).</summary>
    public DbSet<SecurityScanResult> SecurityScanResults => Set<SecurityScanResult>();

    /// <summary>Achados individuais de scans de segurança.</summary>
    public DbSet<SecurityFinding> SecurityFindings => Set<SecurityFinding>();

    /// <summary>Dashboards customizados por persona e tenant.</summary>
    public DbSet<CustomDashboard> CustomDashboards => Set<CustomDashboard>();

    /// <summary>Histórico de revisões de dashboards customizados (V3.1).</summary>
    public DbSet<DashboardRevision> DashboardRevisions => Set<DashboardRevision>();

    /// <summary>Itens de dívida técnica registados por serviço.</summary>
    public DbSet<TechnicalDebtItem> TechnicalDebtItems => Set<TechnicalDebtItem>();

    /// <summary>Avaliações de maturidade de serviços.</summary>
    public DbSet<ServiceMaturityAssessment> ServiceMaturityAssessments => Set<ServiceMaturityAssessment>();

    /// <summary>Snapshots de saúde de equipas (Team Health Dashboard).</summary>
    public DbSet<TeamHealthSnapshot> TeamHealthSnapshots => Set<TeamHealthSnapshot>();

    /// <summary>Impactos de custo por mudança (FinOps por mudança).</summary>
    public DbSet<ChangeCostImpact> ChangeCostImpacts => Set<ChangeCostImpact>();

    /// <summary>Briefings executivos gerados por IA.</summary>
    public DbSet<ExecutiveBriefing> ExecutiveBriefings => Set<ExecutiveBriefing>();

    /// <summary>Atribuições de custo operacional por dimensão (FinOps contextual).</summary>
    public DbSet<CostAttribution> CostAttributions => Set<CostAttribution>();

    /// <summary>Relatórios de compliance de licenças de dependências.</summary>
    public DbSet<LicenseComplianceReport> LicenseComplianceReports => Set<LicenseComplianceReport>();

    /// <summary>Pedidos de aprovação de override de orçamento FinOps.</summary>
    public DbSet<FinOpsBudgetApproval> FinOpsBudgetApprovals => Set<FinOpsBudgetApproval>();

    /// <summary>Métricas OpenTelemetry ingeridas pelo OTEL Collector pipeline.</summary>
    public DbSet<OtelMetricRecord> OtelMetrics => Set<OtelMetricRecord>();

    /// <summary>Agendas de ambientes não produtivos.</summary>
    public DbSet<NonProdSchedule> NonProdSchedules => Set<NonProdSchedule>();

    /// <summary>Estado do seed de demonstração por tenant.</summary>
    public DbSet<DemoSeedState> DemoSeedStates => Set<DemoSeedState>();

    /// <summary>Configurações SAML SSO por tenant.</summary>
    public DbSet<SamlSsoConfiguration> SamlSsoConfigurations => Set<SamlSsoConfiguration>();

    /// <summary>Configuração GreenOps por tenant (factor de intensidade, meta ESG, região).</summary>
    public DbSet<GreenOpsConfiguration> GreenOpsConfigurations => Set<GreenOpsConfiguration>();

    /// <summary>Bundles de suporte gerados pela plataforma.</summary>
    public DbSet<SupportBundle> SupportBundles => Set<SupportBundle>();

    /// <summary>Jobs de recovery de dados iniciados via plataforma.</summary>
    public DbSet<RecoveryJob> RecoveryJobs => Set<RecoveryJob>();

    /// <summary>Notebooks operacionais (V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode).</summary>
    public DbSet<Notebook> Notebooks => Set<Notebook>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(GovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "gov_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
