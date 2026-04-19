using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

internal sealed class SkillLoader(IAiSkillRepository skillRepository) : ISkillLoader
{
    public async Task<string?> LoadContentAsync(string skillName, Guid tenantId, CancellationToken ct)
    {
        var skill = await skillRepository.GetByNameAsync(skillName, tenantId, ct);
        return skill?.SkillContent;
    }

    public async Task<string?> LoadContentByIdAsync(Guid skillId, CancellationToken ct)
    {
        var skill = await skillRepository.GetByIdAsync(AiSkillId.From(skillId), ct);
        return skill?.SkillContent;
    }

    public async Task<IReadOnlyList<SkillSummaryDto>> ListActiveSummariesAsync(Guid tenantId, CancellationToken ct)
    {
        var skills = await skillRepository.ListAsync(SkillStatus.Active, null, tenantId, ct);
        return skills.Select(s => new SkillSummaryDto(
            s.Id.Value,
            s.Name,
            s.DisplayName,
            s.Description,
            s.Tags.Length > 0 ? s.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries) : [],
            s.RequiredTools.Length > 0 ? s.RequiredTools.Split(',', StringSplitOptions.RemoveEmptyEntries) : []
        )).ToList().AsReadOnly();
    }
}
