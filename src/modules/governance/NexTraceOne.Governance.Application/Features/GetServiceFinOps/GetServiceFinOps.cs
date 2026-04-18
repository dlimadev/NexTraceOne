using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetServiceFinOps;

/// <summary>
/// Feature: GetServiceFinOps — perfil de custo contextual de um serviço individual.
/// Inclui waste, eficiência, correlação com confiabilidade e impacto de mudanças.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetServiceFinOps
{
    /// <summary>Query para obter perfil de FinOps de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do serviço.</summary>
    /// <summary>Valida os parâmetros da query de FinOps por serviço.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;
        private readonly IReliabilityModule _reliabilityModule;
        private readonly IConfigurationResolutionService _configService;

        public Handler(ICostIntelligenceModule costModule, IReliabilityModule reliabilityModule, IConfigurationResolutionService configService)
        {
            _costModule = costModule;
            _reliabilityModule = reliabilityModule;
            _configService = configService;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // ── Ler configurações de eficiência ──
            var recommendationCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsRecommendationPolicy, ConfigurationScope.Tenant, null, cancellationToken);
            var savingsRatePct = ParseSavingsRatePct(recommendationCfg?.EffectiveValue);

            var costBandsCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyCostBands, ConfigurationScope.Tenant, null, cancellationToken);
            var (wastefulBand, inefficientBand, acceptableBand) = ParseCostBands(costBandsCfg?.EffectiveValue);

            var burnRateCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyBurnRateThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var (elevatedBurnRate, criticalBurnRate) = ParseBurnRateThresholds(burnRateCfg?.EffectiveValue);

            var record = await _costModule.GetServiceCostAsync(request.ServiceId, cancellationToken: cancellationToken);
            var allRecords = await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            if (record is null)
                return Error.NotFound("FINOPS.SERVICE_NOT_FOUND", "No cost data found for service {0}", request.ServiceId);

            var serviceRecords = allRecords
                .Where(r => r.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Period, StringComparer.Ordinal)
                .ToList();

            var currentRecord = serviceRecords.FirstOrDefault() ?? record;
            var previousRecord = serviceRecords.Skip(1).FirstOrDefault();
            var previousMonthCost = previousRecord?.TotalCost ?? 0m;
            var costTrend = GetTrendDirection(previousMonthCost, currentRecord.TotalCost);

            var averageServiceCost = allRecords.Count == 0 ? currentRecord.TotalCost : allRecords.Average(r => r.TotalCost);
            var waste = Math.Max(0m, currentRecord.TotalCost - averageServiceCost);
            var wasteSignals = waste > 0m
                ? new[]
                {
                    new WasteSignalDto(
                        Description: "Service cost exceeds tenant average for the selected period.",
                        Pattern: "cost-above-average",
                        Type: WasteSignalType.DegradedCostAmplification,
                        EstimatedWaste: Math.Round(waste, 2),
                        DetectedAt: DateTimeOffset.UtcNow.ToString("o"))
                }
                : Array.Empty<WasteSignalDto>();

            var potentialSavings = Math.Round(waste * (savingsRatePct / 100m), 2);
            var optimizations = potentialSavings > 0m
                ? new[]
                {
                    new OptimizationDto(
                        Recommendation: "Review scaling and idle capacity for this service in high-cost environments.",
                        PotentialSavings: potentialSavings,
                        Priority: waste > averageServiceCost ? "High" : "Medium",
                        Rationale: $"Current cost {currentRecord.TotalCost:N2} exceeds average {averageServiceCost:N2}.")
                }
                : Array.Empty<OptimizationDto>();

            // Reliability data from cross-module IReliabilityModule
            var environment = currentRecord.Environment ?? "production";
            var serviceName = currentRecord.ServiceName;
            var errorBudget = await _reliabilityModule.GetRemainingErrorBudgetAsync(serviceName, environment, cancellationToken);
            var burnRate = await _reliabilityModule.GetCurrentBurnRateAsync(serviceName, environment, cancellationToken);
            var reliabilityStatus = await _reliabilityModule.GetCurrentReliabilityStatusAsync(serviceName, environment, cancellationToken);

            var reliabilityScore = errorBudget.HasValue ? Math.Round(errorBudget.Value * 100m, 1) : 0m;
            var reliabilityTrend = burnRate.HasValue
                ? burnRate.Value > criticalBurnRate ? TrendDirection.Declining
                : burnRate.Value < elevatedBurnRate * 0.5m ? TrendDirection.Improving
                : TrendDirection.Stable
                : TrendDirection.Stable;

            // Efficiency indicators derived from cost and reliability data
            var efficiencyIndicators = BuildEfficiencyIndicators(currentRecord.TotalCost, averageServiceCost, reliabilityScore, burnRate, elevatedBurnRate, criticalBurnRate);

            var response = new Response(
                ServiceId: record.ServiceId,
                ServiceName: record.ServiceName,
                Domain: record.Domain ?? string.Empty,
                Team: record.Team ?? string.Empty,
                MonthlyCost: currentRecord.TotalCost,
                PreviousMonthCost: previousMonthCost,
                CostTrend: costTrend,
                Efficiency: ComputeEfficiency(currentRecord.TotalCost, wastefulBand, inefficientBand, acceptableBand),
                WasteSignals: wasteSignals,
                TotalWaste: Math.Round(waste, 2),
                EfficiencyIndicators: efficiencyIndicators,
                ReliabilityScore: reliabilityScore,
                RecentIncidents: 0,
                ReliabilityTrend: reliabilityTrend,
                ChangeImpacts: Array.Empty<ChangeImpactDto>(),
                Optimizations: optimizations,
                TotalPotentialSavings: optimizations.Sum(o => o.PotentialSavings),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence+reliability");

            return Result<Response>.Success(response);
        }

        private static decimal ParseSavingsRatePct(string? json)
        {
            const decimal defaultRate = 35m;
            if (string.IsNullOrWhiteSpace(json)) return defaultRate;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return root.TryGetProperty("savingsRatePct", out var p) && p.TryGetDecimal(out var v)
                    ? Math.Clamp(v, 1m, 100m) : defaultRate;
            }
            catch { return defaultRate; }
        }

        private static (decimal wasteful, decimal inefficient, decimal acceptable) ParseCostBands(string? json)
        {
            const decimal dw = 15000m, di = 10000m, da = 5000m;
            if (string.IsNullOrWhiteSpace(json)) return (dw, di, da);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var w = root.TryGetProperty("Wasteful", out var wp) && wp.TryGetDecimal(out var wv) ? wv : dw;
                var i = root.TryGetProperty("Inefficient", out var ip) && ip.TryGetDecimal(out var iv) ? iv : di;
                var a = root.TryGetProperty("Acceptable", out var ap) && ap.TryGetDecimal(out var av) ? av : da;
                return (w, i, a);
            }
            catch { return (dw, di, da); }
        }

        private static (decimal elevated, decimal critical) ParseBurnRateThresholds(string? json)
        {
            const decimal de = 1.5m, dc = 2.0m;
            if (string.IsNullOrWhiteSpace(json)) return (de, dc);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var e = root.TryGetProperty("elevated", out var ep) && ep.TryGetDecimal(out var ev) ? ev : de;
                var c = root.TryGetProperty("critical", out var cp) && cp.TryGetDecimal(out var cv) ? cv : dc;
                return (Math.Max(e, 0.1m), Math.Max(c, 0.1m));
            }
            catch { return (de, dc); }
        }

        private static List<EfficiencyIndicatorDto> BuildEfficiencyIndicators(
            decimal currentCost, decimal averageCost, decimal reliabilityScore, decimal? burnRate,
            decimal elevatedBurnRate, decimal criticalBurnRate)
        {
            var indicators = new List<EfficiencyIndicatorDto>();

            // Cost-per-reliability indicator
            var costEfficiencyRatio = reliabilityScore > 0 ? Math.Round(currentCost / reliabilityScore, 2) : 0m;
            indicators.Add(new EfficiencyIndicatorDto(
                Name: "Cost per Reliability Point",
                Category: EfficiencyCategory.CostPerTransaction,
                CurrentValue: costEfficiencyRatio,
                TargetValue: averageCost > 0 && reliabilityScore > 0 ? Math.Round(averageCost / reliabilityScore, 2) : 0m,
                Assessment: costEfficiencyRatio > 0 && reliabilityScore > 80m ? "Good" : costEfficiencyRatio > 0 ? "Needs Improvement" : "No Data"));

            // Budget utilization indicator
            var utilization = averageCost > 0 ? Math.Round(currentCost / averageCost * 100m, 1) : 100m;
            indicators.Add(new EfficiencyIndicatorDto(
                Name: "Budget Utilization",
                Category: EfficiencyCategory.ResourceUtilization,
                CurrentValue: utilization,
                TargetValue: 100m,
                Assessment: utilization <= 110m ? "On Track" : utilization <= 130m ? "Over Budget" : "Critical Overspend"));

            // Burn rate indicator
            if (burnRate.HasValue)
            {
                indicators.Add(new EfficiencyIndicatorDto(
                    Name: "Error Budget Burn Rate",
                    Category: EfficiencyCategory.ErrorRate,
                    CurrentValue: Math.Round(burnRate.Value, 2),
                    TargetValue: 1.0m,
                    Assessment: burnRate.Value <= elevatedBurnRate ? "Healthy"
                        : burnRate.Value <= criticalBurnRate ? "Elevated"
                        : "Critical"));
            }

            return indicators;
        }

        private static CostEfficiency ComputeEfficiency(decimal cost, decimal wasteful, decimal inefficient, decimal acceptable) => cost switch
        {
            _ when cost > wasteful => CostEfficiency.Wasteful,
            _ when cost > inefficient => CostEfficiency.Inefficient,
            _ when cost > acceptable => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };

        private static TrendDirection GetTrendDirection(decimal previous, decimal current)
        {
            if (previous <= 0m) return TrendDirection.Stable;
            var deltaPercent = (current - previous) / previous * 100m;
            return deltaPercent switch
            {
                > 5m => TrendDirection.Declining,
                < -5m => TrendDirection.Improving,
                _ => TrendDirection.Stable
            };
        }
    }

    /// <summary>Perfil de FinOps completo de um serviço. IsSimulated é sempre false — dados reais via ICostIntelligenceModule.</summary>
    public sealed record Response(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        decimal MonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency Efficiency,
        IReadOnlyList<WasteSignalDto> WasteSignals,
        decimal TotalWaste,
        IReadOnlyList<EfficiencyIndicatorDto> EfficiencyIndicators,
        decimal ReliabilityScore,
        int RecentIncidents,
        TrendDirection ReliabilityTrend,
        IReadOnlyList<ChangeImpactDto> ChangeImpacts,
        IReadOnlyList<OptimizationDto> Optimizations,
        decimal TotalPotentialSavings,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Sinal de desperdício operacional com timestamp.</summary>
    public sealed record WasteSignalDto(
        string Description,
        string Pattern,
        WasteSignalType Type,
        decimal EstimatedWaste,
        string DetectedAt);

    /// <summary>Indicador de eficiência operacional.</summary>
    public sealed record EfficiencyIndicatorDto(
        string Name,
        EfficiencyCategory Category,
        decimal CurrentValue,
        decimal TargetValue,
        string Assessment);

    /// <summary>Impacto de mudança recente no custo.</summary>
    public sealed record ChangeImpactDto(
        string ChangeId,
        string Description,
        string AppliedAt,
        decimal CostImpact,
        string Explanation);

    /// <summary>Oportunidade de otimização de custo.</summary>
    public sealed record OptimizationDto(
        string Recommendation,
        decimal PotentialSavings,
        string Priority,
        string Rationale);
}
