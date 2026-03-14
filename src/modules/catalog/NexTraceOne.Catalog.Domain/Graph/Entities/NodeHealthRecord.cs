using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Registro de saúde/métricas de um nó do grafo para overlays explicáveis.
/// Cada registro captura o estado calculado de um ativo em um instante,
/// incluindo score, status, fatores contribuintes e origem dos dados.
/// Permite ao usuário entender "por que este nó está vermelho"
/// com breakdown visível e rastreabilidade temporal.
/// </summary>
public sealed class NodeHealthRecord : Entity<NodeHealthRecordId>
{
    private NodeHealthRecord() { }

    /// <summary>Identificador do nó ao qual esta métrica se refere.</summary>
    public Guid NodeId { get; private set; }

    /// <summary>Tipo do nó referenciado (Service, Api, etc.).</summary>
    public NodeType NodeType { get; private set; }

    /// <summary>Modo de overlay ao qual este registro pertence.</summary>
    public OverlayMode OverlayMode { get; private set; }

    /// <summary>Status calculado para o nó neste overlay.</summary>
    public HealthStatus Status { get; private set; }

    /// <summary>Score numérico (0.0 a 1.0) — permite gradientes visuais e ranking.</summary>
    public decimal Score { get; private set; }

    /// <summary>Fatores que contribuíram para o score, serializados como JSON para explicabilidade.</summary>
    public string FactorsJson { get; private set; } = string.Empty;

    /// <summary>Instante UTC em que o cálculo foi realizado.</summary>
    public DateTimeOffset CalculatedAt { get; private set; }

    /// <summary>Sistema ou processo que originou o cálculo.</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um registro de saúde/métricas para um nó do grafo.
    /// Score deve estar entre 0.0 e 1.0 para consistência nos overlays.
    /// FactorsJson deve conter breakdown dos sinais contribuintes.
    /// </summary>
    public static NodeHealthRecord Create(
        Guid nodeId,
        NodeType nodeType,
        OverlayMode overlayMode,
        HealthStatus status,
        decimal score,
        string factorsJson,
        DateTimeOffset calculatedAt,
        string sourceSystem)
    {
        if (score < 0 || score > 1)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0.0 and 1.0.");

        return new NodeHealthRecord
        {
            Id = NodeHealthRecordId.New(),
            NodeId = Guard.Against.Default(nodeId),
            NodeType = nodeType,
            OverlayMode = overlayMode,
            Status = status,
            Score = score,
            FactorsJson = factorsJson ?? "{}",
            CalculatedAt = calculatedAt,
            SourceSystem = Guard.Against.NullOrWhiteSpace(sourceSystem)
        };
    }
}

/// <summary>Identificador fortemente tipado de NodeHealthRecord.</summary>
public sealed record NodeHealthRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static NodeHealthRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static NodeHealthRecordId From(Guid id) => new(id);
}
