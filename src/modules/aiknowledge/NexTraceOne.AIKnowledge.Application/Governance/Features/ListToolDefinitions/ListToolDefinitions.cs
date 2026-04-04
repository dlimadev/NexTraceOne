using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListToolDefinitions;

/// <summary>
/// Feature: ListToolDefinitions — lista definições de ferramentas com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListToolDefinitions
{
    /// <summary>Query de listagem filtrada de definições de ferramentas de IA.</summary>
    public sealed record Query(
        string? Category,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Validador da query ListToolDefinitions.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        }
    }

    /// <summary>Handler que lista definições de ferramentas com filtros.</summary>
    public sealed class Handler(
        IAiToolDefinitionRepository toolRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Governance.Entities.AiToolDefinition> tools;

            if (request.Category is not null)
                tools = await toolRepository.GetByCategoryAsync(request.Category, cancellationToken);
            else
                tools = await toolRepository.GetAllActiveAsync(cancellationToken);

            if (request.IsActive.HasValue)
                tools = tools.Where(t => t.IsActive == request.IsActive.Value).ToList();

            var items = tools
                .Select(t => new ToolDefinitionItem(
                    t.Id.Value, t.Name, t.DisplayName, t.Description,
                    t.Category, t.ParametersSchema, t.Version,
                    t.IsActive, t.RequiresApproval, t.RiskLevel,
                    t.IsOfficial, t.TimeoutMs))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de definições de ferramentas.</summary>
    public sealed record Response(
        IReadOnlyList<ToolDefinitionItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma definição de ferramenta.</summary>
    public sealed record ToolDefinitionItem(
        Guid ToolId, string Name, string DisplayName, string Description,
        string Category, string ParametersSchema, int Version,
        bool IsActive, bool RequiresApproval, int RiskLevel,
        bool IsOfficial, int TimeoutMs);
}
