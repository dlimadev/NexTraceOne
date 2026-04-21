using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>Repositório de perfis de risco de serviços — Risk Center.</summary>
public interface IServiceRiskProfileRepository
{
    /// <summary>Obtém o perfil de risco mais recente de um serviço.</summary>
    Task<ServiceRiskProfile?> GetLatestByServiceAsync(
        string tenantId,
        Guid serviceAssetId,
        CancellationToken ct = default);

    /// <summary>
    /// Lista perfis de risco de um tenant ordenados por score descendente (mais crítico primeiro).
    /// Usado pelo Risk Center Report para Exec/Platform Admin.
    /// </summary>
    Task<IReadOnlyList<ServiceRiskProfile>> ListByTenantRankedAsync(
        string tenantId,
        int maxResults = 50,
        CancellationToken ct = default);

    /// <summary>Adiciona um novo perfil de risco.</summary>
    void Add(ServiceRiskProfile profile);
}
