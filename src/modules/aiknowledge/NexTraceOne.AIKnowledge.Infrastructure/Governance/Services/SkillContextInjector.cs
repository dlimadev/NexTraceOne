using System.Text;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

internal sealed class SkillContextInjector(ISkillLoader loader) : ISkillContextInjector
{
    public async Task<string> InjectSkillsAsync(string baseSystemPrompt, IEnumerable<string> skillNames, Guid tenantId, CancellationToken ct)
    {
        var sb = new StringBuilder(baseSystemPrompt);
        sb.Append("\n\n## Active Skills\n");
        foreach (var skillName in skillNames)
        {
            var content = await loader.LoadContentAsync(skillName, tenantId, ct);
            if (content is not null)
                sb.Append($"\n### Skill: {skillName}\n{content}\n");
        }
        return sb.ToString();
    }

    public async Task<string> BuildSkillsSummaryBlockAsync(Guid tenantId, CancellationToken ct)
    {
        var summaries = await loader.ListActiveSummariesAsync(tenantId, ct);
        var sb = new StringBuilder("## Available Skills (Progressive Disclosure)\n");
        foreach (var skill in summaries)
        {
            var tools = skill.RequiredTools.Length > 0 ? string.Join(", ", skill.RequiredTools) : "none";
            sb.Append($"- {skill.Name}: {skill.Description} [tools: {tools}]\n");
        }
        return sb.ToString();
    }
}
