using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IIncidentLearningReader"/>.
///
/// Devolve lista vazia, sinalizando que não há runbooks aprovados registados.
/// A ligação com o módulo Knowledge é feita por uma bridge dedicada na composition root
/// e, quando registada, substitui este default.
///
/// Wave T.1 — Post-Incident Learning Report.
/// </summary>
internal sealed class NullIncidentLearningReader : IIncidentLearningReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListServicesWithApprovedRunbookAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
