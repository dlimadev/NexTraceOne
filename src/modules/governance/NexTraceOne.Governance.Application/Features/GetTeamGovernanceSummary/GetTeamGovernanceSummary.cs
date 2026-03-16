using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary;

/// <summary>
/// Feature: GetTeamGovernanceSummary — resumo de maturidade e governança de uma equipa.
/// Agrega cobertura de ownership, contratos, documentação e fiabilidade com dimensões detalhadas.
/// </summary>
public static class GetTeamGovernanceSummary
{
    /// <summary>Query para obter resumo de governança de uma equipa pelo ID.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna resumo de governança e maturidade da equipa.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimensions = new List<GovernanceDimensionDto>
            {
                new("Ownership", "Managed", 88.5m, "Improving"),
                new("Contracts", "Defined", 75.0m, "Stable"),
                new("Documentation", "Developing", 62.3m, "Improving"),
                new("Reliability", "Managed", 94.5m, "Stable"),
                new("Change Safety", "Defined", 81.2m, "Improving"),
                new("Incident Response", "Managed", 90.0m, "Stable")
            };

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: "Commerce",
                OverallMaturity: "Defined",
                OwnershipCoverage: 88.5m,
                ContractCoverage: 75.0m,
                DocumentationCoverage: 62.3m,
                ReliabilityScore: 94.5m,
                OpenRiskCount: 3,
                PolicyViolationCount: 2,
                Dimensions: dimensions);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resumo de governança da equipa.</summary>
    public sealed record Response(
        string TeamId,
        string TeamName,
        string OverallMaturity,
        decimal OwnershipCoverage,
        decimal ContractCoverage,
        decimal DocumentationCoverage,
        decimal ReliabilityScore,
        int OpenRiskCount,
        int PolicyViolationCount,
        IReadOnlyList<GovernanceDimensionDto> Dimensions);

    /// <summary>DTO de dimensão de governança com nível, pontuação e tendência.</summary>
    public sealed record GovernanceDimensionDto(
        string Dimension,
        string Level,
        decimal Score,
        string Trend);
}
