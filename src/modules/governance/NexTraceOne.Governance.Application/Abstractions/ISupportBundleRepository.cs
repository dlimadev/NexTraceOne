using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de bundles de suporte.
/// </summary>
public interface ISupportBundleRepository
{
    /// <summary>Lista todos os bundles de suporte, mais recentes primeiro.</summary>
    Task<IReadOnlyList<SupportBundle>> ListAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Obtém um bundle pelo seu identificador.</summary>
    Task<SupportBundle?> GetByIdAsync(SupportBundleId id, CancellationToken ct);

    /// <summary>Adiciona um novo bundle ao repositório.</summary>
    Task AddAsync(SupportBundle bundle, CancellationToken ct);

    /// <summary>Atualiza um bundle existente.</summary>
    void Update(SupportBundle bundle);
}
