using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;

/// <summary>
/// Feature: GetExecutiveDrillDown — drill-down executivo para domínio, equipa ou serviço.
/// Fornece visão detalhada com indicadores, serviços críticos, gaps e recomendações de foco.
/// Consome dados reais do módulo CostIntelligence via contrato público.
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

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
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

            var keyIndicators = new List<KeyIndicatorDto>
            {
                new("Total Monthly Cost", $"{totalCost:N2}", TrendDirection.Stable,
                    $"Aggregated cost from {records.Count} service(s)"),
                new("FinOps Efficiency", efficiency.ToString(), TrendDirection.Stable,
                    $"Based on average service cost of {avgCost:N2}"),
                new("Service Count", records.Count.ToString(), TrendDirection.Stable,
                    "Number of services with cost data"),
                new("Reliability Score", "N/A", TrendDirection.Stable,
                    "Requires integration with reliability module"),
                new("Change Safety", "N/A", TrendDirection.Stable,
                    "Requires integration with change intelligence module"),
                new("Contract Coverage", "N/A", TrendDirection.Stable,
                    "Requires integration with contract governance module")
            };

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
                TopGaps: Array.Empty<GapDto>(),
                RecommendedFocus: Array.Empty<string>(),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

            return Result<Response>.Success(response);
        }

        private async Task<IReadOnlyList<CostRecordSummary>> GetServiceRecordsAsync(string serviceId, CancellationToken cancellationToken)
        {
            var record = await _costModule.GetServiceCostAsync(serviceId, cancellationToken: cancellationToken);
            return record is not null ? new[] { record } : Array.Empty<CostRecordSummary>();
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
        bool IsSimulated = true,
        string DataSource = "demo");

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
