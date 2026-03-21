using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Artefacto produzido por uma execução de agent de IA.
/// Pode ser um draft OpenAPI, cenários de teste, schema Kafka, documentação, etc.
/// Sujeito a review antes de aceitação.
///
/// Ciclo de vida: Pending → (Approved | Rejected | Superseded).
///
/// Invariantes:
/// - ExecutionId e AgentId são obrigatórios.
/// - Title e Content são obrigatórios.
/// - ReviewStatus inicia em Pending.
/// </summary>
public sealed class AiAgentArtifact : AuditableEntity<AiAgentArtifactId>
{
    private AiAgentArtifact() { }

    /// <summary>Execução que produziu o artefacto.</summary>
    public AiAgentExecutionId ExecutionId { get; private set; } = null!;

    /// <summary>Agent que produziu o artefacto.</summary>
    public AiAgentId AgentId { get; private set; } = null!;

    /// <summary>Tipo de artefacto gerado.</summary>
    public AgentArtifactType ArtifactType { get; private set; }

    /// <summary>Título do artefacto.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Conteúdo completo do artefacto.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Formato do conteúdo (ex: "yaml", "json", "markdown").</summary>
    public string Format { get; private set; } = string.Empty;

    /// <summary>Estado de review do artefacto.</summary>
    public ArtifactReviewStatus ReviewStatus { get; private set; }

    /// <summary>Utilizador que efectuou a review.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>Data/hora da review.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Notas justificativas da review.</summary>
    public string? ReviewNotes { get; private set; }

    /// <summary>Versão do artefacto.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Cria um novo artefacto de execução.</summary>
    public static AiAgentArtifact Create(
        AiAgentExecutionId executionId,
        AiAgentId agentId,
        AgentArtifactType artifactType,
        string title,
        string content,
        string format)
    {
        Guard.Against.Null(executionId);
        Guard.Against.Null(agentId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(content);

        return new AiAgentArtifact
        {
            Id = AiAgentArtifactId.New(),
            ExecutionId = executionId,
            AgentId = agentId,
            ArtifactType = artifactType,
            Title = title,
            Content = content,
            Format = format ?? string.Empty,
            ReviewStatus = ArtifactReviewStatus.Pending,
            Version = 1,
        };
    }

    /// <summary>Aprova o artefacto.</summary>
    public Result<bool> Approve(string reviewedBy, DateTimeOffset reviewedAt, string? notes = null)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        if (ReviewStatus != ArtifactReviewStatus.Pending)
            return Error.Business("AiGovernance.Artifact.AlreadyReviewed",
                "Artifact has already been reviewed.");

        ReviewStatus = ArtifactReviewStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        ReviewNotes = notes;
        return true;
    }

    /// <summary>Rejeita o artefacto.</summary>
    public Result<bool> Reject(string reviewedBy, DateTimeOffset reviewedAt, string? notes = null)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        if (ReviewStatus != ArtifactReviewStatus.Pending)
            return Error.Business("AiGovernance.Artifact.AlreadyReviewed",
                "Artifact has already been reviewed.");

        ReviewStatus = ArtifactReviewStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        ReviewNotes = notes;
        return true;
    }

    /// <summary>Marca como supérfluo (substituído por versão mais recente).</summary>
    public void Supersede() => ReviewStatus = ArtifactReviewStatus.Superseded;
}

/// <summary>Identificador fortemente tipado de AiAgentArtifact.</summary>
public sealed record AiAgentArtifactId(Guid Value) : TypedIdBase(Value)
{
    public static AiAgentArtifactId New() => new(Guid.NewGuid());
    public static AiAgentArtifactId From(Guid id) => new(id);
}
