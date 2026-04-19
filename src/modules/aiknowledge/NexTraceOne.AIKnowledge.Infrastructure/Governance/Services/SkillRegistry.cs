using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

internal sealed class SkillRegistry(ISkillLoader loader, IAiSkillRepository skillRepository) : ISkillRegistry
{
    public async Task<string?> ResolveSkillContentAsync(string skillName, Guid tenantId, CancellationToken ct)
        => await loader.LoadContentAsync(skillName, tenantId, ct);

    public async Task<bool> IsSkillAvailableAsync(string skillName, Guid tenantId, CancellationToken ct)
    {
        var skill = await skillRepository.GetByNameAsync(skillName, tenantId, ct);
        return skill != null && skill.Status == SkillStatus.Active;
    }

    public async Task<IReadOnlyList<string>> GetCompatibleSkillsAsync(IEnumerable<string> availableTools, Guid tenantId, CancellationToken ct)
    {
        var tools = availableTools.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skills = await skillRepository.ListAsync(SkillStatus.Active, null, tenantId, ct);
        return skills
            .Where(s => s.RequiredTools.Length == 0 ||
                        s.RequiredTools.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .All(t => tools.Contains(t)))
            .Select(s => s.Name)
            .ToList()
            .AsReadOnly();
    }
}
