using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetBenchmarking;

/// <summary>
/// Feature: GetBenchmarking — comparação contextualizada entre equipas ou domínios.
/// Cada comparação inclui contexto para garantir fairness na interpretação dos resultados.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// Scores não disponíveis são retornados como null em vez de placeholder fixo.
/// </summary>
public static class GetBenchmarking
{
    /// <summary>Query de benchmarking. Dimensão: teams ou domains.</summary>
    public sealed record Query(
        string Dimension) : IQuery<Response>;

    /// <summary>Handler que computa comparações de benchmarking contextualizadas.</summary>
    /// <summary>Valida os parâmetros da query de benchmarking.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Dimension).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            var isDomainDimension = request.Dimension.Equals("domains", StringComparison.OrdinalIgnoreCase);

            var grouped = isDomainDimension
                ? records.GroupBy(r => (Id: r.Domain ?? string.Empty, Name: r.Domain ?? string.Empty))
                : records.GroupBy(r => (Id: r.Team ?? string.Empty, Name: r.Team ?? string.Empty));

            var comparisons = grouped
                .Select(g =>
                {
                    var avgCost = g.Average(r => r.TotalCost);
                    var efficiency = ComputeEfficiency(avgCost);
                    var serviceCount = g.Select(r => r.ServiceId).Distinct().Count();

                    var strengths = new List<string>();
                    var gaps = new List<string>();

                    if (efficiency is CostEfficiency.Efficient or CostEfficiency.Acceptable)
                        strengths.Add("Cost efficiency within acceptable range");
                    else
                        gaps.Add("Cost efficiency needs improvement");

                    // Threshold: average cost per service below 5000 indicates good cost distribution
                    const decimal costPerServiceThreshold = 5000m;
                    if (serviceCount > 0 && avgCost / serviceCount < costPerServiceThreshold)
                        strengths.Add("Low average cost per service");

                    var currency = g.Select(r => r.Currency).FirstOrDefault() ?? "EUR";
                    var context = $"Based on {g.Count()} cost records across {serviceCount} service(s). " +
                                  $"Average cost: {avgCost:N2} {currency}.";

                    return new BenchmarkComparisonDto(
                        GroupId: g.Key.Id,
                        GroupName: g.Key.Name,
                        ServiceCount: serviceCount,
                        Criticality: null,
                        ReliabilityScore: null,
                        ReliabilityTrend: null,
                        ChangeSafetyScore: null,
                        IncidentRecurrenceRate: null,
                        MaturityScore: null,
                        RiskScore: null,
                        FinopsEfficiency: efficiency,
                        Strengths: strengths,
                        Gaps: gaps,
                        Context: context);
                })
                .ToList();

            var response = new Response(
                Dimension: request.Dimension,
                Comparisons: comparisons,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

            return Result<Response>.Success(response);
        }

        private static CostEfficiency ComputeEfficiency(decimal cost) => cost switch
        {
            > 15000m => CostEfficiency.Wasteful,
            > 10000m => CostEfficiency.Inefficient,
            > 5000m => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };
    }

    /// <summary>Resposta de benchmarking. IsSimulated=true indica dados demonstrativos.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<BenchmarkComparisonDto> Comparisons,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>
    /// Comparação de benchmarking para um grupo com forças, gaps e contexto explicativo.
    /// Scores retornados como null quando dados insuficientes para cálculo real.
    /// </summary>
    public sealed record BenchmarkComparisonDto(
        string GroupId,
        string GroupName,
        int ServiceCount,
        string? Criticality,
        decimal? ReliabilityScore,
        TrendDirection? ReliabilityTrend,
        decimal? ChangeSafetyScore,
        decimal? IncidentRecurrenceRate,
        decimal? MaturityScore,
        decimal? RiskScore,
        CostEfficiency FinopsEfficiency,
        IReadOnlyList<string> Strengths,
        IReadOnlyList<string> Gaps,
        string Context);
}
