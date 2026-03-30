using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetPromptTemplate;

/// <summary>
/// Feature: GetPromptTemplate — obtém detalhes completos de um template de prompt.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetPromptTemplate
{
    /// <summary>Query de consulta de um template de prompt pelo identificador.</summary>
    public sealed record Query(Guid TemplateId) : IQuery<Response>;

    /// <summary>Handler que obtém os detalhes completos de um template de prompt.</summary>
    public sealed class Handler(
        IPromptTemplateRepository templateRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await templateRepository.GetByIdAsync(
                PromptTemplateId.From(request.TemplateId), cancellationToken);

            if (template is null)
                return AiGovernanceErrors.PromptTemplateNotFound(request.TemplateId.ToString());

            return new Response(
                template.Id.Value, template.Name, template.DisplayName,
                template.Description, template.Category, template.Content,
                template.Variables, template.Version, template.IsActive,
                template.IsOfficial, template.AgentId, template.TargetPersonas,
                template.ScopeHint, template.Relevance, template.PreferredModelId,
                template.RecommendedTemperature, template.MaxOutputTokens);
        }
    }

    /// <summary>Resposta com detalhes completos de um template de prompt.</summary>
    public sealed record Response(
        Guid TemplateId, string Name, string DisplayName, string Description,
        string Category, string Content, string Variables, int Version,
        bool IsActive, bool IsOfficial, Guid? AgentId, string TargetPersonas,
        string? ScopeHint, string Relevance, Guid? PreferredModelId,
        decimal? RecommendedTemperature, int? MaxOutputTokens);
}
