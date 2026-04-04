using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;

/// <summary>
/// Feature: GetExecutiveDrillDown — drill-down executivo para domínio, equipa ou serviço.
/// Fornece visão detalhada com indicadores, serviços críticos, gaps e recomendações de foco.
/// Consome dados reais dos módulos CostIntelligence, Reliability, ChangeIntelligence e Contracts via contratos públicos.
/// </summary>
public static class GetExecutiveDrillDown
{
    /// <summary>Query de drill-down executivo. Tipo de entidade: domain, team ou service.</summary>
    public sealed record Query(
        string EntityType,
        string EntityId) : IQuery<Response>;

    /// <summary>Handler que computa drill-down executivo detalhado para uma entidade.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;
        private readonly IReliabilityModule _reliabilityModule;
        private readonly IContractsModule _contractsModule;

        public Handler(
            ICostIntelligenceModule costModule,
            IReliabilityModule reliabilityModule,
            IContractsModule contractsModule)
        {
            _costModule = costModule;
            _reliabilityModule = reliabilityModule;
            _contractsModule = contractsModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = request.EntityType.ToLowerInvariant() switch
            {
                "domain" => await _costModule.GetCostsByDomainAsync(request.EntityId, cancellationToken: cancellationToken),
                "team" => await _costModule.GetCostsByTeamAsync(request.EntityId, cancellationToken: cancellationToken),
                "service" => await GetServiceRecordsAsync(request.EntityId, cancellationToken),
                _ => await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken)
            };

            var totalCost = records.Sum(r => r.TotalCost);
            var avgCost = records.Count > 0 ? records.Average(r => r.TotalCost) : 0m;
            var efficiency = ComputeEfficiency(avgCost);

            // Compute reliability score from cross-module integration
            var reliabilityScore = await ComputeReliabilityScoreAsync(records, cancellationToken);
            var contractCoverage = await ComputeContractCoverageAsync(records, cancellationToken);

            var keyIndicators = new List<KeyIndicatorDto>
            {
                new("Total Monthly Cost", $"{totalCost:N2}", TrendDirection.Stable,
                    $"Aggregated cost from {records.Count} service(s)"),
                new("FinOps Efficiency", efficiency.ToString(), TrendDirection.Stable,
                    $"Based on average service cost of {avgCost:N2}"),
                new("Service Count", records.Count.ToString(), TrendDirection.Stable,
                    "Number of services with cost data"),
                new("Reliability Score", reliabilityScore ?? "No data", TrendDirection.Stable,
                    reliabilityScore is not null
                        ? "Aggregated from reliability module SLO data"
                        : "No reliability data available for this entity"),
                new("Contract Coverage", contractCoverage, TrendDirection.Stable,
                    "Percentage of services with contract versions")
            };

            var gaps = new List<GapDto>();
            if (reliabilityScore is null && records.Count > 0)
            {
                gaps.Add(new GapDto("Reliability", RiskLevel.Medium,
                    "No SLO data configured for services in this entity",
                    "Configure SLO definitions for critical services to enable reliability tracking"));
            }
            if (contractCoverage == "0%")
            {
                gaps.Add(new GapDto("Contract Governance", RiskLevel.High,
                    "No contract versions found for services in this entity",
                    "Register API contracts to ensure governance coverage and compatibility tracking"));
            }

            var recommendedFocus = new List<string>();
            if (efficiency >= CostEfficiency.Inefficient)
                recommendedFocus.Add("Review high-cost services for optimization opportunities");
            if (reliabilityScore is null)
                recommendedFocus.Add("Configure SLO definitions for reliability monitoring");
            if (contractCoverage == "0%")
                recommendedFocus.Add("Register API contracts for governance coverage");

            var criticalServices = records
                .Where(r => ComputeEfficiency(r.TotalCost) is CostEfficiency.Wasteful)
                .OrderByDescending(r => r.TotalCost)
                .Take(5)
                .Select(r => new CriticalServiceDto(
                    r.ServiceId,
                    r.ServiceName,
                    RiskLevel.High,
                    $"High cost: {r.TotalCost:N2} {r.Currency}"))
                .ToList();

            var response = new Response(
                EntityType: request.EntityType,
                EntityId: request.EntityId,
                EntityName: request.EntityId,
                RiskLevel: efficiency >= CostEfficiency.Inefficient ? RiskLevel.High : RiskLevel.Medium,
                MaturityLevel: MaturityLevel.Developing,
                KeyIndicators: keyIndicators,
                CriticalServices: criticalServices,
                TopGaps: gaps,
                RecommendedFocus: recommendedFocus,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence+reliability+contracts");

            return Result<Response>.Success(response);
        }

        private async Task<IReadOnlyList<CostRecordSummary>> GetServiceRecordsAsync(string serviceId, CancellationToken cancellationToken)
        {
            var record = await _costModule.GetServiceCostAsync(serviceId, cancellationToken: cancellationToken);
            return record is not null ? new[] { record } : Array.Empty<CostRecordSummary>();
        }

        private async Task<string?> ComputeReliabilityScoreAsync(
            IReadOnlyList<CostRecordSummary> records, CancellationToken cancellationToken)
        {
            if (records.Count == 0) return null;

            var statuses = new List<string>();
            foreach (var record in records.Take(20))
            {
                try
                {
                    var status = await _reliabilityModule.GetCurrentReliabilityStatusAsync(
                        record.ServiceName, "production", cancellationToken);
                    if (status is not null)
                        statuses.Add(status);
                }
                catch
                {
                    // Resilient — skip services without reliability data
                }
            }

            if (statuses.Count == 0) return null;

            var healthyCount = statuses.Count(s => s.Equals("Healthy", StringComparison.OrdinalIgnoreCase));
            var percentage = (decimal)healthyCount / statuses.Count * 100;
            return $"{percentage:F0}% healthy ({healthyCount}/{statuses.Count})";
        }

        private async Task<string> ComputeContractCoverageAsync(
            IReadOnlyList<CostRecordSummary> records, CancellationToken cancellationToken)
        {
            if (records.Count == 0) return "N/A";

            var withContract = 0;
            foreach (var record in records.Take(20))
            {
                try
                {
                    // Use ServiceId as potential ApiAssetId (GUID format expected)
                    if (Guid.TryParse(record.ServiceId, out var assetId))
                    {
                        var hasContract = await _contractsModule.HasContractVersionAsync(assetId, cancellationToken);
                        if (hasContract) withContract++;
                    }
                }
                catch
                {
                    // Resilient — skip services that fail contract lookup
                }
            }

            var percentage = records.Count > 0 ? (decimal)withContract / Math.Min(records.Count, 20) * 100 : 0;
            return $"{percentage:F0}%";
        }

        private static CostEfficiency ComputeEfficiency(decimal cost) => cost switch
        {
            > 15000m => CostEfficiency.Wasteful,
            > 10000m => CostEfficiency.Inefficient,
            > 5000m => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };
    }

    /// <summary>Resposta de drill-down executivo com indicadores, serviços críticos, gaps e foco recomendado.</summary>
    public sealed record Response(
        string EntityType,
        string EntityId,
        string EntityName,
        RiskLevel RiskLevel,
        MaturityLevel MaturityLevel,
        IReadOnlyList<KeyIndicatorDto> KeyIndicators,
        IReadOnlyList<CriticalServiceDto> CriticalServices,
        IReadOnlyList<GapDto> TopGaps,
        IReadOnlyList<string> RecommendedFocus,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string DataSource = "cost-intelligence");

    /// <summary>Indicador-chave com valor, tendência e explicação contextual.</summary>
    public sealed record KeyIndicatorDto(
        string Name,
        string Value,
        TrendDirection Trend,
        string Explanation);

    /// <summary>Serviço crítico com nível de risco e problema principal.</summary>
    public sealed record CriticalServiceDto(
        string ServiceId,
        string ServiceName,
        RiskLevel RiskLevel,
        string MainIssue);

    /// <summary>Gap identificado com severidade, descrição e recomendação.</summary>
    public sealed record GapDto(
        string Area,
        RiskLevel Severity,
        string Description,
        string Recommendation);
}
