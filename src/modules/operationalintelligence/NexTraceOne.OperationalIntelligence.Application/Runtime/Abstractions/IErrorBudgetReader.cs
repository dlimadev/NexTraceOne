namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de SLO e compliance para cálculo de error budget.
/// Cruza SloObservation com ServiceAsset para calcular budget disponível e consumido.
/// Por omissão satisfeita por <c>NullErrorBudgetReader</c> (honest-null).
/// Wave AN.1 — GetErrorBudgetReport.
/// </summary>
public interface IErrorBudgetReader
{
    Task<IReadOnlyList<ServiceSloEntry>> ListByTenantAsync(
        string tenantId,
        int periodDays,
        CancellationToken ct);

    /// <summary>Entrada de SLO por serviço para cálculo de error budget.</summary>
    public sealed record ServiceSloEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        decimal SloTargetPct,
        decimal ActualCompliancePct,
        DateTimeOffset PeriodStartDate,
        DateTimeOffset PeriodEndDate,
        IReadOnlyList<DailyBudgetSample> DailySamples);

    /// <summary>Amostra diária de compliance para construção de timeline.</summary>
    public sealed record DailyBudgetSample(DateTimeOffset Date, decimal CompliancePct);
}
