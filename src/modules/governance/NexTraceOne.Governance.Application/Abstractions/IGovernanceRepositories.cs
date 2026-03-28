using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

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
