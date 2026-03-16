using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary;

/// <summary>
/// Feature: GetDomainGovernanceSummary — resumo de maturidade e governança de um domínio de negócio.
/// Agrega cobertura de ownership, contratos, documentação e fiabilidade com dimensões detalhadas.
/// </summary>
public static class GetDomainGovernanceSummary
{
    /// <summary>Query para obter resumo de governança de um domínio pelo ID.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna resumo de governança e maturidade do domínio.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimensions = new List<GovernanceDimensionDto>
            {
                new("Ownership", "Managed", 91.0m, "Stable"),
                new("Contracts", "Defined", 78.5m, "Improving"),
                new("Documentation", "Developing", 65.0m, "Improving"),
                new("Reliability", "Defined", 92.3m, "Stable"),
                new("Change Safety", "Defined", 79.8m, "Improving"),
                new("Incident Response", "Managed", 87.5m, "Stable")
            };

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: "Commerce",
                OverallMaturity: "Defined",
                OwnershipCoverage: 91.0m,
                ContractCoverage: 78.5m,
                DocumentationCoverage: 65.0m,
                ReliabilityScore: 92.3m,
                OpenRiskCount: 5,
                PolicyViolationCount: 4,
                Dimensions: dimensions);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resumo de governança do domínio.</summary>
    public sealed record Response(
        string DomainId,
        string DomainName,
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
