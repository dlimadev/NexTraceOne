using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListPromptTemplates;

/// <summary>
/// Feature: ListPromptTemplates — lista templates de prompt com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListPromptTemplates
{
    /// <summary>Query de listagem filtrada de templates de prompt.</summary>
    public sealed record Query(
        string? Category,
        string? Persona,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista templates de prompt com filtros.</summary>
    public sealed class Handler(
        IPromptTemplateRepository templateRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Governance.Entities.PromptTemplate> templates;

            if (request.Category is not null)
                templates = await templateRepository.GetByCategoryAsync(request.Category, cancellationToken);
            else if (request.Persona is not null)
                templates = await templateRepository.GetByPersonaAsync(request.Persona, cancellationToken);
            else
                templates = await templateRepository.GetAllActiveAsync(cancellationToken);

            if (request.IsActive.HasValue)
                templates = templates.Where(t => t.IsActive == request.IsActive.Value).ToList();

            var items = templates
                .Select(t => new PromptTemplateItem(
                    t.Id.Value, t.Name, t.DisplayName, t.Description,
                    t.Category, t.Content, t.Variables, t.Version,
                    t.IsActive, t.IsOfficial, t.AgentId, t.TargetPersonas,
                    t.ScopeHint, t.Relevance, t.PreferredModelId,
                    t.RecommendedTemperature, t.MaxOutputTokens))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de templates de prompt.</summary>
    public sealed record Response(
        IReadOnlyList<PromptTemplateItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um template de prompt.</summary>
    public sealed record PromptTemplateItem(
        Guid TemplateId, string Name, string DisplayName, string Description,
        string Category, string Content, string Variables, int Version,
        bool IsActive, bool IsOfficial, Guid? AgentId, string TargetPersonas,
        string? ScopeHint, string Relevance, Guid? PreferredModelId,
        decimal? RecommendedTemperature, int? MaxOutputTokens);
}
