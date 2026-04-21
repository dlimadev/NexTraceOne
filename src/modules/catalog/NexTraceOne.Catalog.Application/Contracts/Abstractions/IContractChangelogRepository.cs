using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para entradas de changelog de evolução contratual.
/// </summary>
public interface IContractChangelogRepository
{
    /// <summary>Obtém uma entrada de changelog pelo seu identificador.</summary>
    Task<ContractChangelog?> GetByIdAsync(ContractChangelogId id, CancellationToken cancellationToken);

    /// <summary>Lista entradas de changelog por ativo de API.</summary>
    Task<IReadOnlyList<ContractChangelog>> ListByApiAssetAsync(string apiAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista entradas de changelog de um tenant num intervalo temporal.
    /// Usado por GetApiSchemaStabilityReport para agregar frequência de mudanças por contrato.
    /// Wave R.2 — API Schema Stability Report.
    /// </summary>
    Task<IReadOnlyList<ContractChangelog>> ListByTenantInPeriodAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Lista entradas de changelog pendentes de aprovação formal.</summary>
    Task<IReadOnlyList<ContractChangelog>> ListPendingApprovalAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova entrada de changelog.</summary>
    Task AddAsync(ContractChangelog changelog, CancellationToken cancellationToken);
}
