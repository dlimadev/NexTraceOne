using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetFindings;

/// <summary>
/// Feature: GetRulesetFindings -- retorna os findings de linting de uma release.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRulesetFindings
{
    /// <summary>Query de consulta de findings de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de findings.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna os findings de linting de uma release.</summary>
    public sealed class Handler(ILintResultRepository repository) : IQueryHandler<Query, Response>
    {
        /// <summary>Processa a query de findings.</summary>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var lintResult = await repository.GetByReleaseIdAsync(request.ReleaseId, cancellationToken);
            if (lintResult is null)
                return RulesetGovernanceErrors.LintResultNotFound(request.ReleaseId.ToString());

            var findingDtos = lintResult.Findings.Select(f => new FindingDto(
                f.Rule,
                f.Severity.ToString(),
                f.Message,
                f.Path)).ToList();

            return new Response(
                lintResult.Id.Value,
                lintResult.ReleaseId,
                lintResult.Score,
                lintResult.TotalFindings,
                findingDtos,
                lintResult.ExecutedAt);
        }
    }

    /// <summary>DTO de um finding individual.</summary>
    public sealed record FindingDto(string Rule, string Severity, string Message, string Path);

    /// <summary>Resposta com os findings da release.</summary>
    public sealed record Response(
        Guid LintResultId,
        Guid ReleaseId,
        decimal Score,
        int TotalFindings,
        IReadOnlyList<FindingDto> Findings,
        DateTimeOffset ExecutedAt);
}
