namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>Carrega o conteúdo de uma Skill da base de dados ou do disco.</summary>
public interface ISkillLoader
{
    Task<string?> LoadContentAsync(string skillName, Guid tenantId, CancellationToken ct);
    Task<string?> LoadContentByIdAsync(Guid skillId, CancellationToken ct);
    Task<IReadOnlyList<SkillSummaryDto>> ListActiveSummariesAsync(Guid tenantId, CancellationToken ct);
}

public sealed record SkillSummaryDto(Guid SkillId, string Name, string DisplayName, string Description, string[] Tags, string[] RequiredTools);
