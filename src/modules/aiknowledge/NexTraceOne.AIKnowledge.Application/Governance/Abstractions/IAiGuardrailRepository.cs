using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de guardrails de proteção de input/output de IA.
/// Suporta consulta por nome, categoria, tipo de guarda e estado ativo.
/// </summary>
public interface IAiGuardrailRepository
{
    /// <summary>Obtém um guardrail pelo identificador.</summary>
    Task<AiGuardrail?> GetByIdAsync(AiGuardrailId id, CancellationToken ct = default);

    /// <summary>Obtém um guardrail pelo nome técnico.</summary>
    Task<AiGuardrail?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Lista todos os guardrails ativos ordenados por prioridade.</summary>
    Task<IReadOnlyList<AiGuardrail>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Lista guardrails filtrados por categoria.</summary>
    Task<IReadOnlyList<AiGuardrail>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Lista guardrails filtrados por tipo de guarda ("input", "output", "both").</summary>
    Task<IReadOnlyList<AiGuardrail>> GetByGuardTypeAsync(string guardType, CancellationToken ct = default);

    /// <summary>Verifica se já existe um guardrail com o nome especificado.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Adiciona um novo guardrail para persistência.</summary>
    Task AddAsync(AiGuardrail entity, CancellationToken ct = default);
}
