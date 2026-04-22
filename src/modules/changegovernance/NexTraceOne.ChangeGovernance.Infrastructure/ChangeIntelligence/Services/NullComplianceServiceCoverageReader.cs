using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IComplianceServiceCoverageReader"/>.
///
/// Devolve lista vazia, sinalizando que não há avaliações de compliance por serviço registadas.
/// Uma bridge real — ligando avaliações per-serviço ao módulo de compliance — pode substituir
/// este default na composition root quando disponível.
///
/// Wave U.1 — Compliance Coverage Matrix Report.
/// </summary>
internal sealed class NullComplianceServiceCoverageReader : IComplianceServiceCoverageReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<IComplianceServiceCoverageReader.ServiceStandardCoverage>> ListCoverageAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<IComplianceServiceCoverageReader.ServiceStandardCoverage>>([]);
}
