namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de maturidade SRE por equipa.
/// Orquestra dados de múltiplos domínios para avaliar 6 práticas SRE.
/// Por omissão satisfeita por <c>NullSreMaturityReader</c> (honest-null).
/// Wave AN.3 — GetSreMaturityIndexReport.
/// </summary>
public interface ISreMaturityReader
{
    Task<IReadOnlyList<TeamSreDataEntry>> ListByTenantAsync(
        string tenantId,
        int chaosLookbackMonths,
        CancellationToken ct);

    /// <summary>Entrada de dados SRE por equipa para cálculo de maturidade.</summary>
    public sealed record TeamSreDataEntry(
        string TeamId,
        string TeamName,
        int TotalServices,
        int ServicesWithSlo,
        int ServicesWithErrorBudgetTracking,
        int ServicesWithChaosExperiment,
        bool HasAutoApprovalOrPipelineAutomation,
        int TotalSevereOrCriticalIncidents,
        int IncidentsWithPostIncidentReview,
        int TotalIncidentsWithService,
        int IncidentsWithActiveRunbook,
        decimal? PreviousPeriodSreScore);
}
