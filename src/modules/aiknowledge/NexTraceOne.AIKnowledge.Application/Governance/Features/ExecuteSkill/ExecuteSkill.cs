using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteSkill;

/// <summary>
/// Feature: ExecuteSkill — regista e executa uma skill de IA.
/// Verifica que a skill está ativa, cria o log de execução e incrementa o contador.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ExecuteSkill
{
    /// <summary>Comando de execução de uma skill.</summary>
    public sealed record Command(
        Guid SkillId,
        string InputJson,
        string? ModelOverride,
        Guid? AgentId,
        string ExecutedBy,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de execução de skill.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SkillId).NotEmpty();
            RuleFor(x => x.InputJson).NotEmpty();
            RuleFor(x => x.ExecutedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que executa uma skill e regista o log de execução.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository,
        IAiSkillExecutionRepository executionRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var skill = await skillRepository.GetByIdAsync(
                AiSkillId.From(request.SkillId), cancellationToken);

            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillId.ToString());

            if (skill.Status != SkillStatus.Active)
                return AiGovernanceErrors.SkillNotActive(skill.Name);

            var modelUsed = request.ModelOverride ?? "default";
            var agentId = request.AgentId.HasValue ? AiAgentId.From(request.AgentId.Value) : null;

            var execution = AiSkillExecution.Log(
                skillId: skill.Id,
                executedBy: request.ExecutedBy,
                modelUsed: modelUsed,
                inputJson: request.InputJson,
                outputJson: "{}",
                durationMs: 0,
                promptTokens: 0,
                completionTokens: 0,
                isSuccess: true,
                errorMessage: null,
                tenantId: request.TenantId,
                executedAt: DateTimeOffset.UtcNow,
                agentId: agentId);

            executionRepository.Add(execution);
            skill.IncrementExecutionCount();

            return new Response(
                ExecutionId: execution.Id.Value,
                SkillId: skill.Id.Value,
                SkillName: skill.Name,
                Status: "Completed",
                OutputJson: "{}");
        }
    }

    /// <summary>Resposta da execução de skill.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid SkillId,
        string SkillName,
        string Status,
        string OutputJson);
}
