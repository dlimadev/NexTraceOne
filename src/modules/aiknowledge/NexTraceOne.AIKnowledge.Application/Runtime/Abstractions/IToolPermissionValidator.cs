namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Valida se um agent tem permissão para executar uma tool específica
/// com base no campo AllowedTools do agent.
/// </summary>
public interface IToolPermissionValidator
{
    /// <summary>
    /// Verifica se uma tool está permitida para o agent.
    /// Usa a lista de AllowedTools configurada no agent (comma-separated).
    /// </summary>
    bool IsToolAllowed(string allowedToolsCsv, string toolName);

    /// <summary>
    /// Retorna a lista de tools permitidas para o agent,
    /// filtradas contra o registry de tools realmente disponíveis.
    /// </summary>
    IReadOnlyList<ToolDefinition> GetAllowedTools(string allowedToolsCsv);
}
