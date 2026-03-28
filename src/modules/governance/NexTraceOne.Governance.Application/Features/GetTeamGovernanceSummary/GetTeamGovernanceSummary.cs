using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

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
    public sealed class Handler(
        ITeamRepository teamRepository,
        ICatalogGraphModule catalogGraph) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.TeamId, out var teamGuid))
                return Error.Validation("INVALID_TEAM_ID", "Team ID '{0}' is not a valid GUID.", request.TeamId);

            var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
            if (team is null)
                return Error.NotFound("TEAM_NOT_FOUND", "Team '{0}' not found.", request.TeamId);

            var serviceCount = await catalogGraph.CountServicesByTeamAsync(team.Name, cancellationToken);
            var contracts = await catalogGraph.ListContractsByTeamAsync(team.Name, cancellationToken) ?? [];
            var dependencies = await catalogGraph.ListCrossTeamDependenciesAsync(team.Name, cancellationToken) ?? [];

            var contractCoverage = serviceCount == 0
                ? 0m
                : Math.Min(100m, contracts.Count * 100m / serviceCount);

            var documentationSignals = contracts.Count(c => !string.IsNullOrWhiteSpace(c.Version));
            var documentationCoverage = contracts.Count == 0
                ? 0m
                : Math.Min(100m, documentationSignals * 100m / contracts.Count);

            var reliabilityScore = serviceCount == 0
                ? 0m
                : Math.Max(0m, 100m - (dependencies.Count * 5m));

            var ownershipCoverage = 100m;
            var openRiskCount = dependencies.Count;
            var policyViolationCount = Math.Max(0, serviceCount - contracts.Count);

            var dimensions = new List<GovernanceDimensionDto>
            {
                new("Ownership", ToLevel(ownershipCoverage), ownershipCoverage, "Stable"),
                new("Contracts", ToLevel(contractCoverage), contractCoverage, GetTrend(contractCoverage)),
                new("Documentation", ToLevel(documentationCoverage), documentationCoverage, GetTrend(documentationCoverage)),
                new("Reliability", ToLevel(reliabilityScore), reliabilityScore, GetTrend(reliabilityScore)),
                new("Change Safety", ToLevel(100m - openRiskCount * 10m), Math.Max(0m, 100m - openRiskCount * 10m), "Stable"),
                new("Incident Response", ToLevel(reliabilityScore), reliabilityScore, "Stable")
            };

            var overallScore = dimensions.Average(d => d.Score);

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: team.DisplayName,
                OverallMaturity: ToLevel(overallScore),
                OwnershipCoverage: ownershipCoverage,
                ContractCoverage: contractCoverage,
                DocumentationCoverage: documentationCoverage,
                ReliabilityScore: reliabilityScore,
                OpenRiskCount: openRiskCount,
                PolicyViolationCount: policyViolationCount,
                Dimensions: dimensions,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }

        private static string ToLevel(decimal score) =>
            score switch
            {
                >= 85m => "Managed",
                >= 70m => "Defined",
                >= 40m => "Developing",
                _ => "Initial"
            };

        private static string GetTrend(decimal score) =>
            score >= 70m ? "Stable" : "Improving";
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
        IReadOnlyList<GovernanceDimensionDto> Dimensions,
        bool IsSimulated = false);

    /// <summary>DTO de dimensão de governança com nível, pontuação e tendência.</summary>
    public sealed record GovernanceDimensionDto(
        string Dimension,
        string Level,
        decimal Score,
        string Trend);
}
