using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ListRulesets;

/// <summary>
/// Feature: ListRulesets — lista rulesets com paginação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListRulesets
{
    /// <summary>Query de listagem de rulesets com paginação.</summary>
    public sealed record Query(int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de rulesets.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista rulesets com paginação.</summary>
    public sealed class Handler(IRulesetRepository repository) : IQueryHandler<Query, Response>
    {
        /// <summary>Processa a query de listagem de rulesets.</summary>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var rulesets = await repository.ListAsync(request.Page, request.PageSize, cancellationToken);

            var dtos = rulesets.Select(r => new RulesetDto(
                r.Id.Value,
                r.Name,
                r.Description,
                r.RulesetType.ToString(),
                r.IsActive,
                r.RulesetCreatedAt)).ToList();

            return new Response(dtos);
        }
    }

    /// <summary>DTO de resumo de Ruleset para listagem.</summary>
    public sealed record RulesetDto(
        Guid RulesetId,
        string Name,
        string Description,
        string RulesetType,
        bool IsActive,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de rulesets.</summary>
    public sealed record Response(IReadOnlyList<RulesetDto> Rulesets);
}
