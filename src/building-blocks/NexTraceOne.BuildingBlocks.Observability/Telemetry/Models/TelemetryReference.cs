namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Referência do Product Store para dados crus no Telemetry Store.
/// Serve como ponteiro/link entre os dados agregados (PostgreSQL) e
/// os traces/logs crus armazenados em backends especializados (Tempo, Loki, etc.).
///
/// Tabela-alvo: telemetry_references (Product Store — PostgreSQL).
///
/// Princípio: o Product Store nunca armazena traces/logs crus em volume,
/// mas mantém referências indexáveis para navegação investigativa.
/// Um investigador pode partir de uma anomalia ou correlação no Product Store
/// e seguir a referência para acessar os dados crus no Telemetry Store.
/// </summary>
public sealed record TelemetryReference
{
    /// <summary>Identificador único da referência.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Tipo de sinal referenciado (Traces, Logs, Metrics).</summary>
    public required TelemetrySignalType SignalType { get; init; }

    /// <summary>
    /// Identificador externo no Telemetry Store.
    /// Para traces: trace_id. Para logs: log stream ID ou query filter.
    /// </summary>
    public required string ExternalId { get; init; }

    /// <summary>
    /// Backend onde o dado cru reside ("tempo", "loki", "clickhouse", etc.).
    /// Permite que a UI saiba qual API chamar para buscar o dado original.
    /// </summary>
    public required string BackendType { get; init; }

    /// <summary>
    /// URI ou query para acesso direto ao dado no Telemetry Store.
    /// Ex: "http://tempo:3200/api/traces/{traceId}" ou query Loki.
    /// </summary>
    public string? AccessUri { get; init; }

    /// <summary>Identificador do serviço relacionado.</summary>
    public Guid? ServiceId { get; init; }

    /// <summary>Nome do serviço relacionado (para consulta sem join).</summary>
    public string? ServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public string? Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// ID de correlação que liga esta referência a uma anomalia, investigação ou release.
    /// Permite navegação bidirecional entre contextos investigativos e dados crus.
    /// </summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Timestamp do dado original no Telemetry Store.</summary>
    public required DateTimeOffset OriginalTimestamp { get; init; }

    /// <summary>Timestamp de criação desta referência.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
