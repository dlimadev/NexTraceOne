using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.LoadSkill;

/// <summary>
/// Feature: LoadSkill — carrega o conteúdo de uma Skill para injecção em contexto de agente.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class LoadSkill
{
    public sealed record Query(string SkillName, Guid TenantId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(ISkillLoader skillLoader, IAiSkillRepository skillRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var skill = await skillRepository.GetByNameAsync(request.SkillName, request.TenantId, ct);
            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillName);

            var content = await skillLoader.LoadContentAsync(request.SkillName, request.TenantId, ct);

            return new Response(
                skill.Id.Value,
                skill.Name,
                skill.DisplayName,
                skill.Version,
                content ?? string.Empty,
                skill.InputSchema,
                skill.OutputSchema,
                skill.RequiredTools,
                skill.PreferredModels);
        }
    }

    public sealed record Response(
        Guid SkillId,
        string Name,
        string DisplayName,
        string Version,
        string Content,
        string InputSchema,
        string OutputSchema,
        string RequiredTools,
        string PreferredModels);
}
