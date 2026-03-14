using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Errors;

namespace NexTraceOne.RulesetGovernance.Application.Features.GetRulesetScore;

/// <summary>
/// Feature: GetRulesetScore -- retorna o score de conformidade de uma release.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRulesetScore
{
    /// <summary>Query de consulta do score de conformidade de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de score.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o score de conformidade de uma release.</summary>
    public sealed class Handler(ILintResultRepository repository) : IQueryHandler<Query, Response>
    {
        /// <summary>Processa a query de score.</summary>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var lintResult = await repository.GetByReleaseIdAsync(request.ReleaseId, cancellationToken);
            if (lintResult is null)
                return RulesetGovernanceErrors.LintResultNotFound(request.ReleaseId.ToString());

            return new Response(
                lintResult.ReleaseId,
                lintResult.Score,
                lintResult.TotalFindings,
                lintResult.ExecutedAt);
        }
    }

    /// <summary>Resposta com o score de conformidade da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        decimal Score,
        int TotalFindings,
        DateTimeOffset ExecutedAt);
}
