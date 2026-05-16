using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterSkill;

/// <summary>
/// Feature: RegisterSkill — regista uma nova skill de IA customizada.
/// Skills System são criadas via SeedDefaultSkills.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RegisterSkill
{
    /// <summary>Comando de registo de uma nova skill.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string SkillContent,
        string OwnershipType,
        string Visibility,
        string OwnerId,
        Guid TenantId,
        string[]? Tags,
        string[]? RequiredTools,
        string[]? PreferredModels,
        string? InputSchema,
        string? OutputSchema,
        bool IsComposable,
        string? ParentAgentId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de skill.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.SkillContent).NotEmpty();
            RuleFor(x => x.OwnershipType).NotEmpty()
                .Must(v => v != "System")
                .WithMessage("System skills cannot be created via API. Use SeedDefaultSkills.");
            RuleFor(x => x.Visibility).NotEmpty();
            RuleFor(x => x.OwnerId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.InputSchema).MaximumLength(16000);
            RuleFor(x => x.OutputSchema).MaximumLength(16000);
        }
    }

    /// <summary>Handler que regista uma nova skill customizada.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<SkillOwnershipType>(request.OwnershipType, ignoreCase: true, out var ownershipType))
                return Error.Validation("Skill.InvalidOwnershipType", $"'{request.OwnershipType}' is not a valid skill ownership type.");
            if (!Enum.TryParse<SkillVisibility>(request.Visibility, ignoreCase: true, out var visibility))
                return Error.Validation("Skill.InvalidVisibility", $"'{request.Visibility}' is not a valid skill visibility.");

            var exists = await skillRepository.ExistsByNameAsync(request.Name, request.TenantId, cancellationToken);
            if (exists)
                return AiGovernanceErrors.SkillNameAlreadyExists(request.Name);

            var skill = AiSkill.CreateCustom(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                skillContent: request.SkillContent,
                ownershipType: ownershipType,
                visibility: visibility,
                ownerId: request.OwnerId,
                tenantId: request.TenantId,
                tags: request.Tags,
                requiredTools: request.RequiredTools,
                preferredModels: request.PreferredModels,
                inputSchema: request.InputSchema,
                outputSchema: request.OutputSchema,
                isComposable: request.IsComposable,
                parentAgentId: request.ParentAgentId);

            skillRepository.Add(skill);

            return new Response(skill.Id.Value, skill.Name, skill.Status.ToString());
        }
    }

    /// <summary>Resposta do registo de skill.</summary>
    public sealed record Response(Guid SkillId, string Name, string Status);
}
