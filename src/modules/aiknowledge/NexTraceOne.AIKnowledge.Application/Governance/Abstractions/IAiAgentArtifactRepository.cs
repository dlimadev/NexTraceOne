using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de artefactos produzidos por execuções de agents.
/// Suporta listagem por execução e agent, e obtenção individual.
/// </summary>
public interface IAiAgentArtifactRepository
{
    /// <summary>Obtém um artefacto pelo identificador.</summary>
    Task<AiAgentArtifact?> GetByIdAsync(AiAgentArtifactId id, CancellationToken ct);

    /// <summary>Lista artefactos de uma execução específica.</summary>
    Task<IReadOnlyList<AiAgentArtifact>> ListByExecutionAsync(
        AiAgentExecutionId executionId, CancellationToken ct);

    /// <summary>Lista artefactos de um agent com filtro opcional de review status.</summary>
    Task<IReadOnlyList<AiAgentArtifact>> ListByAgentAsync(
        AiAgentId agentId, ArtifactReviewStatus? reviewStatus, int pageSize, CancellationToken ct);

    /// <summary>Adiciona um novo artefacto.</summary>
    Task AddAsync(AiAgentArtifact artifact, CancellationToken ct);

    /// <summary>Atualiza um artefacto existente.</summary>
    Task UpdateAsync(AiAgentArtifact artifact, CancellationToken ct);
}
