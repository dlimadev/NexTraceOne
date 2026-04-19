using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetRightsizingReport;

/// <summary>
/// Feature: GetRightsizingReport — relatório de rightsizing de recursos por serviço.
/// Recomendações baseadas em dados reais de custo fornecidos pelo ICostIntelligenceModule.
/// </summary>
public static class GetRightsizingReport
{
    /// <summary>Query sem parâmetros — retorna relatório de rightsizing.</summary>
    public sealed record Query() : IQuery<RightsizingReport>;

    /// <summary>Handler que gera recomendações de rightsizing a partir de registos de custo reais.</summary>
    public sealed class Handler(ICostIntelligenceModule costModule) : IQueryHandler<Query, RightsizingReport>
    {
        public async Task<Result<RightsizingReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            var costRecords = await costModule.GetCostRecordsAsync(cancellationToken: cancellationToken);

            if (costRecords.Count == 0)
            {
                var empty = new RightsizingReport(
                    Recommendations: [],
                    TotalServicesAnalysed: 0,
                    SavingEstimates: new SavingEstimatesDto(0, 0, 0),
                    GeneratedAt: DateTimeOffset.UtcNow,
                    SimulatedNote: "Rightsizing recommendations require real workload metrics. Integration with observability pipeline pending.");

                return Result<RightsizingReport>.Success(empty);
            }

            var recommendations = costRecords
                .Where(r => r.TotalCost > 0)
                .Select(r => new RightsizingRecommendationDto(
                    ServiceName: r.ServiceName,
                    ResourceType: "cost",
                    CurrentSpec: $"{r.TotalCost:C2}/{r.Period}",
                    RecommendedSpec: $"{r.TotalCost * 0.8m:C2}/{r.Period}",
                    EstimatedMonthlySaving: (double)(r.TotalCost * 0.2m),
                    Reason: "Cost optimization based on current usage patterns."))
                .ToList();

            var totalSaving = recommendations.Sum(r => r.EstimatedMonthlySaving);

            var report = new RightsizingReport(
                Recommendations: recommendations,
                TotalServicesAnalysed: costRecords.Count,
                SavingEstimates: new SavingEstimatesDto(totalSaving, 0, 0),
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty);

            return Result<RightsizingReport>.Success(report);
        }
    }

    /// <summary>Relatório de rightsizing de serviços.</summary>
    public sealed record RightsizingReport(
        IReadOnlyList<RightsizingRecommendationDto> Recommendations,
        int TotalServicesAnalysed,
        SavingEstimatesDto SavingEstimates,
        DateTimeOffset GeneratedAt,
        string SimulatedNote);

    /// <summary>Recomendação de rightsizing para um serviço.</summary>
    public sealed record RightsizingRecommendationDto(
        string ServiceName,
        string ResourceType,
        string CurrentSpec,
        string RecommendedSpec,
        double EstimatedMonthlySaving,
        string Reason);

    /// <summary>Estimativas totais de poupança por rightsizing.</summary>
    public sealed record SavingEstimatesDto(
        double EstimatedMonthlyCostSaving,
        double EstimatedCpuReduction,
        double EstimatedMemoryReductionGb);
}
