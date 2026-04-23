using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IEvidencePackIntegrityReader"/>.
/// Retorna listas vazias até a infraestrutura real ser ligada.
/// Wave BC.2 — GetEvidencePackIntegrityReport.
/// </summary>
internal sealed class NullEvidencePackIntegrityReader : IEvidencePackIntegrityReader
{
    public Task<IReadOnlyList<IEvidencePackIntegrityReader.EvidencePackEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IEvidencePackIntegrityReader.EvidencePackEntry>>([]);
}
