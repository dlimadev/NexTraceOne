using System.Text.Json;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Nó do Organizational Memory Engine — captura decisões, incidentes, contratos e padrões aprendidos.
/// Forma um grafo temporal de conhecimento organizacional.
/// </summary>
public sealed class OrganizationalMemoryNode : AuditableEntity<OrganizationalMemoryNodeId>
{
    private OrganizationalMemoryNode() { }

    public string NodeType { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Context { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string TagsJson { get; private set; } = "[]";
    public string LinkedNodeIdsJson { get; private set; } = "[]";
    public string SourceType { get; private set; } = string.Empty;
    public string? SourceId { get; private set; }
    public double RelevanceScore { get; private set; } = 1.0;
    public Guid TenantId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public uint RowVersion { get; set; }

    public IReadOnlyList<string> Tags =>
        JsonSerializer.Deserialize<List<string>>(TagsJson) ?? [];

    public IReadOnlyList<Guid> LinkedNodeIds =>
        JsonSerializer.Deserialize<List<Guid>>(LinkedNodeIdsJson) ?? [];

    public static OrganizationalMemoryNode Create(
        string nodeType,
        string subject,
        string title,
        string content,
        string context,
        string actorId,
        string[] tags,
        string sourceType,
        string? sourceId,
        Guid tenantId,
        DateTimeOffset recordedAt)
    {
        Guard.Against.NullOrWhiteSpace(nodeType);
        Guard.Against.NullOrWhiteSpace(subject);
        Guard.Against.NullOrWhiteSpace(title);

        return new OrganizationalMemoryNode
        {
            Id = OrganizationalMemoryNodeId.New(),
            NodeType = nodeType,
            Subject = subject,
            Title = title,
            Content = content ?? string.Empty,
            Context = context ?? string.Empty,
            ActorId = actorId ?? string.Empty,
            TagsJson = JsonSerializer.Serialize(tags ?? []),
            SourceType = sourceType ?? string.Empty,
            SourceId = sourceId,
            TenantId = tenantId,
            RecordedAt = recordedAt,
            RelevanceScore = 1.0,
        };
    }

    public void LinkTo(Guid relatedNodeId)
    {
        var list = JsonSerializer.Deserialize<List<Guid>>(LinkedNodeIdsJson) ?? [];
        if (!list.Contains(relatedNodeId))
            list.Add(relatedNodeId);
        LinkedNodeIdsJson = JsonSerializer.Serialize(list);
    }

    public void UpdateRelevanceScore(double score)
    {
        RelevanceScore = Math.Clamp(score, 0.0, 1.0);
    }
}

/// <summary>Identificador fortemente tipado de OrganizationalMemoryNode.</summary>
public sealed record OrganizationalMemoryNodeId(Guid Value) : TypedIdBase(Value)
{
    public static OrganizationalMemoryNodeId New() => new(Guid.NewGuid());
    public static OrganizationalMemoryNodeId From(Guid id) => new(id);
}
