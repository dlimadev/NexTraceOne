using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;

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
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
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

    /// <summary>Itens de dívida técnica registados por serviço.</summary>
    public DbSet<TechnicalDebtItem> TechnicalDebtItems => Set<TechnicalDebtItem>();

    /// <summary>Avaliações de maturidade de serviços.</summary>
    public DbSet<ServiceMaturityAssessment> ServiceMaturityAssessments => Set<ServiceMaturityAssessment>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(GovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "gov_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
