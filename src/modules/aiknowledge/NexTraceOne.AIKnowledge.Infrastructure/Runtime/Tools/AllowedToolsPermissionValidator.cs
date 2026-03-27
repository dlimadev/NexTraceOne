using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Valida permissões de tools com base no campo AllowedTools do agent.
/// AllowedTools é uma string CSV: "list_services,get_service_health,list_recent_changes".
/// Vazio = nenhuma tool permitida.
/// </summary>
public sealed class AllowedToolsPermissionValidator : IToolPermissionValidator
{
    private readonly IToolRegistry _registry;

    public AllowedToolsPermissionValidator(IToolRegistry registry)
    {
        _registry = registry;
    }

    public bool IsToolAllowed(string allowedToolsCsv, string toolName)
    {
        if (string.IsNullOrWhiteSpace(allowedToolsCsv))
            return false;

        var allowed = ParseAllowedTools(allowedToolsCsv);
        return allowed.Contains(toolName, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ToolDefinition> GetAllowedTools(string allowedToolsCsv)
    {
        if (string.IsNullOrWhiteSpace(allowedToolsCsv))
            return [];

        var allowed = ParseAllowedTools(allowedToolsCsv);
        return _registry.GetAll()
            .Where(t => allowed.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static HashSet<string> ParseAllowedTools(string csv)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
