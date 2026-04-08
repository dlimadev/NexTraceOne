using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthScore;

/// <summary>
/// Feature: GetContractHealthScore — obtém o score de saúde mais recente de um contrato.
/// Consulta o score persistido para exibição rápida no catálogo, badges e dashboards.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractHealthScore
{
    /// <summary>Query para obter o score de saúde de um contrato.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de score de saúde.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém o score de saúde persistido de um contrato.
    /// </summary>
    public sealed class Handler(IContractHealthScoreRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var healthScore = await repository.GetByApiAssetIdAsync(
                request.ApiAssetId, cancellationToken);

            if (healthScore is null)
                return ContractsErrors.ContractHealthScoreNotFound(request.ApiAssetId.ToString());

            return new Response(
                healthScore.Id.Value,
                healthScore.ApiAssetId,
                healthScore.OverallScore,
                healthScore.BreakingChangeFrequencyScore,
                healthScore.ConsumerImpactScore,
                healthScore.ReviewRecencyScore,
                healthScore.ExampleCoverageScore,
                healthScore.PolicyComplianceScore,
                healthScore.DocumentationScore,
                healthScore.IsDegraded,
                healthScore.DegradationThreshold,
                healthScore.CalculatedAt);
        }
    }

    /// <summary>Resposta do score de saúde de um contrato.</summary>
    public sealed record Response(
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
}
