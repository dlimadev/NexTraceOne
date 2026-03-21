using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteAgent;

/// <summary>
/// Feature: ExecuteAgent — executa um agent de IA com input e modelo opcionais.
/// Delega ao IAiAgentRuntimeService que orquestra o pipeline completo.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class ExecuteAgent
{
    /// <summary>Comando de execução de agent.</summary>
    public sealed record Command(
        Guid AgentId,
        string Input,
        Guid? ModelIdOverride,
        string? ContextJson) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de execução.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AgentId).NotEmpty();
            RuleFor(x => x.Input).NotEmpty().MaximumLength(32_000);
            RuleFor(x => x.ContextJson).MaximumLength(16_000);
        }
    }

    /// <summary>Handler que executa um agent via runtime service.</summary>
    public sealed class Handler(
        IAiAgentRuntimeService runtimeService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var agentId = Domain.Governance.Entities.AiAgentId.From(request.AgentId);

            var result = await runtimeService.ExecuteAsync(
                agentId,
                request.Input,
                request.ModelIdOverride,
                request.ContextJson,
                cancellationToken);

            if (result.IsFailure)
                return result.Error;

            var executionResult = result.Value;

            return new Response(
                executionResult.ExecutionId,
                executionResult.AgentId,
                executionResult.AgentName,
                executionResult.Status,
                executionResult.Output,
                executionResult.PromptTokens,
                executionResult.CompletionTokens,
                executionResult.DurationMs,
                executionResult.Artifacts.Select(a => new ArtifactItem(
                    a.ArtifactId, a.ArtifactType, a.Title, a.Format)).ToList());
        }
    }

    /// <summary>Resposta da execução de agent.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid AgentId,
        string AgentName,
        string Status,
        string Output,
        int PromptTokens,
        int CompletionTokens,
        long DurationMs,
        IReadOnlyList<ArtifactItem> Artifacts);

    /// <summary>Artefacto produzido na execução.</summary>
    public sealed record ArtifactItem(
        Guid ArtifactId,
        string ArtifactType,
        string Title,
        string Format);
}
