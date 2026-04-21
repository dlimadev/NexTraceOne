using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação honest-null de <see cref="ITeamOperationalMetricsReader"/>.
///
/// Devolve lista vazia, sinalizando que não há bridge configurado para métricas
/// operacionais por equipa. Relatórios que dependem desta abstracção retornarão
/// dados vazios enquanto não houver uma implementação real registada.
///
/// Wave R.3 — Team Operational Health Report.
/// </summary>
internal sealed class NullTeamOperationalMetricsReader : ITeamOperationalMetricsReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TeamOperationalMetrics>> ListTeamMetricsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TeamOperationalMetrics>>([]);
}
