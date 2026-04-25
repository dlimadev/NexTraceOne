using System.Diagnostics;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteSkill;

/// <summary>
/// Feature: ExecuteSkill — executa uma skill de IA via LLM e regista o log de execução.
/// Verifica que a skill está ativa, delega ao ISkillExecutor e persiste o resultado.
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

    /// <summary>Handler que executa a skill via LLM e regista o log de execução.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository,
        IAiSkillExecutionRepository executionRepository,
        ISkillExecutor skillExecutor,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var skill = await skillRepository.GetByIdAsync(
                AiSkillId.From(request.SkillId), cancellationToken);

            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillId.ToString());

            if (skill.Status != SkillStatus.Active)
                return AiGovernanceErrors.SkillNotActive(skill.Name);

            var agentId = request.AgentId.HasValue ? AiAgentId.From(request.AgentId.Value) : null;
            var executedAt = dateTimeProvider.UtcNow;

            // Execute skill via AI Runtime
            var output = await skillExecutor.ExecuteAsync(
                skill,
                request.InputJson,
                request.ModelOverride,
                request.TenantId,
                request.ExecutedBy,
                cancellationToken);

            // Log the execution
            var durationMs = (long)output.Duration.TotalMilliseconds;
            var execution = AiSkillExecution.Log(
                skillId: skill.Id,
                executedBy: request.ExecutedBy,
                modelUsed: output.ModelUsed,
                inputJson: request.InputJson,
                outputJson: output.OutputJson,
                durationMs: durationMs,
                promptTokens: output.PromptTokens,
                completionTokens: output.CompletionTokens,
                isSuccess: output.Success,
                errorMessage: output.ErrorMessage,
                tenantId: request.TenantId,
                executedAt: executedAt,
                agentId: agentId);

            executionRepository.Add(execution);
            skill.IncrementExecutionCount();

            var status = output.Success ? "Completed" : "Failed";

            return new Response(
                ExecutionId: execution.Id.Value,
                SkillId: skill.Id.Value,
                SkillName: skill.Name,
                Status: status,
                OutputJson: output.OutputJson,
                ModelUsed: output.ModelUsed,
                ProviderId: output.ProviderId,
                PromptTokens: output.PromptTokens,
                CompletionTokens: output.CompletionTokens,
                DurationMs: durationMs,
                ErrorMessage: output.ErrorMessage);
        }
    }

    /// <summary>Resposta da execução de skill.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid SkillId,
        string SkillName,
        string Status,
        string OutputJson,
        string ModelUsed,
        string ProviderId,
        int PromptTokens,
        int CompletionTokens,
        long DurationMs,
        string? ErrorMessage = null);
}
