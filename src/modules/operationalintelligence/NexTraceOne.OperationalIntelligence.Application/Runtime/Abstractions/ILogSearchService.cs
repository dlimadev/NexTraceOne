using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de pesquisa de logs estruturados.
/// Abstracção sobre backend de telemetria (Elasticsearch ou ClickHouse).
/// Permite ao utilizador escolher qual database usar na instalação.
/// SaaS-07: Log Search UI.
/// </summary>
public interface ITelemetrySearchService
{
    /// <summary>
    /// Pesquisa logs com os filtros especificados.
    /// Devolve tuplo (entradas, total).
    /// Suporta tanto Elasticsearch quanto ClickHouse como backend.
    /// </summary>
    Task<(IReadOnlyList<SearchLogs.LogEntry> Entries, long Total)> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Indexa um log no backend de telemetria.
    /// Usado para ingestão de logs em tempo real.
    /// </summary>
    Task IndexLogAsync(SearchLogs.LogEntry log, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se o backend de telemetria está saudável e acessível.
    /// Retorna true se o serviço estiver operacional.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtém estatísticas do backend (número de índices, tamanho total, etc.).
    /// Útil para monitoring e capacity planning.
    /// </summary>
    Task<TelemetryBackendStats> GetStatsAsync(CancellationToken cancellationToken);
}

/// <summary>Estatísticas do backend de telemetria.</summary>
public sealed record TelemetryBackendStats(
    string BackendType,
    long TotalDocuments,
    long TotalSizeBytes,
    int ActiveIndices,
    DateTimeOffset LastIndexTime);

/// <summary>Parâmetros de pesquisa de logs.</summary>
public sealed record LogSearchRequest(
    Guid TenantId,
    string? ServiceName,
    string? Severity,
    string? Environment,
    string? SearchText,
    DateTimeOffset From,
    DateTimeOffset To,
    int Page,
    int PageSize);
