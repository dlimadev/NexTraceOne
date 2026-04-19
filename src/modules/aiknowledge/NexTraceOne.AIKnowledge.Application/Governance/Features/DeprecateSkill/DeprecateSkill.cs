using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.DeprecateSkill;

/// <summary>
/// Feature: DeprecateSkill — descontinua uma skill (Active → Deprecated).
/// Estrutura VSA: Command + Handler + Response num único ficheiro.
/// </summary>
public static class DeprecateSkill
{
    /// <summary>Comando para descontinuar uma skill pelo identificador.</summary>
    public sealed record Command(Guid SkillId) : ICommand<Response>;

    /// <summary>Handler que descontinua uma skill.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var skill = await skillRepository.GetByIdAsync(
                AiSkillId.From(request.SkillId), cancellationToken);

            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillId.ToString());

            skill.Deprecate();

            return new Response(skill.Id.Value, skill.Status.ToString());
        }
    }

    /// <summary>Resposta da descontinuação de skill.</summary>
    public sealed record Response(Guid SkillId, string Status);
}
