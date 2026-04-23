namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de incidentes para scorecard de impacto.
/// Agrega IncidentRecord, SloObservation, RuntimeSnapshot e ServiceAsset.
/// Por omissão satisfeita por <c>NullIncidentImpactScorecardReader</c> (honest-null).
/// Wave AN.2 — GetIncidentImpactScorecardReport.
/// </summary>
public interface IIncidentImpactScorecardReader
{
    Task<IReadOnlyList<IncidentEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);

    /// <summary>Entrada de incidente com dados para cálculo de impacto.</summary>
    public sealed record IncidentEntry(
        string IncidentId,
        string ServiceId,
        string ServiceName,
        string TeamId,
        string TeamName,
        int DurationMinutes,
        int BlastRadiusDependents,
        decimal SloImpactPct,
        bool CustomerFacing,
        DateTimeOffset OccurredAt);
}
