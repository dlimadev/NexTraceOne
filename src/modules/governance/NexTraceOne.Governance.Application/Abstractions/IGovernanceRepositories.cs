using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Interface do repositório de Teams para o módulo Governance.
/// Define operações CRUD e consultas para equipas.
/// </summary>
public interface ITeamRepository
{
    /// <summary>Lista todas as equipas, opcionalmente filtradas por status.</summary>
    Task<IReadOnlyList<Team>> ListAsync(TeamStatus? status, CancellationToken ct);

    /// <summary>Obtém uma equipa pelo seu identificador.</summary>
    Task<Team?> GetByIdAsync(TeamId id, CancellationToken ct);

    /// <summary>Obtém uma equipa pelo seu nome técnico.</summary>
    Task<Team?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona uma nova equipa ao repositório.</summary>
    Task AddAsync(Team team, CancellationToken ct);

    /// <summary>Atualiza uma equipa existente.</summary>
    Task UpdateAsync(Team team, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de GovernanceDomains para o módulo Governance.
/// Define operações CRUD e consultas para domínios de governança.
/// </summary>
public interface IGovernanceDomainRepository
{
    /// <summary>Lista todos os domínios, opcionalmente filtrados por criticidade.</summary>
    Task<IReadOnlyList<GovernanceDomain>> ListAsync(DomainCriticality? criticality, CancellationToken ct);

    /// <summary>Obtém um domínio pelo seu identificador.</summary>
    Task<GovernanceDomain?> GetByIdAsync(GovernanceDomainId id, CancellationToken ct);

    /// <summary>Obtém um domínio pelo seu nome técnico.</summary>
    Task<GovernanceDomain?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona um novo domínio ao repositório.</summary>
    Task AddAsync(GovernanceDomain domain, CancellationToken ct);

    /// <summary>Atualiza um domínio existente.</summary>
    Task UpdateAsync(GovernanceDomain domain, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de GovernancePacks para o módulo Governance.
/// Define operações CRUD e consultas para pacotes de governança.
/// </summary>
public interface IGovernancePackRepository
{
    /// <summary>Lista todos os packs, opcionalmente filtrados por categoria e/ou status.</summary>
    Task<IReadOnlyList<GovernancePack>> ListAsync(
        GovernanceRuleCategory? category,
        GovernancePackStatus? status,
        CancellationToken ct);

    /// <summary>Obtém um pack pelo seu identificador.</summary>
    Task<GovernancePack?> GetByIdAsync(GovernancePackId id, CancellationToken ct);

    /// <summary>Obtém um pack pelo seu nome técnico.</summary>
    Task<GovernancePack?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona um novo pack ao repositório.</summary>
    Task AddAsync(GovernancePack pack, CancellationToken ct);

    /// <summary>Atualiza um pack existente.</summary>
    Task UpdateAsync(GovernancePack pack, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de GovernancePackVersions para o módulo Governance.
/// Define operações CRUD e consultas para versões de pacotes de governança.
/// </summary>
public interface IGovernancePackVersionRepository
{
    /// <summary>Lista todas as versões de um pack.</summary>
    Task<IReadOnlyList<GovernancePackVersion>> ListByPackIdAsync(GovernancePackId packId, CancellationToken ct);

    /// <summary>Obtém uma versão pelo seu identificador.</summary>
    Task<GovernancePackVersion?> GetByIdAsync(GovernancePackVersionId id, CancellationToken ct);

    /// <summary>Obtém a versão mais recente de um pack.</summary>
    Task<GovernancePackVersion?> GetLatestByPackIdAsync(GovernancePackId packId, CancellationToken ct);

    /// <summary>Adiciona uma nova versão ao repositório.</summary>
    Task AddAsync(GovernancePackVersion version, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de GovernanceWaivers para o módulo Governance.
/// Define operações CRUD e consultas para waivers de governança.
/// </summary>
public interface IGovernanceWaiverRepository
{
    /// <summary>Lista todos os waivers, opcionalmente filtrados por pack e/ou status.</summary>
    Task<IReadOnlyList<GovernanceWaiver>> ListAsync(
        GovernancePackId? packId,
        WaiverStatus? status,
        CancellationToken ct);

    /// <summary>Obtém um waiver pelo seu identificador.</summary>
    Task<GovernanceWaiver?> GetByIdAsync(GovernanceWaiverId id, CancellationToken ct);

    /// <summary>Adiciona um novo waiver ao repositório.</summary>
    Task AddAsync(GovernanceWaiver waiver, CancellationToken ct);

    /// <summary>Atualiza um waiver existente.</summary>
    Task UpdateAsync(GovernanceWaiver waiver, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de DelegatedAdministrations para o módulo Governance.
/// Define operações CRUD e consultas para delegações de administração.
/// </summary>
public interface IDelegatedAdministrationRepository
{
    /// <summary>Lista todas as delegações, opcionalmente filtradas por scope e/ou estado ativo.</summary>
    Task<IReadOnlyList<DelegatedAdministration>> ListAsync(
        DelegationScope? scope,
        bool? isActive,
        CancellationToken ct);

    /// <summary>Obtém uma delegação pelo seu identificador.</summary>
    Task<DelegatedAdministration?> GetByIdAsync(DelegatedAdministrationId id, CancellationToken ct);

    /// <summary>Lista delegações ativas para um utilizador específico.</summary>
    Task<IReadOnlyList<DelegatedAdministration>> ListByGranteeAsync(
        string granteeUserId,
        CancellationToken ct);

    /// <summary>Adiciona uma nova delegação ao repositório.</summary>
    Task AddAsync(DelegatedAdministration delegation, CancellationToken ct);

    /// <summary>Atualiza uma delegação existente.</summary>
    Task UpdateAsync(DelegatedAdministration delegation, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de TeamDomainLinks para o módulo Governance.
/// Define operações CRUD e consultas para associações equipa-domínio.
/// </summary>
public interface ITeamDomainLinkRepository
{
    /// <summary>Lista todas as associações de uma equipa.</summary>
    Task<IReadOnlyList<TeamDomainLink>> ListByTeamIdAsync(TeamId teamId, CancellationToken ct);

    /// <summary>Lista todas as associações de um domínio.</summary>
    Task<IReadOnlyList<TeamDomainLink>> ListByDomainIdAsync(GovernanceDomainId domainId, CancellationToken ct);

    /// <summary>Obtém uma associação pelo seu identificador.</summary>
    Task<TeamDomainLink?> GetByIdAsync(TeamDomainLinkId id, CancellationToken ct);

    /// <summary>Obtém a associação específica entre uma equipa e um domínio.</summary>
    Task<TeamDomainLink?> GetByTeamAndDomainAsync(
        TeamId teamId,
        GovernanceDomainId domainId,
        CancellationToken ct);

    /// <summary>Adiciona uma nova associação ao repositório.</summary>
    Task AddAsync(TeamDomainLink link, CancellationToken ct);

    /// <summary>Remove uma associação do repositório.</summary>
    Task RemoveAsync(TeamDomainLink link, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de GovernanceRolloutRecords para o módulo Governance.
/// Define operações CRUD e consultas para registos de rollout.
/// </summary>
public interface IGovernanceRolloutRecordRepository
{
    /// <summary>
    /// Lista rollouts com filtros opcionais.
    /// </summary>
    Task<IReadOnlyList<GovernanceRolloutRecord>> ListAsync(
        GovernancePackId? packId,
        GovernanceScopeType? scopeType,
        string? scopeValue,
        RolloutStatus? status,
        CancellationToken ct);

    /// <summary>Lista todos os rollouts de um pack.</summary>
    Task<IReadOnlyList<GovernanceRolloutRecord>> ListByPackIdAsync(
        GovernancePackId packId,
        CancellationToken ct);

    /// <summary>Lista todos os rollouts de uma versão específica.</summary>
    Task<IReadOnlyList<GovernanceRolloutRecord>> ListByVersionIdAsync(
        GovernancePackVersionId versionId,
        CancellationToken ct);

    /// <summary>Lista rollouts filtrados por status.</summary>
    Task<IReadOnlyList<GovernanceRolloutRecord>> ListByStatusAsync(
        RolloutStatus status,
        CancellationToken ct);

    /// <summary>Obtém um rollout pelo seu identificador.</summary>
    Task<GovernanceRolloutRecord?> GetByIdAsync(GovernanceRolloutRecordId id, CancellationToken ct);

    /// <summary>Adiciona um novo rollout ao repositório.</summary>
    Task AddAsync(GovernanceRolloutRecord record, CancellationToken ct);

    /// <summary>Atualiza um rollout existente.</summary>
    Task UpdateAsync(GovernanceRolloutRecord record, CancellationToken ct);
}

/// <summary>
/// DTO for monthly count aggregation.
/// </summary>
public sealed record MonthlyCount(string Period, int Count);

/// <summary>
/// Interface do repositório de Analytics para Governance Trends.
/// Define consultas agregadas para trends executivos.
/// </summary>
public interface IGovernanceAnalyticsRepository
{
    /// <summary>Retorna contagem de waivers criados por mês.</summary>
    Task<IReadOnlyList<MonthlyCount>> GetWaiverCountsByMonthAsync(int months, CancellationToken ct);

    /// <summary>Retorna contagem de packs publicados por mês.</summary>
    Task<IReadOnlyList<MonthlyCount>> GetPublishedPackCountsByMonthAsync(int months, CancellationToken ct);

    /// <summary>Retorna contagem de rollouts criados por mês.</summary>
    Task<IReadOnlyList<MonthlyCount>> GetRolloutCountsByMonthAsync(int months, CancellationToken ct);

    /// <summary>Retorna contagem total de waivers pendentes.</summary>
    Task<int> GetPendingWaiverCountAsync(CancellationToken ct);

    /// <summary>Retorna contagem total de packs publicados ativos.</summary>
    Task<int> GetPublishedPackCountAsync(CancellationToken ct);
}

/// <summary>
/// Interface do repositório de EvidencePackages para o módulo Governance.
/// </summary>
public interface IEvidencePackageRepository
{
    /// <summary>Lista pacotes de evidência, opcionalmente filtrados por escopo e status.</summary>
    Task<IReadOnlyList<EvidencePackage>> ListAsync(
        string? scope,
        EvidencePackageStatus? status,
        CancellationToken ct);

    /// <summary>Obtém pacote de evidência por identificador.</summary>
    Task<EvidencePackage?> GetByIdAsync(EvidencePackageId id, CancellationToken ct);

    /// <summary>Adiciona novo pacote de evidência.</summary>
    Task AddAsync(EvidencePackage package, CancellationToken ct);

    /// <summary>Atualiza pacote de evidência existente.</summary>
    Task UpdateAsync(EvidencePackage package, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de ComplianceGap para o módulo Governance.
/// </summary>
public interface IComplianceGapRepository
{
    /// <summary>Lista gaps de compliance, opcionalmente filtrados por escopo.</summary>
    Task<IReadOnlyList<ComplianceGap>> ListAsync(
        string? teamId,
        string? domainId,
        string? serviceId,
        CancellationToken ct);

    /// <summary>Adiciona novo gap de compliance.</summary>
    Task AddAsync(ComplianceGap gap, CancellationToken ct);
}

/// <summary>
/// Fornece métricas reais de filas de outbox acessíveis ao módulo Governance.
/// Implementada na camada de infraestrutura com acesso direto ao GovernanceDbContext.
/// </summary>
public interface IPlatformQueueMetricsProvider
{
    /// <summary>Retorna snapshots das filas de outbox com contagens reais.</summary>
    Task<IReadOnlyList<QueueSnapshot>> GetQueueSnapshotsAsync(CancellationToken ct);
}

/// <summary>Métricas de uma fila de outbox derivadas da tabela de outbox messages.</summary>
public sealed record QueueSnapshot(
    string QueueName,
    string Subsystem,
    long PendingCount,
    long FailedCount,
    DateTimeOffset? LastActivityAt);

/// <summary>
/// Fornece o catálogo de background jobs conhecidos da plataforma.
/// Implementada na camada de infraestrutura; o estado de execução em runtime
/// não está disponível a partir do ApiHost (BackgroundWorkers é processo separado).
/// </summary>
public interface IPlatformJobStatusProvider
{
    /// <summary>Retorna os jobs conhecidos com o melhor estado disponível.</summary>
    Task<IReadOnlyList<KnownJobSnapshot>> GetJobSnapshotsAsync(CancellationToken ct);
}

/// <summary>Snapshot de um background job conhecido da plataforma.</summary>
public sealed record KnownJobSnapshot(
    string JobId,
    string Name,
    string Description);

/// <summary>
/// Fornece eventos operacionais reais derivados de dados de governança persistidos.
/// Implementada na camada de infraestrutura usando rollouts e waivers como fonte de eventos.
/// </summary>
public interface IPlatformEventProvider
{
    /// <summary>Retorna eventos operacionais recentes com base em atividade de governança.</summary>
    Task<IReadOnlyList<GovernanceOperationalEvent>> GetRecentEventsAsync(int limit, CancellationToken ct);
}

/// <summary>Evento operacional derivado de atividade real de governança.</summary>
public sealed record GovernanceOperationalEvent(
    string EventId,
    DateTimeOffset Timestamp,
    string Severity,
    string Subsystem,
    string Message,
    bool Resolved);

/// <summary>
/// Interface do repositório de PolicyAsCodeDefinition para o módulo Governance.
/// Define operações CRUD e consultas para definições de política como código.
/// </summary>
public interface IPolicyAsCodeRepository
{
    /// <summary>Lista todas as definições de política, opcionalmente filtradas por status ou modo.</summary>
    Task<IReadOnlyList<PolicyAsCodeDefinition>> ListAsync(
        PolicyDefinitionStatus? status,
        PolicyEnforcementMode? enforcementMode,
        CancellationToken ct);

    /// <summary>Obtém uma definição de política pelo seu identificador.</summary>
    Task<PolicyAsCodeDefinition?> GetByIdAsync(PolicyAsCodeDefinitionId id, CancellationToken ct);

    /// <summary>Obtém uma definição de política pelo nome técnico.</summary>
    Task<PolicyAsCodeDefinition?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona uma nova definição de política.</summary>
    Task AddAsync(PolicyAsCodeDefinition definition, CancellationToken ct);

    /// <summary>Atualiza uma definição de política existente.</summary>
    Task UpdateAsync(PolicyAsCodeDefinition definition, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de DashboardRevision (V3.1 — Dashboard Intelligence Foundation).
/// Persiste snapshots imutáveis de dashboards para histórico, diff e revert.
/// </summary>
public interface IDashboardRevisionRepository
{
    /// <summary>Lista revisões de um dashboard, ordenadas da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<DashboardRevision>> ListByDashboardIdAsync(
        CustomDashboardId dashboardId,
        int maxResults,
        CancellationToken ct);

    /// <summary>Obtém uma revisão específica pelo número de revisão.</summary>
    Task<DashboardRevision?> GetByRevisionNumberAsync(
        CustomDashboardId dashboardId,
        int revisionNumber,
        CancellationToken ct);

    /// <summary>Obtém o número total de revisões de um dashboard.</summary>
    Task<int> CountByDashboardIdAsync(CustomDashboardId dashboardId, CancellationToken ct);

    /// <summary>Adiciona uma nova revisão ao repositório.</summary>
    Task AddAsync(DashboardRevision revision, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de Custom Dashboards para o módulo Governance.
/// Define operações CRUD e consultas para dashboards customizados por persona.
/// </summary>
public interface ICustomDashboardRepository
{
    /// <summary>Lista dashboards do tenant, opcionalmente filtrados por persona.</summary>
    Task<IReadOnlyList<CustomDashboard>> ListAsync(string? persona, CancellationToken ct);

    /// <summary>Obtém um dashboard pelo seu identificador.</summary>
    Task<CustomDashboard?> GetByIdAsync(CustomDashboardId id, CancellationToken ct);

    /// <summary>Conta o número total de dashboards do tenant, opcionalmente por persona.</summary>
    Task<int> CountAsync(string? persona, CancellationToken ct);

    /// <summary>Adiciona um novo dashboard ao repositório.</summary>
    Task AddAsync(CustomDashboard dashboard, CancellationToken ct);

    /// <summary>Atualiza um dashboard existente.</summary>
    Task UpdateAsync(CustomDashboard dashboard, CancellationToken ct);

    /// <summary>Remove um dashboard do repositório.</summary>
    Task DeleteAsync(CustomDashboard dashboard, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de Technical Debt Items para o módulo Governance.
/// Define operações CRUD e consultas para itens de dívida técnica.
/// </summary>
public interface ITechnicalDebtRepository
{
    /// <summary>Lista itens de dívida técnica, opcionalmente filtrados por serviço ou equipa.</summary>
    Task<IReadOnlyList<TechnicalDebtItem>> ListAsync(
        string? serviceName,
        string? debtType,
        int topN,
        CancellationToken ct);

    /// <summary>Obtém um item de dívida técnica pelo seu identificador.</summary>
    Task<TechnicalDebtItem?> GetByIdAsync(TechnicalDebtItemId id, CancellationToken ct);

    /// <summary>Adiciona um novo item de dívida técnica ao repositório.</summary>
    Task AddAsync(TechnicalDebtItem item, CancellationToken ct);

    /// <summary>Atualiza um item de dívida técnica existente.</summary>
    Task UpdateAsync(TechnicalDebtItem item, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de ServiceMaturityAssessment para o módulo Governance.
/// Define operações CRUD e consultas para avaliações de maturidade de serviços.
/// </summary>
public interface IServiceMaturityAssessmentRepository
{
    /// <summary>Obtém uma avaliação pelo identificador.</summary>
    Task<ServiceMaturityAssessment?> GetByIdAsync(ServiceMaturityAssessmentId id, CancellationToken ct);

    /// <summary>Obtém a avaliação mais recente de um serviço.</summary>
    Task<ServiceMaturityAssessment?> GetByServiceIdAsync(Guid serviceId, CancellationToken ct);

    /// <summary>Lista avaliações, opcionalmente filtradas por nível de maturidade.</summary>
    Task<IReadOnlyList<ServiceMaturityAssessment>> ListAsync(
        ServiceMaturityLevel? level,
        CancellationToken ct);

    /// <summary>Adiciona uma nova avaliação.</summary>
    Task AddAsync(ServiceMaturityAssessment assessment, CancellationToken ct);

    /// <summary>Atualiza uma avaliação existente.</summary>
    Task UpdateAsync(ServiceMaturityAssessment assessment, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de TeamHealthSnapshot para o módulo Governance.
/// Define operações CRUD e consultas para snapshots de saúde de equipas.
/// </summary>
public interface ITeamHealthSnapshotRepository
{
    /// <summary>Obtém um snapshot pelo identificador.</summary>
    Task<TeamHealthSnapshot?> GetByIdAsync(TeamHealthSnapshotId id, CancellationToken ct);

    /// <summary>Obtém o snapshot mais recente de uma equipa.</summary>
    Task<TeamHealthSnapshot?> GetByTeamIdAsync(Guid teamId, CancellationToken ct);

    /// <summary>Lista snapshots, opcionalmente filtrados por score mínimo.</summary>
    Task<IReadOnlyList<TeamHealthSnapshot>> ListAsync(int? minOverallScore, CancellationToken ct);

    /// <summary>Adiciona um novo snapshot.</summary>
    Task AddAsync(TeamHealthSnapshot snapshot, CancellationToken ct);

    /// <summary>Atualiza um snapshot existente.</summary>
    Task UpdateAsync(TeamHealthSnapshot snapshot, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de ChangeCostImpact para o módulo Governance.
/// Define operações CRUD e consultas para impacto de custo por mudança (FinOps).
/// </summary>
public interface IChangeCostImpactRepository
{
    /// <summary>Obtém um registo de impacto de custo pelo identificador.</summary>
    Task<ChangeCostImpact?> GetByIdAsync(ChangeCostImpactId id, CancellationToken ct);

    /// <summary>Obtém o registo de impacto de custo associado a uma release.</summary>
    Task<ChangeCostImpact?> GetByReleaseIdAsync(Guid releaseId, CancellationToken ct);

    /// <summary>Lista os N impactos de custo mais significativos (por delta absoluto).</summary>
    Task<IReadOnlyList<ChangeCostImpact>> ListCostliestAsync(int top, CancellationToken ct);

    /// <summary>Lista impactos de custo de um serviço específico.</summary>
    Task<IReadOnlyList<ChangeCostImpact>> ListByServiceAsync(string serviceName, CancellationToken ct);

    /// <summary>Adiciona um novo registo de impacto de custo.</summary>
    Task AddAsync(ChangeCostImpact impact, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de ExecutiveBriefing para o módulo Governance.
/// Define operações CRUD e consultas para briefings executivos gerados por IA.
/// </summary>
public interface IExecutiveBriefingRepository
{
    /// <summary>Obtém um briefing pelo seu identificador.</summary>
    Task<ExecutiveBriefing?> GetByIdAsync(ExecutiveBriefingId id, CancellationToken ct);

    /// <summary>Lista briefings, opcionalmente filtrados por frequência e/ou status.</summary>
    Task<IReadOnlyList<ExecutiveBriefing>> ListAsync(
        BriefingFrequency? frequency,
        BriefingStatus? status,
        CancellationToken ct);

    /// <summary>Adiciona um novo briefing ao repositório.</summary>
    Task AddAsync(ExecutiveBriefing briefing, CancellationToken ct);

    /// <summary>Atualiza um briefing existente.</summary>
    Task UpdateAsync(ExecutiveBriefing briefing, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de CostAttribution para o módulo Governance.
/// Define operações CRUD e consultas para atribuição de custo operacional por dimensão.
/// </summary>
public interface ICostAttributionRepository
{
    /// <summary>Obtém uma atribuição de custo pelo seu identificador.</summary>
    Task<CostAttribution?> GetByIdAsync(CostAttributionId id, CancellationToken ct);

    /// <summary>Lista atribuições de custo por dimensão, opcionalmente filtradas por período.</summary>
    Task<IReadOnlyList<CostAttribution>> ListByDimensionAsync(
        CostAttributionDimension dimension,
        DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd,
        CancellationToken ct);

    /// <summary>Obtém os N registos com maior custo total para uma dimensão, opcionalmente até uma data limite.</summary>
    Task<IReadOnlyList<CostAttribution>> GetTopByDimensionAsync(
        CostAttributionDimension dimension,
        int top,
        DateTimeOffset? periodEnd,
        CancellationToken ct);

    /// <summary>Adiciona uma nova atribuição de custo ao repositório.</summary>
    Task AddAsync(CostAttribution attribution, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de LicenseComplianceReport para o módulo Governance.
/// Define operações CRUD e consultas para relatórios de compliance de licenças de dependências.
/// </summary>
public interface ILicenseComplianceReportRepository
{
    /// <summary>Obtém um relatório de compliance de licenças pelo seu identificador.</summary>
    Task<LicenseComplianceReport?> GetByIdAsync(LicenseComplianceReportId id, CancellationToken ct);

    /// <summary>Lista relatórios de compliance por escopo, opcionalmente filtrados por scope key.</summary>
    Task<IReadOnlyList<LicenseComplianceReport>> ListByScopeAsync(
        LicenseComplianceScope scope,
        string? scopeKey,
        CancellationToken ct);

    /// <summary>Obtém o relatório mais recente para um scope key específico.</summary>
    Task<LicenseComplianceReport?> GetLatestByScopeKeyAsync(
        string scopeKey, CancellationToken ct);

    /// <summary>Adiciona um novo relatório de compliance ao repositório.</summary>
    Task AddAsync(LicenseComplianceReport report, CancellationToken ct);
}

/// <summary>
/// Interface do repositório de FinOpsBudgetApproval para o módulo Governance.
/// Define operações para gerir pedidos de aprovação de override de orçamento FinOps.
/// </summary>
public interface IFinOpsBudgetApprovalRepository
{
    /// <summary>Obtém um pedido de aprovação pelo seu identificador.</summary>
    Task<FinOpsBudgetApproval?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Lista pedidos de aprovação com filtros opcionais por status e serviço.</summary>
    Task<IReadOnlyList<FinOpsBudgetApproval>> ListAsync(
        FinOpsBudgetApprovalStatus? status,
        string? serviceName,
        CancellationToken ct);

    /// <summary>Adiciona um novo pedido de aprovação ao repositório.</summary>
    Task AddAsync(FinOpsBudgetApproval approval, CancellationToken ct);

    /// <summary>Atualiza um pedido de aprovação existente.</summary>
    void Update(FinOpsBudgetApproval approval);
}
