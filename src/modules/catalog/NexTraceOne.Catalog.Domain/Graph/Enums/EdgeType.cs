namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tipos semânticos de arestas no grafo de engenharia.
/// Cada tipo define a natureza da relação entre dois nós,
/// com direção clara para permitir propagação de impacto e explicabilidade visual.
/// </summary>
public enum EdgeType
{
    /// <summary>A → B: A é proprietário de B (Team→Service, Service→API).</summary>
    Owns = 0,

    /// <summary>A → B: A contém B (Domain→Team, Service→Endpoint).</summary>
    Contains = 1,

    /// <summary>A → B: A depende de B em tempo de design ou runtime.</summary>
    DependsOn = 2,

    /// <summary>A → B: A invoca B em runtime (descoberto via OpenTelemetry).</summary>
    Calls = 3,

    /// <summary>A → B: A expõe B (Service→API, API→Endpoint).</summary>
    Exposes = 4,

    /// <summary>A → B: A está implantado em B (Service→Environment).</summary>
    DeployedTo = 5,

    /// <summary>A → B: A impacta B (propagação de blast radius).</summary>
    Impacts = 6,

    /// <summary>A → B: A pertence ao domínio B (Service→Domain).</summary>
    BelongsTo = 7,

    // ── Novos tipos de arestas para core systems / mainframe ──

    /// <summary>A → B: A produz mensagens para B (Service → MQ Queue).</summary>
    Produces = 8,

    /// <summary>A → B: A consome mensagens de B (Service → MQ Queue).</summary>
    Consumes = 9,

    /// <summary>A → B: A desencadeia a execução de B (Batch Job → Batch Job).</summary>
    Triggers = 10,

    /// <summary>A → B: A agenda a execução de B (Scheduler → Batch Job).</summary>
    Schedules = 11,

    /// <summary>A → B: A utiliza B (COBOL Program → Copybook).</summary>
    Uses = 12,

    /// <summary>A → B: A está vinculado a B (z/OS Connect → CICS Transaction).</summary>
    BoundTo = 13
}
