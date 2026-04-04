using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardrails;

/// <summary>
/// Feature: ListGuardrails — lista guardrails com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListGuardrails
{
    /// <summary>Query de listagem filtrada de guardrails de IA.</summary>
    public sealed record Query(
        string? Category,
        string? GuardType,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Validador da query ListGuardrails.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
            RuleFor(x => x.GuardType).MaximumLength(100).When(x => x.GuardType is not null);
        }
    }

    /// <summary>Handler que lista guardrails com filtros opcionais.</summary>
    public sealed class Handler(
        IAiGuardrailRepository guardrailRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Governance.Entities.AiGuardrail> guardrails;

            if (request.Category is not null)
                guardrails = await guardrailRepository.GetByCategoryAsync(request.Category, cancellationToken);
            else if (request.GuardType is not null)
                guardrails = await guardrailRepository.GetByGuardTypeAsync(request.GuardType, cancellationToken);
            else
                guardrails = await guardrailRepository.GetAllActiveAsync(cancellationToken);

            if (request.IsActive.HasValue)
                guardrails = guardrails.Where(g => g.IsActive == request.IsActive.Value).ToList();

            var items = guardrails
                .Select(g => new GuardrailItem(
                    g.Id.Value, g.Name, g.DisplayName, g.Description,
                    g.Category, g.GuardType, g.Pattern, g.PatternType,
                    g.Severity, g.Action, g.UserMessage,
                    g.IsActive, g.IsOfficial, g.AgentId, g.ModelId, g.Priority))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de guardrails.</summary>
    public sealed record Response(
        IReadOnlyList<GuardrailItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um guardrail.</summary>
    public sealed record GuardrailItem(
        Guid GuardrailId, string Name, string DisplayName, string Description,
        string Category, string GuardType, string Pattern, string PatternType,
        string Severity, string Action, string? UserMessage,
        bool IsActive, bool IsOfficial, Guid? AgentId, Guid? ModelId, int Priority);
}
