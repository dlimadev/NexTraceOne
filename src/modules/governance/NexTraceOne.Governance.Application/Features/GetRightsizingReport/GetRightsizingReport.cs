using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetRightsizingReport;

/// <summary>
/// Feature: GetRightsizingReport — relatório de rightsizing de recursos por serviço.
/// Recomendações reais requerem integração com pipeline de observabilidade. SimulatedNote explica.
/// </summary>
public static class GetRightsizingReport
{
    /// <summary>Query sem parâmetros — retorna relatório de rightsizing.</summary>
    public sealed record Query() : IQuery<RightsizingReport>;

    /// <summary>Handler que retorna relatório de rightsizing (integração real pendente).</summary>
    public sealed class Handler : IQueryHandler<Query, RightsizingReport>
    {
        public Task<Result<RightsizingReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new RightsizingReport(
                Recommendations: [],
                TotalServicesAnalysed: 0,
                SavingEstimates: new SavingEstimatesDto(0, 0, 0),
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "Rightsizing recommendations require real workload metrics. Integration with observability pipeline pending.");

            return Task.FromResult(Result<RightsizingReport>.Success(response));
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
