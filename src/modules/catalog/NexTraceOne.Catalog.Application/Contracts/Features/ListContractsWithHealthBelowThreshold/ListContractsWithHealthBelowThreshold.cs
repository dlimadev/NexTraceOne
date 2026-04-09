using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContractsWithHealthBelowThreshold;

/// <summary>
/// Feature: ListContractsWithHealthBelowThreshold — lista contratos com score de saúde abaixo de um threshold.
/// Usado para dashboards de governança, alertas proativos e filtros de promotion gates.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListContractsWithHealthBelowThreshold
{
    /// <summary>Query para listar contratos com score abaixo do threshold.</summary>
    public sealed record Query(int Threshold = 50) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem por threshold.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Threshold).InclusiveBetween(0, 100);
        }
    }

    /// <summary>
    /// Handler que lista contratos com score de saúde abaixo do threshold especificado.
    /// </summary>
    public sealed class Handler(IContractHealthScoreRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scores = await repository.ListBelowThresholdAsync(
                request.Threshold, cancellationToken);

            var items = scores
                .Select(s => new HealthScoreItem(
                    s.Id.Value,
                    s.ApiAssetId,
                    s.OverallScore,
                    s.BreakingChangeFrequencyScore,
                    s.ConsumerImpactScore,
                    s.ReviewRecencyScore,
                    s.ExampleCoverageScore,
                    s.PolicyComplianceScore,
                    s.DocumentationScore,
                    s.IsDegraded,
                    s.DegradationThreshold,
                    s.CalculatedAt))
                .ToList()
                .AsReadOnly();

            return new Response(items, items.Count, request.Threshold);
        }
    }

    /// <summary>Item de score de saúde na listagem.</summary>
    public sealed record HealthScoreItem(
        Guid HealthScoreId,
        Guid ApiAssetId,
        int OverallScore,
        int BreakingChangeFrequencyScore,
        int ConsumerImpactScore,
        int ReviewRecencyScore,
        int ExampleCoverageScore,
        int PolicyComplianceScore,
        int DocumentationScore,
        bool IsDegraded,
        int DegradationThreshold,
        DateTimeOffset CalculatedAt);

    /// <summary>Resposta da listagem de contratos com score abaixo do threshold.</summary>
    public sealed record Response(
        IReadOnlyList<HealthScoreItem> Items,
        int TotalCount,
        int Threshold);
}
