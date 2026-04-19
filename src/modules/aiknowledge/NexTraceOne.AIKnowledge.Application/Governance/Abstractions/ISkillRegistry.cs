namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>Registry dinâmico de Skills.</summary>
public interface ISkillRegistry
{
    Task<string?> ResolveSkillContentAsync(string skillName, Guid tenantId, CancellationToken ct);
    Task<bool> IsSkillAvailableAsync(string skillName, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetCompatibleSkillsAsync(IEnumerable<string> availableTools, Guid tenantId, CancellationToken ct);
}
