namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de previsão de deprecação de contratos.
/// Analisa contratos activos para prever candidatos a deprecação.
/// Por omissão satisfeita por <c>NullContractDeprecationForecastReader</c> (honest-null).
/// Wave AV.3 — GetContractDeprecationForecast.
/// </summary>
public interface IContractDeprecationForecastReader
{
    Task<IReadOnlyList<ActiveContractForecastEntry>> ListActiveContractsByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de contrato activo para análise preditiva de deprecação.</summary>
    public sealed record ActiveContractForecastEntry(
        Guid ContractId,
        string ContractName,
        string ContractVersion,
        string Protocol,
        string? OwnerTeamId,
        DateTimeOffset CreatedAt,
        bool HasSuccessorVersion,
        int CurrentConsumerCount,
        int ConsumerCountPrevMonth,
        int ConsumerCountTwoMonthsAgo,
        bool OwnerSignalledDeprecation,
        IReadOnlyList<PlannedDeprecationCalendarEntry> PlannedDeprecations);

    /// <summary>Entrada de calendário de deprecações planeadas.</summary>
    public sealed record PlannedDeprecationCalendarEntry(
        Guid ContractId,
        string ContractName,
        DateTimeOffset PlannedDeprecationDate,
        DateTimeOffset? PlannedSunsetDate,
        int ActiveConsumerCount);
}
