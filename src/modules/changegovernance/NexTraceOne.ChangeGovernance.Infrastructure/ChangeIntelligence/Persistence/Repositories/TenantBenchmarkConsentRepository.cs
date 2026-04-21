using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para consentimentos de benchmark cross-tenant.
/// </summary>
internal sealed class TenantBenchmarkConsentRepository(ChangeIntelligenceDbContext context) : ITenantBenchmarkConsentRepository
{
    /// <summary>Obtém o consentimento de benchmark de um tenant específico.</summary>
    public async Task<TenantBenchmarkConsent?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
        => await context.BenchmarkConsents
            .SingleOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted, ct);

    /// <summary>Adiciona um novo registo de consentimento.</summary>
    public void Add(TenantBenchmarkConsent consent)
        => context.BenchmarkConsents.Add(consent);

    /// <summary>Marca o consentimento como modificado no change tracker.</summary>
    public void Update(TenantBenchmarkConsent consent)
        => context.BenchmarkConsents.Update(consent);
}
