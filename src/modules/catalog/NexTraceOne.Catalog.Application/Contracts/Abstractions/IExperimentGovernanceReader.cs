namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de governança de experimentos A/B.
/// Filtra <c>FeatureFlagRecord</c> do tipo Experiment e agrega métricas de ciclo de vida.
/// Por omissão satisfeita por <c>NullExperimentGovernanceReader</c> (honest-null).
/// Wave AS.3 — GetExperimentGovernanceReport.
/// </summary>
public interface IExperimentGovernanceReader
{
    Task<IReadOnlyList<ExperimentEntry>> ListExperimentsByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de experimento para análise de governança.</summary>
    public sealed record ExperimentEntry(
        string ServiceId,
        string ServiceName,
        string FlagKey,
        int ExperimentDurationDays,
        bool HasSuccessCriteria,
        bool IsActiveInProd,
        IReadOnlyList<string> ActiveEnvironments,
        DateTimeOffset? LastToggledAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ConcludedAt);
}
