using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetDomainFinOps;

/// <summary>
/// Feature: GetDomainFinOps — perfil de custo contextual agregado por domínio.
/// Inclui resumo de custo, equipas, desperdício, eficiência e correlação com confiabilidade.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetDomainFinOps
{
    /// <summary>Query para obter perfil de FinOps de um domínio.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do domínio.</summary>
    /// <summary>Valida os parâmetros da query de FinOps por domínio.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DomainId).NotEmpty().MaximumLength(200);
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
            // ── Ler configurações de eficiência ──
            var costBandsCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyCostBands, ConfigurationScope.Tenant, null, cancellationToken);
            var (wastefulBand, inefficientBand, acceptableBand) = ParseCostBands(costBandsCfg?.EffectiveValue);

            var trendThresholdCfg = await _configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsEfficiencyTrendThresholdPct, ConfigurationScope.Tenant, null, cancellationToken);
            var trendThresholdPct = decimal.TryParse(trendThresholdCfg?.EffectiveValue, out var tt) ? Math.Max(tt, 0.1m) : 5m;

            var records = await _costModule.GetCostsByDomainAsync(request.DomainId, cancellationToken: cancellationToken) ?? [];
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

            var tenantAverageCost = allRecords.Count == 0 ? 0m : allRecords.Average(r => r.TotalCost);

            var teams = latestRecordsByService
                .GroupBy(r => r.Team ?? string.Empty)
                .Select(g => new DomainTeamCostDto(
                    g.Key,
                    g.Key,
                    g.Count(),
                    g.Sum(r => r.TotalCost),
                    Math.Round(g.Sum(r => Math.Max(0m, r.TotalCost - tenantAverageCost)), 2),
                    ComputeEfficiency(g.Average(r => r.TotalCost), wastefulBand, inefficientBand, acceptableBand),
                    0m))
                .ToList();

            var topWasteServices = latestRecordsByService
                .OrderByDescending(r => r.TotalCost)
                .Where(r => ComputeEfficiency(r.TotalCost, wastefulBand, inefficientBand, acceptableBand) is CostEfficiency.Wasteful or CostEfficiency.Inefficient)
                .Take(5)
                .Select(r => new WasteServiceDto(
                    r.ServiceId,
                    r.ServiceName,
                    r.Team ?? string.Empty,
                    Math.Round(Math.Max(0m, r.TotalCost - tenantAverageCost), 2),
                    ComputeEfficiency(r.TotalCost, wastefulBand, inefficientBand, acceptableBand)))
                .ToList();

            var totalCost = teams.Sum(t => t.MonthlyCost);
            var previousMonthCost = previousRecordsByService.Values.Sum(v => v.TotalCost);
            var overallEfficiency = teams.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(teams.Average(t => t.MonthlyCost), wastefulBand, inefficientBand, acceptableBand);

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: request.DomainId,
                TotalMonthlyCost: totalCost,
                PreviousMonthCost: previousMonthCost,
                CostTrend: GetTrendDirection(previousMonthCost, totalCost, trendThresholdPct),
                OverallEfficiency: overallEfficiency,
                TotalWaste: teams.Sum(t => t.WasteAmount),
                TeamCount: teams.Count,
                ServiceCount: latestRecordsByService.Count,
                Teams: teams,
                TopWasteServices: topWasteServices,
                TrendSeries: BuildDomainTrendSeries(records),
                AvgReliabilityScore: 0m,
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

        private static IReadOnlyList<TrendPointDto> BuildDomainTrendSeries(IReadOnlyList<CostRecordSummary> records) =>
            records
                .GroupBy(r => r.Period)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.TotalCost)))
                .ToList();
    }

    /// <summary>Perfil de FinOps agregado por domínio. IsSimulated é sempre false — dados reais via ICostIntelligenceModule.</summary>
    public sealed record Response(
        string DomainId,
        string DomainName,
        decimal TotalMonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency OverallEfficiency,
        decimal TotalWaste,
        int TeamCount,
        int ServiceCount,
        IReadOnlyList<DomainTeamCostDto> Teams,
        IReadOnlyList<WasteServiceDto> TopWasteServices,
        IReadOnlyList<TrendPointDto> TrendSeries,
        decimal AvgReliabilityScore,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Custo de equipa dentro do domínio.</summary>
    public sealed record DomainTeamCostDto(
        string TeamId,
        string TeamName,
        int ServiceCount,
        decimal MonthlyCost,
        decimal WasteAmount,
        CostEfficiency Efficiency,
        decimal AvgReliabilityScore);

    /// <summary>Serviço com maior desperdício no domínio.</summary>
    public sealed record WasteServiceDto(
        string ServiceId,
        string ServiceName,
        string Team,
        decimal WasteAmount,
        CostEfficiency Efficiency);

    /// <summary>Ponto de série temporal de custo.</summary>
    public sealed record TrendPointDto(
        string Period,
        decimal Cost);
}
