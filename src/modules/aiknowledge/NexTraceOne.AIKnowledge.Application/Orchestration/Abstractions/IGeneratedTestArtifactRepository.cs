using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Repositório de artefatos de teste gerados por IA.
/// </summary>
public interface IGeneratedTestArtifactRepository
{
    /// <summary>Adiciona e persiste um novo artefato de teste gerado.</summary>
    Task AddAsync(GeneratedTestArtifact artifact, CancellationToken ct);

    /// <summary>Lista os artefatos mais recentes de uma release.</summary>
    Task<IReadOnlyList<ArtifactSummaryData>> GetRecentByReleaseAsync(
        Guid releaseId,
        int maxCount,
        CancellationToken ct);
}

/// <summary>Resumo de artefato de teste para contexto de release.</summary>
public sealed record ArtifactSummaryData(
    string ServiceName,
    string TestFramework,
    string Status,
    decimal Confidence);
