using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateSkill;

/// <summary>
/// Feature: UpdateSkill — atualiza os metadados de uma skill existente.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class UpdateSkill
{
    /// <summary>Comando de atualização de uma skill.</summary>
    public sealed record Command(
        Guid SkillId,
        string DisplayName,
        string Description,
        string SkillContent,
        string[]? Tags,
        string[]? RequiredTools,
        string[]? PreferredModels,
        string? InputSchema,
        string? OutputSchema,
        bool? IsComposable) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de skill.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SkillId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.InputSchema).MaximumLength(16000);
            RuleFor(x => x.OutputSchema).MaximumLength(16000);
        }
    }

    /// <summary>Handler que atualiza uma skill existente.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var skill = await skillRepository.GetByIdAsync(
                Domain.Governance.Entities.AiSkillId.From(request.SkillId), cancellationToken);

            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillId.ToString());

            skill.Update(
                request.DisplayName,
                request.Description,
                request.SkillContent,
                request.Tags,
                request.RequiredTools,
                request.PreferredModels,
                request.InputSchema,
                request.OutputSchema,
                request.IsComposable);

            return new Response(skill.Id.Value, skill.Name, skill.Status.ToString(), skill.Version);
        }
    }

    /// <summary>Resposta da atualização de skill.</summary>
    public sealed record Response(Guid SkillId, string Name, string Status, string Version);
}
