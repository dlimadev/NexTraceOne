using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Domain.Entities;

/// <summary>
/// Snapshot persistido do knowledge graph operacional, capturando o estado computado
/// do grafo (nós, arestas, componentes conexos) num ponto no tempo.
/// Permite tracking histórico de evolução da conectividade e cobertura do conhecimento.
///
/// Ciclo de vida: Generated → (Reviewed | Stale).
/// Fica Stale quando um novo snapshot é gerado.
///
/// Pilar: Source of Truth &amp; Operational Knowledge.
/// Ideia 6 — Operational Knowledge Graph.
/// </summary>
public sealed class KnowledgeGraphSnapshot : AuditableEntity<KnowledgeGraphSnapshotId>
{
    private KnowledgeGraphSnapshot() { }

    /// <summary>Número total de nós no grafo (documentos, serviços, contratos, etc.).</summary>
    public int TotalNodes { get; private set; }

    /// <summary>Número total de arestas (relações) no grafo.</summary>
    public int TotalEdges { get; private set; }

    /// <summary>Número de componentes conexos identificados.</summary>
    public int ConnectedComponents { get; private set; }

    /// <summary>Nós isolados sem relação com outros (count).</summary>
    public int IsolatedNodes { get; private set; }

    /// <summary>Score de cobertura do grafo (0-100): mede quão conectado está o conhecimento.</summary>
    public int CoverageScore { get; private set; }

    /// <summary>Distribuição de nós por tipo (JSONB) — ex: {"Document":15,"Service":10,"Contract":8}.</summary>
    public string NodeTypeDistribution { get; private set; } = string.Empty;

    /// <summary>Distribuição de arestas por tipo de relação (JSONB).</summary>
    public string EdgeTypeDistribution { get; private set; } = string.Empty;

    /// <summary>Top entidades com mais conexões (JSONB).</summary>
    public string? TopConnectedEntities { get; private set; }

    /// <summary>Entidades órfãs sem relação (JSONB).</summary>
    public string? OrphanEntities { get; private set; }

    /// <summary>Recomendações para melhorar a conectividade do knowledge graph (JSONB).</summary>
    public string? Recommendations { get; private set; }

    /// <summary>Estado do snapshot no ciclo de vida.</summary>
    public KnowledgeGraphSnapshotStatus Status { get; private set; } = KnowledgeGraphSnapshotStatus.Generated;

    /// <summary>Data/hora UTC em que o snapshot foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Data/hora UTC da última revisão (se aplicável).</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Comentário de revisão do utilizador.</summary>
    public string? ReviewComment { get; private set; }

    /// <summary>Tenant ao qual pertence o snapshot.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Cria um novo snapshot do knowledge graph.</summary>
    public static KnowledgeGraphSnapshot Generate(
        int totalNodes,
        int totalEdges,
        int connectedComponents,
        int isolatedNodes,
        int coverageScore,
        string nodeTypeDistribution,
        string edgeTypeDistribution,
        string? topConnectedEntities,
        string? orphanEntities,
        string? recommendations,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Negative(totalNodes, nameof(totalNodes));
        Guard.Against.Negative(totalEdges, nameof(totalEdges));
        Guard.Against.Negative(connectedComponents, nameof(connectedComponents));
        Guard.Against.Negative(isolatedNodes, nameof(isolatedNodes));
        Guard.Against.NullOrWhiteSpace(nodeTypeDistribution, nameof(nodeTypeDistribution));
        Guard.Against.NullOrWhiteSpace(edgeTypeDistribution, nameof(edgeTypeDistribution));

        if (coverageScore < 0 || coverageScore > 100)
            throw new ArgumentException("Coverage score must be between 0 and 100.", nameof(coverageScore));

        if (isolatedNodes > totalNodes)
            throw new ArgumentException("Isolated nodes cannot exceed total nodes.", nameof(isolatedNodes));

        return new KnowledgeGraphSnapshot
        {
            Id = KnowledgeGraphSnapshotId.New(),
            TotalNodes = totalNodes,
            TotalEdges = totalEdges,
            ConnectedComponents = connectedComponents,
            IsolatedNodes = isolatedNodes,
            CoverageScore = coverageScore,
            NodeTypeDistribution = nodeTypeDistribution,
            EdgeTypeDistribution = edgeTypeDistribution,
            TopConnectedEntities = topConnectedEntities,
            OrphanEntities = orphanEntities,
            Recommendations = recommendations,
            Status = KnowledgeGraphSnapshotStatus.Generated,
            TenantId = tenantId,
            GeneratedAt = generatedAt
        };
    }

    /// <summary>Marca o snapshot como revisado pelo utilizador.</summary>
    public Result<Unit> Review(string comment, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(comment, nameof(comment));

        if (Status == KnowledgeGraphSnapshotStatus.Reviewed)
            return Error.Conflict("KNOWLEDGE_GRAPH_SNAPSHOT_ALREADY_REVIEWED",
                $"Knowledge graph snapshot '{Id.Value}' has already been reviewed.");

        Status = KnowledgeGraphSnapshotStatus.Reviewed;
        ReviewComment = comment;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }

    /// <summary>Marca o snapshot como stale (substituído por snapshot mais recente).</summary>
    public Result<Unit> MarkAsStale()
    {
        if (Status == KnowledgeGraphSnapshotStatus.Stale)
            return Error.Conflict("KNOWLEDGE_GRAPH_SNAPSHOT_ALREADY_STALE",
                $"Knowledge graph snapshot '{Id.Value}' is already stale.");

        Status = KnowledgeGraphSnapshotStatus.Stale;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de KnowledgeGraphSnapshot.</summary>
public sealed record KnowledgeGraphSnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static KnowledgeGraphSnapshotId New() => new(Guid.NewGuid());
    public static KnowledgeGraphSnapshotId From(Guid id) => new(id);
}
