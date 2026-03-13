using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Snapshot materializado do grafo de engenharia em um ponto no tempo.
/// Permite consultas temporais (time-travel), diff entre dois instantes
/// e replay do estado do grafo. Cada snapshot captura o estado completo
/// dos nós e arestas como JSON serializado, garantindo reprodutibilidade.
/// Estratégia: snapshots periódicos + change events entre eles.
/// </summary>
public sealed class GraphSnapshot : Entity<GraphSnapshotId>
{
    private GraphSnapshot() { }

    /// <summary>Rótulo amigável do snapshot (ex: "Pre-release v2.1", "Post-incident #42").</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Instante UTC em que o snapshot foi capturado.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>Estado serializado dos nós no momento da captura (JSON).</summary>
    public string NodesJson { get; private set; } = string.Empty;

    /// <summary>Estado serializado das arestas no momento da captura (JSON).</summary>
    public string EdgesJson { get; private set; } = string.Empty;

    /// <summary>Contagem de nós no momento da captura — para consultas rápidas sem deserializar.</summary>
    public int NodeCount { get; private set; }

    /// <summary>Contagem de arestas no momento da captura.</summary>
    public int EdgeCount { get; private set; }

    /// <summary>Usuário ou sistema que disparou a captura.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo snapshot do grafo com estado materializado.
    /// O JSON deve conter o estado completo dos nós e arestas
    /// para garantir reprodutibilidade total do grafo naquele instante.
    /// </summary>
    public static GraphSnapshot Create(
        string label,
        DateTimeOffset capturedAt,
        string nodesJson,
        string edgesJson,
        int nodeCount,
        int edgeCount,
        string createdBy)
    {
        return new GraphSnapshot
        {
            Id = GraphSnapshotId.New(),
            Label = Guard.Against.NullOrWhiteSpace(label),
            CapturedAt = capturedAt,
            NodesJson = Guard.Against.NullOrWhiteSpace(nodesJson),
            EdgesJson = Guard.Against.NullOrWhiteSpace(edgesJson),
            NodeCount = Guard.Against.Negative(nodeCount),
            EdgeCount = Guard.Against.Negative(edgeCount),
            CreatedBy = Guard.Against.NullOrWhiteSpace(createdBy)
        };
    }
}

/// <summary>Identificador fortemente tipado de GraphSnapshot.</summary>
public sealed record GraphSnapshotId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static GraphSnapshotId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static GraphSnapshotId From(Guid id) => new(id);
}
