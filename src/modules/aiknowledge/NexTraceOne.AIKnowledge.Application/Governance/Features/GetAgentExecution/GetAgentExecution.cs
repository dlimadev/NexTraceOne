using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentExecution;

/// <summary>
/// Feature: GetAgentExecution — obtém detalhes de uma execução de agent.
/// Inclui artefactos produzidos na mesma execução.
/// Estrutura VSA: Query + Handler + Response.
/// </summary>
public static class GetAgentExecution
{
    /// <summary>Query para obter uma execução por ID.</summary>
    public sealed record Query(Guid ExecutionId) : IQuery<Response>;

    /// <summary>Validador da query GetAgentExecution.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ExecutionId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém detalhes de uma execução.</summary>
    public sealed class Handler(
        IAiAgentExecutionRepository executionRepository,
        IAiAgentArtifactRepository artifactRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var execution = await executionRepository.GetByIdAsync(
                AiAgentExecutionId.From(request.ExecutionId), cancellationToken);

            if (execution is null)
                return AiGovernanceErrors.AgentExecutionNotFound(request.ExecutionId.ToString());

            var artifacts = await artifactRepository.ListByExecutionAsync(
                execution.Id, cancellationToken);

            return new Response(
                execution.Id.Value,
                execution.AgentId.Value,
                execution.ExecutedBy,
                execution.Status.ToString(),
                execution.ModelIdUsed,
                execution.ProviderUsed,
                execution.InputJson,
                execution.OutputJson,
                execution.PromptTokens,
                execution.CompletionTokens,
                execution.TotalTokens,
                execution.DurationMs,
                execution.StartedAt,
                execution.CompletedAt,
                execution.CorrelationId,
                execution.ErrorMessage,
                artifacts.Select(a => new ArtifactSummary(
                    a.Id.Value,
                    a.ArtifactType.ToString(),
                    a.Title,
                    a.Format,
                    a.ReviewStatus.ToString())).ToList());
        }
    }

    /// <summary>Resposta com detalhes da execução.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid AgentId,
        string ExecutedBy,
        string Status,
        Guid ModelIdUsed,
        string ProviderUsed,
        string InputJson,
        string OutputJson,
        int PromptTokens,
        int CompletionTokens,
        int TotalTokens,
        long DurationMs,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string CorrelationId,
        string? ErrorMessage,
        IReadOnlyList<ArtifactSummary> Artifacts);

    /// <summary>Resumo de artefacto na resposta de execução.</summary>
    public sealed record ArtifactSummary(
        Guid ArtifactId,
        string ArtifactType,
        string Title,
        string Format,
        string ReviewStatus);
}
