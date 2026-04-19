using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de execuções de skills de IA.
/// Permite consulta e registo de logs de execução.
/// </summary>
public interface IAiSkillExecutionRepository
{
    /// <summary>Obtém uma execução pelo identificador fortemente tipado.</summary>
    Task<AiSkillExecution?> GetByIdAsync(AiSkillExecutionId id, CancellationToken ct);

    /// <summary>Lista execuções de uma skill com limite de resultados.</summary>
    Task<IReadOnlyList<AiSkillExecution>> ListBySkillAsync(AiSkillId skillId, int limit, CancellationToken ct);

    /// <summary>Adiciona uma nova execução para persistência.</summary>
    void Add(AiSkillExecution execution);
}
