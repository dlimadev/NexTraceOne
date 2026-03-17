using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary;

/// <summary>
/// Feature: GetChangesSummary — retorna contadores agregados de mudanças para dashboards de Change Confidence.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangesSummary
{
    /// <summary>Query de resumo agregado de mudanças.</summary>
    public sealed record Query(
        string? TeamName,
        string? Environment,
        DateTimeOffset? From,
        DateTimeOffset? To) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() { }
    }

    /// <summary>Handler que retorna contadores agregados de mudanças.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (total, validated, needsAttention, suspectedRegressions, correlatedWithIncidents) =
                await repository.GetSummaryCountsAsync(
                    request.TeamName, request.Environment,
                    request.From, request.To, cancellationToken);

            return new Response(
                total, validated, needsAttention,
                suspectedRegressions, correlatedWithIncidents);
        }
    }

    /// <summary>Resposta com contadores agregados de mudanças.</summary>
    public sealed record Response(
        int TotalChanges,
        int ValidatedChanges,
        int ChangesNeedingAttention,
        int SuspectedRegressions,
        int ChangesCorrelatedWithIncidents);
}
