using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetToolDefinition;

/// <summary>
/// Feature: GetToolDefinition — obtém detalhes completos de uma definição de ferramenta.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetToolDefinition
{
    /// <summary>Query de consulta de uma definição de ferramenta pelo identificador.</summary>
    public sealed record Query(Guid ToolId) : IQuery<Response>;

    /// <summary>Validador da query GetToolDefinition.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ToolId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém os detalhes completos de uma definição de ferramenta.</summary>
    public sealed class Handler(
        IAiToolDefinitionRepository toolRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tool = await toolRepository.GetByIdAsync(
                AiToolDefinitionId.From(request.ToolId), cancellationToken);

            if (tool is null)
                return AiGovernanceErrors.ToolDefinitionNotFound(request.ToolId.ToString());

            return new Response(
                tool.Id.Value, tool.Name, tool.DisplayName, tool.Description,
                tool.Category, tool.ParametersSchema, tool.Version,
                tool.IsActive, tool.RequiresApproval, tool.RiskLevel,
                tool.IsOfficial, tool.TimeoutMs);
        }
    }

    /// <summary>Resposta com detalhes completos de uma definição de ferramenta.</summary>
    public sealed record Response(
        Guid ToolId, string Name, string DisplayName, string Description,
        string Category, string ParametersSchema, int Version,
        bool IsActive, bool RequiresApproval, int RiskLevel,
        bool IsOfficial, int TimeoutMs);
}
