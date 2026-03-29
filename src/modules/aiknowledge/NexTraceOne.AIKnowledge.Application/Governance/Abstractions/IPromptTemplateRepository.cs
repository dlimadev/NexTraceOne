using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de templates de prompt para operações assistidas por IA.
/// Suporta consulta por nome, categoria, persona e gestão de versões.
/// </summary>
public interface IPromptTemplateRepository
{
    /// <summary>Obtém um template pelo identificador.</summary>
    Task<PromptTemplate?> GetByIdAsync(PromptTemplateId id, CancellationToken ct = default);

    /// <summary>Obtém a versão ativa de um template pelo nome.</summary>
    Task<PromptTemplate?> GetActiveByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Lista todos os templates ativos.</summary>
    Task<IReadOnlyList<PromptTemplate>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Lista templates filtrados por categoria.</summary>
    Task<IReadOnlyList<PromptTemplate>> GetByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>Lista templates relevantes para uma persona específica.</summary>
    Task<IReadOnlyList<PromptTemplate>> GetByPersonaAsync(string persona, CancellationToken ct = default);

    /// <summary>Verifica se já existe um template com o nome especificado.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Adiciona um novo template para persistência.</summary>
    Task AddAsync(PromptTemplate entity, CancellationToken ct = default);
}
