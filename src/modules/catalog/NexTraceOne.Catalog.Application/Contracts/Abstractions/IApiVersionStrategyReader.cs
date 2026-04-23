namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de estratégia de versionamento de APIs.
/// Agrega ContractDefinition por protocolo (REST, SOAP, AsyncAPI).
/// Por omissão satisfeita por <c>NullApiVersionStrategyReader</c> (honest-null).
/// Wave AV.2 — GetApiVersionStrategyReport.
/// </summary>
public interface IApiVersionStrategyReader
{
    Task<IReadOnlyList<ServiceVersionEntry>> ListServiceVersionDataByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);

    /// <summary>Entrada de dados de versionamento por serviço.</summary>
    public sealed record ServiceVersionEntry(
        string ServiceId,
        string ServiceName,
        string? OwnerTeamId,
        string Protocol,
        int ActiveVersionCount,
        bool SemverAdherence,
        int BreakingChangesLast90d,
        double AvgVersionLifetimeDays,
        string? OldestActiveVersion,
        IReadOnlyList<string> ActiveVersionTags);
}
