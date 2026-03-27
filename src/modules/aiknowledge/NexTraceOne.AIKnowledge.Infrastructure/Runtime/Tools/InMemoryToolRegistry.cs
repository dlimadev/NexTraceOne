using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Registo in-memory de tools disponíveis para agents de IA.
/// Tools são registadas no arranque via DI e ficam disponíveis para o runtime.
/// Singleton — a lista de tools não muda durante a vida da aplicação.
/// </summary>
public sealed class InMemoryToolRegistry : IToolRegistry
{
    private readonly IReadOnlyDictionary<string, ToolDefinition> _tools;

    public InMemoryToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools
            .Select(t => t.Definition)
            .ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ToolDefinition? GetByName(string toolName)
        => _tools.GetValueOrDefault(toolName);

    public IReadOnlyList<ToolDefinition> GetAll()
        => _tools.Values.ToList();

    public IReadOnlyList<ToolDefinition> GetByCategory(string category)
        => _tools.Values
            .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public bool Exists(string toolName)
        => _tools.ContainsKey(toolName);
}
