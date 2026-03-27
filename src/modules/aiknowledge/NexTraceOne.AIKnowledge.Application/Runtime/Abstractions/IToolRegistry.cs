namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Registo centralizado de tools disponíveis para agents de IA.
/// Permite resolver tools por nome e listar tools disponíveis por categoria.
/// </summary>
public interface IToolRegistry
{
    /// <summary>Obtém uma tool pelo nome exacto.</summary>
    ToolDefinition? GetByName(string toolName);

    /// <summary>Lista todas as tools registadas.</summary>
    IReadOnlyList<ToolDefinition> GetAll();

    /// <summary>Lista tools por categoria funcional.</summary>
    IReadOnlyList<ToolDefinition> GetByCategory(string category);

    /// <summary>Verifica se uma tool com o nome dado está registada.</summary>
    bool Exists(string toolName);
}
