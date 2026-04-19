namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>Injeta o conteúdo de uma ou mais Skills no system prompt.</summary>
public interface ISkillContextInjector
{
    Task<string> InjectSkillsAsync(string baseSystemPrompt, IEnumerable<string> skillNames, Guid tenantId, CancellationToken ct);
    Task<string> BuildSkillsSummaryBlockAsync(Guid tenantId, CancellationToken ct);
}
