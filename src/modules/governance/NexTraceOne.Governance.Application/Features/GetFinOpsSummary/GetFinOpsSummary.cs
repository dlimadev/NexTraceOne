using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsSummary;

/// <summary>
/// Feature: GetFinOpsSummary — resumo de FinOps contextual por serviço, equipa e domínio.
/// FinOps no NexTraceOne é contextual: ligado a operação, comportamento e eficiência.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetFinOpsSummary
{
    /// <summary>Query de resumo de FinOps contextual.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null,
        string? Range = null) : IQuery<Response>;

    /// <summary>Handler que computa resumo de FinOps contextual.</summary>
    /// <summary>Valida os filtros opcionais da query de resumo FinOps.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).MaximumLength(200)
                .When(x => x.TeamId is not null);
            RuleFor(x => x.DomainId).MaximumLength(200)
                .When(x => x.DomainId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200)
                .When(x => x.ServiceId is not null);
            RuleFor(x => x.Range).MaximumLength(200)
                .When(x => x.Range is not null);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;
        private readonly IConfigurationResolutionService _configService;

        public Handler(ICostIntelligenceModule costModule, IConfigurationResolutionService configService)
        {
            _costModule = costModule;
            _configService = configService;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // ── Ler política de recomendações e bandas de eficiência ──
            var recommendationCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsRecommendationPolicy, ConfigurationScope.Tenant, null, cancellationToken);
            var savingsRatePct = ParseSavingsRatePct(recommendationCfg?.EffectiveValue);

            var costBandsCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyCostBands, ConfigurationScope.Tenant, null, cancellationToken);
            var (wastefulBand, inefficientBand, acceptableBand) = ParseCostBands(costBandsCfg?.EffectiveValue);

            var records = await _costModule.GetCostRecordsAsync(request.Range, cancellationToken) ?? [];

            var filtered = records.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(r => (r.Team ?? string.Empty).Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                filtered = filtered.Where(r => (r.Domain ?? string.Empty).Equals(request.DomainId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(r => r.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            var costRecords = filtered.ToList();

            var services = costRecords
                .Select(r => new ServiceCostDto(
                    r.ServiceId,
                    r.ServiceName,
                    r.Domain ?? string.Empty,
                    r.Team ?? string.Empty,
                    ComputeEfficiency(r.TotalCost, wastefulBand, inefficientBand, acceptableBand),
                    r.TotalCost,
                    TrendDirection.Stable,
                    BuildWasteSignals(r.TotalCost, costRecords),
                    null))
                .ToList();

            var topDrivers = services
                .OrderByDescending(s => s.MonthlyCost)
                .Take(3)
                .Select(s => new CostDriverDto(s.ServiceId, s.ServiceName, s.MonthlyCost, s.Efficiency))
                .ToList();

            var optimizationOpportunities = services
                .Where(s => s.Efficiency is CostEfficiency.Wasteful or CostEfficiency.Inefficient)
                .Select(s =>
                {
                    var waste = s.WasteSignals.Sum(w => w.EstimatedWaste);
                    var potentialSavings = Math.Round(waste * (savingsRatePct / 100m), 2);
                    return new OptimizationOpportunityDto(
                        s.ServiceId, s.ServiceName,
                        potentialSavings,
                        s.Efficiency == CostEfficiency.Wasteful ? "High" : "Medium",
                        $"Review cost allocation for {s.ServiceName}");
                })
                .ToList();

            var overallEfficiency = services.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(services.Average(s => s.MonthlyCost), wastefulBand, inefficientBand, acceptableBand);

            var response = new Response(
                TotalMonthlyCost: services.Sum(s => s.MonthlyCost),
                TotalWaste: services.Sum(s => s.WasteSignals.Sum(w => w.EstimatedWaste)),
                OverallEfficiency: overallEfficiency,
                CostTrend: TrendDirection.Stable,
                Services: services,
                TopCostDrivers: topDrivers,
                TopWasteSignals: services
                    .SelectMany(s => s.WasteSignals)
                    .OrderByDescending(s => s.EstimatedWaste)
                    .Take(5)
                    .ToList(),
                OptimizationOpportunities: optimizationOpportunities,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

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

        private static CostEfficiency ComputeEfficiency(decimal cost, decimal wasteful, decimal inefficient, decimal acceptable) => cost switch
        {
            _ when cost > wasteful => CostEfficiency.Wasteful,
            _ when cost > inefficient => CostEfficiency.Inefficient,
            _ when cost > acceptable => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };

        private static IReadOnlyList<WasteSignalDto> BuildWasteSignals(decimal currentCost, IReadOnlyList<CostRecordSummary> allRecords)
        {
            if (allRecords.Count == 0) return Array.Empty<WasteSignalDto>();
            var average = allRecords.Average(r => r.TotalCost);
            var waste = Math.Max(0m, currentCost - average);
            if (waste <= 0m) return Array.Empty<WasteSignalDto>();

            return
            [
                new WasteSignalDto(
                    Description: "Cost is above tenant average for the selected scope.",
                    Pattern: "cost-above-average",
                    Type: WasteSignalType.DegradedCostAmplification,
                    EstimatedWaste: Math.Round(waste, 2))
            ];
        }
    }

    /// <summary>
    /// Resposta do resumo de FinOps.
    /// IsSimulated é sempre false — dados são reais via ICostIntelligenceModule cross-module.
    /// O parâmetro existe para futura extensão caso se adicionem cenários de simulação/projeção.
    /// </summary>
    public sealed record Response(
        decimal TotalMonthlyCost,
        decimal TotalWaste,
        CostEfficiency OverallEfficiency,
        TrendDirection CostTrend,
        IReadOnlyList<ServiceCostDto> Services,
        IReadOnlyList<CostDriverDto> TopCostDrivers,
        IReadOnlyList<WasteSignalDto> TopWasteSignals,
        IReadOnlyList<OptimizationOpportunityDto> OptimizationOpportunities,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Custo contextual por serviço.</summary>
    public sealed record ServiceCostDto(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        CostEfficiency Efficiency,
        decimal MonthlyCost,
        TrendDirection Trend,
        IReadOnlyList<WasteSignalDto> WasteSignals,
        ReliabilityCorrelationDto? ReliabilityCorrelation);

    /// <summary>Sinal de desperdício operacional identificado.</summary>
    public sealed record WasteSignalDto(
        string Description,
        string Pattern,
        WasteSignalType Type,
        decimal EstimatedWaste);

    /// <summary>Correlação de custo com confiabilidade operacional.</summary>
    public sealed record ReliabilityCorrelationDto(
        decimal ReliabilityScore,
        int RecentIncidents,
        TrendDirection ReliabilityTrend);

    /// <summary>Principal driver de custo.</summary>
    public sealed record CostDriverDto(
        string ServiceId,
        string ServiceName,
        decimal MonthlyCost,
        CostEfficiency Efficiency);

    /// <summary>Oportunidade de otimização de custo.</summary>
    public sealed record OptimizationOpportunityDto(
        string ServiceId,
        string ServiceName,
        decimal PotentialSavings,
        string Priority,
        string Recommendation);
}
