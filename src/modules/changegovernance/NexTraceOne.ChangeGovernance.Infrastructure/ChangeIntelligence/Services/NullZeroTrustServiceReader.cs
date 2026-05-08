using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação null (honest-null) de <see cref="IZeroTrustServiceReader"/>.
/// Retorna lista vazia — nenhum dado de segurança de serviço registado.
/// Wave AD.1 — GetZeroTrustPostureReport.
/// </summary>
internal sealed class NullZeroTrustServiceReader : IZeroTrustServiceReader
{
    public Task<IReadOnlyList<ServiceSecurityEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ServiceSecurityEntry>>([]);
}
