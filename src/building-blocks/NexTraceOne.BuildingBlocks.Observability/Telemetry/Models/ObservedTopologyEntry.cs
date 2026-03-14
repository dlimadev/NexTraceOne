namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Entrada de topologia observada a partir de dados de telemetria.
/// Representa uma aresta no grafo de dependências descoberta automaticamente
/// pelo pipeline de telemetria (span attributes, HTTP client calls, DB connections).
///
/// Tabela-alvo: observed_topology (Product Store — PostgreSQL).
/// A topologia observada complementa a topologia declarada do catálogo,
/// permitindo detectar dependências não documentadas (shadow dependencies).
///
/// Alimenta: Módulo 3 (Graph/Topology), Módulo 10 (Runtime Intelligence),
/// blast radius calculation, e drift detection.
/// </summary>
public sealed record ObservedTopologyEntry
{
    /// <summary>Identificador único da aresta de topologia.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Identificador do serviço de origem (caller).</summary>
    public required Guid SourceServiceId { get; init; }

    /// <summary>Nome do serviço de origem.</summary>
    public required string SourceServiceName { get; init; }

    /// <summary>Identificador do serviço de destino (callee).</summary>
    public required Guid TargetServiceId { get; init; }

    /// <summary>Nome do serviço de destino.</summary>
    public required string TargetServiceName { get; init; }

    /// <summary>
    /// Tipo de comunicação observada (http, grpc, database, messaging, etc.).
    /// Inferido a partir de span attributes como db.system, rpc.system, http.method.
    /// </summary>
    public required string CommunicationType { get; init; }

    /// <summary>Ambiente em que a comunicação foi observada.</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Nível de confiança da observação (0.0 a 1.0).
    /// Baseado em: volume de chamadas, consistência temporal, presença de erros.
    /// Dependências com alta confiança podem ser promovidas para o catálogo declarado.
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>Primeira vez que esta aresta foi observada.</summary>
    public required DateTimeOffset FirstSeenAt { get; init; }

    /// <summary>Última vez que esta aresta foi observada.</summary>
    public required DateTimeOffset LastSeenAt { get; init; }

    /// <summary>Total de chamadas observadas desde a primeira ocorrência.</summary>
    public long TotalCallCount { get; init; }

    /// <summary>
    /// Se verdadeiro, esta dependência não consta no catálogo declarado.
    /// Shadow dependencies são um sinal de risco para blast radius.
    /// </summary>
    public bool IsShadowDependency { get; init; }

    /// <summary>Timestamp de criação/atualização deste registro.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
