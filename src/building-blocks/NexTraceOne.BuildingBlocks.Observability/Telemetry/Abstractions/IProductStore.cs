using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;

// IMPLEMENTATION STATUS: Implemented — EF Core repositories in OperationalIntelligence.Infrastructure.TelemetryStore.
// ObservedTopologyRepository, AnomalySnapshotRepository, TelemetryReferenceRepository,
// ReleaseCorrelationRepository, InvestigationContextRepository provide PostgreSQL-backed persistence.
// Registered in DI via AddTelemetryStoreInfrastructure().

/// <summary>
/// Porta de escrita para topologia observada no Product Store.
/// Registra arestas de comunicação entre serviços descobertas via telemetria.
/// </summary>
public interface IObservedTopologyWriter
{
    /// <summary>
    /// Registra ou atualiza uma aresta de topologia observada.
    /// Se a aresta já existe (source + target + environment), atualiza LastSeenAt e contadores.
    /// Se não existe, cria nova entrada com FirstSeenAt = agora.
    /// </summary>
    Task UpsertAsync(ObservedTopologyEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Persiste um lote de entradas de topologia (bulk upsert).</summary>
    Task UpsertBatchAsync(IReadOnlyList<ObservedTopologyEntry> entries, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura para topologia observada no Product Store.
/// Alimenta: grafo de dependências, blast radius, shadow dependency detection.
/// </summary>
public interface IObservedTopologyReader
{
    /// <summary>Busca todas as arestas de topologia para um serviço (incoming e outgoing).</summary>
    Task<IReadOnlyList<ObservedTopologyEntry>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Busca toda a topologia observada para um ambiente.</summary>
    Task<IReadOnlyList<ObservedTopologyEntry>> GetByEnvironmentAsync(
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Busca shadow dependencies (dependências não declaradas no catálogo).</summary>
    Task<IReadOnlyList<ObservedTopologyEntry>> GetShadowDependenciesAsync(
        string environment,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de escrita para snapshots de anomalia no Product Store.
/// </summary>
public interface IAnomalySnapshotWriter
{
    /// <summary>Persiste um snapshot de anomalia detectada.</summary>
    Task WriteAsync(AnomalySnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Marca uma anomalia como resolvida.</summary>
    Task ResolveAsync(Guid anomalyId, DateTimeOffset resolvedAt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura para snapshots de anomalia no Product Store.
/// </summary>
public interface IAnomalySnapshotReader
{
    /// <summary>Busca anomalias ativas (não resolvidas) para um serviço.</summary>
    Task<IReadOnlyList<AnomalySnapshot>> GetActiveByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Busca anomalias em um intervalo de tempo.</summary>
    Task<IReadOnlyList<AnomalySnapshot>> GetByTimeRangeAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de escrita para referências de telemetria (ponteiros para dados crus no Telemetry Store).
/// </summary>
public interface ITelemetryReferenceWriter
{
    /// <summary>Registra uma referência para dados crus no Telemetry Store.</summary>
    Task WriteAsync(TelemetryReference reference, CancellationToken cancellationToken = default);

    /// <summary>Registra um lote de referências (bulk insert).</summary>
    Task WriteBatchAsync(IReadOnlyList<TelemetryReference> references, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura para referências de telemetria.
/// Permite navegar do Product Store (anomalias, correlações, investigações)
/// para os dados crus armazenados no provider de observabilidade (Elastic, ClickHouse).
/// </summary>
public interface ITelemetryReferenceReader
{
    /// <summary>Busca referências por correlation ID (anomalia, release, investigação).</summary>
    Task<IReadOnlyList<TelemetryReference>> GetByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>Busca referências por serviço e intervalo de tempo.</summary>
    Task<IReadOnlyList<TelemetryReference>> GetByServiceAsync(
        Guid serviceId,
        TelemetrySignalType signalType,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de escrita para correlações release/runtime no Product Store.
/// </summary>
public interface IReleaseCorrelationWriter
{
    /// <summary>Registra uma correlação entre deploy e métricas de runtime.</summary>
    Task WriteAsync(ReleaseRuntimeCorrelation correlation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura para correlações release/runtime.
/// Permite investigar o impacto de uma release nos indicadores operacionais.
/// </summary>
public interface IReleaseCorrelationReader
{
    /// <summary>Busca correlações de uma release específica.</summary>
    Task<IReadOnlyList<ReleaseRuntimeCorrelation>> GetByReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);

    /// <summary>Busca correlações de um serviço em um intervalo de tempo.</summary>
    Task<IReadOnlyList<ReleaseRuntimeCorrelation>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de escrita para contextos investigativos no Product Store.
/// </summary>
public interface IInvestigationContextWriter
{
    /// <summary>Cria ou atualiza um contexto investigativo.</summary>
    Task UpsertAsync(InvestigationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Porta de leitura para contextos investigativos.
/// Alimenta: AI Orchestration, interface de investigação, audit trail.
/// </summary>
public interface IInvestigationContextReader
{
    /// <summary>Busca um contexto investigativo por ID.</summary>
    Task<InvestigationContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Busca contextos investigativos abertos para um serviço.</summary>
    Task<IReadOnlyList<InvestigationContext>> GetOpenByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Busca contextos investigativos em um intervalo de tempo.</summary>
    Task<IReadOnlyList<InvestigationContext>> GetByTimeRangeAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}
