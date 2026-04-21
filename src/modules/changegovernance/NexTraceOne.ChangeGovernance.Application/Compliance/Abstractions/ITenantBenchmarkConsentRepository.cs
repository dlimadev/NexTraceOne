using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>Repositório de consentimentos de benchmark cross-tenant.</summary>
public interface ITenantBenchmarkConsentRepository
{
    /// <summary>Obtém o consentimento do tenant, se existir.</summary>
    Task<TenantBenchmarkConsent?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Adiciona um novo registo de consentimento.</summary>
    void Add(TenantBenchmarkConsent consent);

    /// <summary>Marca o consentimento como modificado.</summary>
    void Update(TenantBenchmarkConsent consent);
}
