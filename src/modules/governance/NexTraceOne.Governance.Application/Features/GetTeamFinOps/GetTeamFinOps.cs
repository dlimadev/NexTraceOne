using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetTeamFinOps;

/// <summary>
/// Feature: GetTeamFinOps — perfil de custo contextual agregado por equipa.
/// Inclui resumo de custo, serviços, desperdício, eficiência e correlação com confiabilidade.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetTeamFinOps
{
    /// <summary>Query para obter perfil de FinOps de uma equipa.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps da equipa.</summary>
    /// <summary>Valida os parâmetros da query de FinOps por equipa.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
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
            // ── Ler configurações de eficiência e orçamento por equipa ──
            var costBandsCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyCostBands, ConfigurationScope.Tenant, null, cancellationToken);
            var (wastefulBand, inefficientBand, acceptableBand) = ParseCostBands(costBandsCfg?.EffectiveValue);

            var trendThresholdCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyTrendThresholdPct, ConfigurationScope.Tenant, null, cancellationToken);
            var trendThresholdPct = decimal.TryParse(trendThresholdCfg?.EffectiveValue, out var tt) ? Math.Max(tt, 0.1m) : 5m;

            var teamBudgetCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsBudgetByTeam, ConfigurationScope.Tenant, null, cancellationToken);
            var teamMonthlyBudget = ParseTeamBudget(teamBudgetCfg?.EffectiveValue, request.TeamId);

            var records = await _costModule.GetCostsByTeamAsync(request.TeamId, cancellationToken: cancellationToken) ?? [];
            var allRecords = await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            var latestRecordsByService = records
                .GroupBy(r => r.ServiceId)
                .Select(g => g.OrderByDescending(r => r.Period, StringComparer.Ordinal).First())
                .ToList();

            var previousRecordsByService = records
                .GroupBy(r => r.ServiceId)
                .Select(g => g.OrderByDescending(r => r.Period, StringComparer.Ordinal).Skip(1).FirstOrDefault())
                .Where(r => r is not null)
                .Cast<CostRecordSummary>()
                .ToDictionary(r => r.ServiceId, StringComparer.OrdinalIgnoreCase);

            var teamAverageCost = latestRecordsByService.Count == 0 ? 0m : latestRecordsByService.Average(r => r.TotalCost);
            var tenantAverageCost = allRecords.Count == 0 ? teamAverageCost : allRecords.Average(r => r.TotalCost);

            // Quando existe orçamento configurado para a equipa, as bandas de eficiência são relativas ao orçamento
            var effectiveBand = teamMonthlyBudget > 0
                ? (wasteful: teamMonthlyBudget, inefficient: teamMonthlyBudget * 0.8m, acceptable: teamMonthlyBudget * 0.5m)
                : (wasteful: wastefulBand, inefficient: inefficientBand, acceptable: acceptableBand);

            var services = latestRecordsByService
                .Select(r => new TeamServiceCostDto(
                    r.ServiceId,
                    r.ServiceName,
                    ComputeEfficiency(r.TotalCost, effectiveBand.wasteful, effectiveBand.inefficient, effectiveBand.acceptable),
                    r.TotalCost,
                    GetTrendDirection(previousRecordsByService.GetValueOrDefault(r.ServiceId)?.TotalCost ?? 0m, r.TotalCost, trendThresholdPct),
                    Math.Round(Math.Max(0m, r.TotalCost - tenantAverageCost), 2),
                    0m))
                .ToList();

            var totalCost = services.Sum(s => s.MonthlyCost);
            var previousMonthCost = previousRecordsByService.Values.Sum(v => v.TotalCost);
            var overallEfficiency = services.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(services.Average(s => s.MonthlyCost), effectiveBand.wasteful, effectiveBand.inefficient, effectiveBand.acceptable);

            var topFocus = services.Count == 0
                ? string.Empty
                : $"Review cost allocation for {services.OrderByDescending(s => s.MonthlyCost).First().ServiceName}";

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: request.TeamId,
                Domain: records.FirstOrDefault()?.Domain ?? string.Empty,
                TotalMonthlyCost: totalCost,
                PreviousMonthCost: previousMonthCost,
                CostTrend: GetTrendDirection(previousMonthCost, totalCost, trendThresholdPct),
                OverallEfficiency: overallEfficiency,
                TotalWaste: services.Sum(s => s.WasteAmount),
                ServiceCount: services.Count,
                Services: services,
                TrendSeries: BuildTeamTrendSeries(records),
                AvgReliabilityScore: 0m,
                TotalRecentIncidents: 0,
                TopOptimizationFocus: topFocus,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

            return Result<Response>.Success(response);
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

        private static decimal ParseTeamBudget(string? json, string teamId)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0m;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty(teamId, out var teamEntry))
                {
                    if (teamEntry.TryGetProperty("monthlyBudget", out var mb) && mb.TryGetDecimal(out var bv))
                        return bv;
                }
                return 0m;
            }
            catch { return 0m; }
        }

        private static CostEfficiency ComputeEfficiency(decimal cost, decimal wasteful, decimal inefficient, decimal acceptable) => cost switch
        {
            _ when cost > wasteful => CostEfficiency.Wasteful,
            _ when cost > inefficient => CostEfficiency.Inefficient,
            _ when cost > acceptable => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };

        private static TrendDirection GetTrendDirection(decimal previous, decimal current, decimal thresholdPct)
        {
            if (previous <= 0m) return TrendDirection.Stable;
            var deltaPercent = (current - previous) / previous * 100m;
            return deltaPercent switch
            {
                _ when deltaPercent > thresholdPct => TrendDirection.Declining,
                _ when deltaPercent < -thresholdPct => TrendDirection.Improving,
                _ => TrendDirection.Stable
            };
        }

        private static IReadOnlyList<TrendPointDto> BuildTeamTrendSeries(IReadOnlyList<CostRecordSummary> records) =>
            records
                .GroupBy(r => r.Period)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.TotalCost)))
                .ToList();
    }

    /// <summary>Perfil de FinOps agregado por equipa. IsSimulated é sempre false — dados reais via ICostIntelligenceModule.</summary>
    public sealed record Response(
        string TeamId,
        string TeamName,
        string Domain,
        decimal TotalMonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency OverallEfficiency,
        decimal TotalWaste,
        int ServiceCount,
        IReadOnlyList<TeamServiceCostDto> Services,
        IReadOnlyList<TrendPointDto> TrendSeries,
        decimal AvgReliabilityScore,
        int TotalRecentIncidents,
        string TopOptimizationFocus,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Custo de serviço dentro da equipa.</summary>
    public sealed record TeamServiceCostDto(
        string ServiceId,
        string ServiceName,
        CostEfficiency Efficiency,
        decimal MonthlyCost,
        TrendDirection Trend,
        decimal WasteAmount,
        decimal ReliabilityScore);

    /// <summary>Ponto de série temporal de custo.</summary>
    public sealed record TrendPointDto(
        string Period,
        decimal Cost);
}
