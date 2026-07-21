using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRuleset;

/// <summary>
/// Feature: GetRuleset — obtém um ruleset de linting pelo seu identificador (vista de detalhe
/// do gestor de rulesets Spectral). Estrutura VSA: Query + Validator + Handler + Response.
/// </summary>
public static class GetRuleset
{
    /// <summary>Query de obtenção de um ruleset por id.</summary>
    public sealed record Query(Guid RulesetId) : IQuery<RulesetDto>;

    /// <summary>Valida a query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.RulesetId).NotEmpty();
    }

    /// <summary>DTO de detalhe de um ruleset.</summary>
    public sealed record RulesetDto(
        Guid RulesetId,
        string Name,
        string Description,
        string Content,
        string RulesetType,
        bool IsActive,
        DateTimeOffset CreatedAt);

    /// <summary>Handler que obtém o ruleset.</summary>
    public sealed class Handler(IRulesetRepository repository) : IQueryHandler<Query, RulesetDto>
    {
        public async Task<Result<RulesetDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = await repository.GetByIdAsync(RulesetId.From(request.RulesetId), cancellationToken);
            if (ruleset is null)
                return Error.NotFound("Ruleset.NotFound", $"Ruleset {request.RulesetId} not found.");

            return new RulesetDto(
                ruleset.Id.Value,
                ruleset.Name,
                ruleset.Description,
                ruleset.Content,
                ruleset.RulesetType.ToString(),
                ruleset.IsActive,
                ruleset.RulesetCreatedAt);
        }
    }
}
