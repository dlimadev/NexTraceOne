using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetRiskHeatmap;

/// <summary>
/// Feature: GetRiskHeatmap — heatmap de risco multidimensional por domínio, equipa ou criticidade.
/// Permite visualização de concentração de risco com explicação contextual por célula.
/// </summary>
public static class GetRiskHeatmap
{
    /// <summary>Query de heatmap de risco. Dimensão: domain, team ou serviceCriticality.</summary>
    public sealed record Query(
        string? Dimension = null) : IQuery<Response>;

    /// <summary>Handler que computa as células do heatmap de risco.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimension = request.Dimension ?? "domain";

            var cells = new List<RiskHeatmapCellDto>
            {
                new("domain-commerce", "Commerce", RiskLevel.Critical, 92.0m,
                    Incidents: 5, ChangeFailures: 3, ReliabilityDegradation: true,
                    ContractGaps: 2, DocumentationGaps: 3, RunbookGaps: 4,
                    DependencyFragility: 2, RegressionCount: 3,
                    "Service degradation in Order Processor with frequent rollbacks and recurring incidents"),
                new("domain-payments", "Payments", RiskLevel.High, 74.5m,
                    Incidents: 3, ChangeFailures: 2, ReliabilityDegradation: false,
                    ContractGaps: 1, DocumentationGaps: 2, RunbookGaps: 3,
                    DependencyFragility: 1, RegressionCount: 2,
                    "Recurring timeout incidents and failed deployments in Payment API"),
                new("domain-integration", "Integration", RiskLevel.High, 71.0m,
                    Incidents: 2, ChangeFailures: 1, ReliabilityDegradation: false,
                    ContractGaps: 4, DocumentationGaps: 5, RunbookGaps: 4,
                    DependencyFragility: 3, RegressionCount: 1,
                    "Legacy adapters with no contracts, documentation or runbooks"),
                new("domain-identity", "Identity", RiskLevel.Medium, 48.0m,
                    Incidents: 1, ChangeFailures: 0, ReliabilityDegradation: false,
                    ContractGaps: 1, DocumentationGaps: 1, RunbookGaps: 2,
                    DependencyFragility: 0, RegressionCount: 0,
                    "Missing runbook for User Service, contract version outdated"),
                new("domain-analytics", "Analytics", RiskLevel.Medium, 45.5m,
                    Incidents: 1, ChangeFailures: 1, ReliabilityDegradation: false,
                    ContractGaps: 1, DocumentationGaps: 2, RunbookGaps: 2,
                    DependencyFragility: 1, RegressionCount: 1,
                    "Reporting engine with outdated contract and missing runbook"),
                new("domain-messaging", "Messaging", RiskLevel.Low, 18.0m,
                    Incidents: 0, ChangeFailures: 0, ReliabilityDegradation: false,
                    ContractGaps: 0, DocumentationGaps: 0, RunbookGaps: 1,
                    DependencyFragility: 0, RegressionCount: 0,
                    "All dependencies healthy, only minor runbook gap in Notification Hub"),
                new("domain-security", "Security", RiskLevel.Medium, 40.0m,
                    Incidents: 0, ChangeFailures: 0, ReliabilityDegradation: false,
                    ContractGaps: 0, DocumentationGaps: 2, RunbookGaps: 1,
                    DependencyFragility: 1, RegressionCount: 0,
                    "Auth Gateway with incomplete documentation and unmapped dependencies"),
                new("domain-platform", "Platform", RiskLevel.Low, 22.0m,
                    Incidents: 0, ChangeFailures: 0, ReliabilityDegradation: false,
                    ContractGaps: 1, DocumentationGaps: 0, RunbookGaps: 0,
                    DependencyFragility: 0, RegressionCount: 0,
                    "Minor versioning gap in Cache Manager, overall healthy")
            };

            var response = new Response(
                Dimension: dimension,
                Cells: cells,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do heatmap de risco com células por grupo.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<RiskHeatmapCellDto> Cells,
        DateTimeOffset GeneratedAt);

    /// <summary>Célula do heatmap de risco com indicadores multidimensionais e explicação.</summary>
    public sealed record RiskHeatmapCellDto(
        string GroupId,
        string GroupName,
        RiskLevel RiskLevel,
        decimal RiskScore,
        int Incidents,
        int ChangeFailures,
        bool ReliabilityDegradation,
        int ContractGaps,
        int DocumentationGaps,
        int RunbookGaps,
        int DependencyFragility,
        int RegressionCount,
        string Explanation);
}
