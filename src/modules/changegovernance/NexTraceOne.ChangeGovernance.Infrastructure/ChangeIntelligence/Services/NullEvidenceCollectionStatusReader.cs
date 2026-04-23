using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IEvidenceCollectionStatusReader"/>.
/// Retorna listas vazias e data de auditoria nula até a infraestrutura real ser ligada.
/// Wave BB.2 — GetEvidenceCollectionStatusReport.
/// </summary>
internal sealed class NullEvidenceCollectionStatusReader : IEvidenceCollectionStatusReader
{
    public Task<IReadOnlyList<IEvidenceCollectionStatusReader.EvidenceControlEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IEvidenceCollectionStatusReader.EvidenceControlEntry>>([]);

    public Task<DateTimeOffset?> GetNextAuditDateAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<DateTimeOffset?>(null);
}
