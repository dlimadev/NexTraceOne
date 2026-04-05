using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgent;

/// <summary>
/// Feature: GetAgent — obtém detalhes completos de um agent pelo ID.
/// Retorna todas as propriedades, incluindo configuração de runtime.
/// Estrutura VSA: Query + Handler + Response.
/// </summary>
public static class GetAgent
{
    /// <summary>Query para obter um agent por ID.</summary>
    public sealed record Query(Guid AgentId) : IQuery<Response>;

    /// <summary>Validador da query GetAgent.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AgentId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém detalhes de um agent.</summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var agent = await agentRepository.GetByIdAsync(
                AiAgentId.From(request.AgentId), cancellationToken);

            if (agent is null)
                return AiGovernanceErrors.AgentNotFound(request.AgentId.ToString());

            return new Response(
                agent.Id.Value,
                agent.Name,
                agent.DisplayName,
                agent.Slug,
                agent.Description,
                agent.Category.ToString(),
                agent.IsOfficial,
                agent.IsActive,
                agent.SystemPrompt,
                agent.Objective,
                agent.PreferredModelId,
                agent.Capabilities,
                agent.TargetPersona,
                agent.Icon,
                agent.SortOrder,
                agent.OwnershipType.ToString(),
                agent.Visibility.ToString(),
                agent.PublicationStatus.ToString(),
                agent.OwnerId,
                agent.OwnerTeamId,
                agent.AllowedModelIds,
                agent.AllowedTools,
                agent.InputSchema,
                agent.OutputSchema,
                agent.AllowModelOverride,
                agent.Version,
                agent.ExecutionCount);
        }
    }

    /// <summary>Resposta com detalhes completos do agent.</summary>
    public sealed record Response(
        Guid AgentId,
        string Name,
        string DisplayName,
        string Slug,
        string Description,
        string Category,
        bool IsOfficial,
        bool IsActive,
        string SystemPrompt,
        string Objective,
        Guid? PreferredModelId,
        string Capabilities,
        string TargetPersona,
        string Icon,
        int SortOrder,
        string OwnershipType,
        string Visibility,
        string PublicationStatus,
        string OwnerId,
        string OwnerTeamId,
        string AllowedModelIds,
        string AllowedTools,
        string InputSchema,
        string OutputSchema,
        bool AllowModelOverride,
        int Version,
        long ExecutionCount);
}
