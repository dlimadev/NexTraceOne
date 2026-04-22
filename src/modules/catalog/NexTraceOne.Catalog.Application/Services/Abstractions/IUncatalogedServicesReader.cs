namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de descoberta de serviços não catalogados.
/// Cruza dados de telemetria com o catálogo registado para detectar "shadow services".
/// Por omissão satisfeita por <c>NullUncatalogedServicesReader</c> (honest-null).
/// Wave AM.1 — GetUncatalogedServicesReport.
/// </summary>
public interface IUncatalogedServicesReader
{
    /// <summary>
    /// Lista serviços observados em telemetria no período que não têm registo no catálogo.
    /// </summary>
    Task<UncatalogedServicesSummary> GetSummaryAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);

    /// <summary>Sumário de serviços não catalogados detectados por telemetria.</summary>
    public sealed record UncatalogedServicesSummary(
        int CatalogedServiceCount,
        IReadOnlyList<UncatalogedServiceEntry> UncatalogedServices);

    /// <summary>Entrada de um serviço detectado em telemetria mas não registado no catálogo.</summary>
    public sealed record UncatalogedServiceEntry(
        string ServiceName,
        DateTimeOffset FirstSeen,
        DateTimeOffset LastSeen,
        int DailyCallCount,
        IReadOnlyList<string> ObservedEnvironments,
        string? PossibleOwner);
}
