using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de definições de ferramentas disponíveis para agentes IA.
/// Suporta consulta por nome, categoria e gestão de estado (ativo/inativo).
/// </summary>
public interface IAiToolDefinitionRepository
{
    /// <summary>Obtém uma definição de ferramenta pelo identificador.</summary>
    Task<AiToolDefinition?> GetByIdAsync(AiToolDefinitionId id, CancellationToken ct = default);

    /// <summary>Obtém uma definição de ferramenta pelo nome técnico.</summary>
    Task<AiToolDefinition?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Lista todas as definições de ferramentas ativas.</summary>
    Task<IReadOnlyList<AiToolDefinition>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Lista definições filtradas por categoria.</summary>
    Task<IReadOnlyList<AiToolDefinition>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Verifica se já existe uma ferramenta com o nome especificado.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Adiciona uma nova definição para persistência.</summary>
    Task AddAsync(AiToolDefinition entity, CancellationToken ct = default);
}
