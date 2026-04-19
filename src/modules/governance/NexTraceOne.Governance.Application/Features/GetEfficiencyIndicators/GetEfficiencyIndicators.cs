using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators;

/// <summary>
/// Feature: GetEfficiencyIndicators — indicadores de eficiência operacional filtrados por serviço ou equipa.
/// Eficiência no NexTraceOne mede a relação entre custo e valor operacional real.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// Heurística: custo do serviço vs. custo médio do tenant determina eficiência relativa.
/// </summary>
public static class GetEfficiencyIndicators
{
    /// <summary>Query para obter indicadores de eficiência.</summary>
    public sealed record Query(
        string? ServiceId = null,
        string? TeamId = null) : IQuery<Response>;

    /// <summary>Handler que retorna indicadores de eficiência operacional baseados em dados reais de custo.</summary>
    /// <summary>Valida os filtros opcionais da query de indicadores de eficiência.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).MaximumLength(200)
                .When(x => x.ServiceId is not null);
            RuleFor(x => x.TeamId).MaximumLength(200)
                .When(x => x.TeamId is not null);
        }
    }

    public sealed class Handler(ICostIntelligenceModule costModule, IConfigurationResolutionService configService, IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // ── Ler thresholds de anomalia para derivar bandas de eficiência ──
            var anomalyThresholdsCfg = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsAnomalyThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var (wastefulRatio, inefficientRatio) = ParseAnomalyThresholds(anomalyThresholdsCfg?.EffectiveValue);

            var records = await costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            if (records.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    OverallEfficiencyScore: 0m,
                    ServiceCount: 0,
                    Services: Array.Empty<ServiceEfficiencyDto>(),
                    GeneratedAt: clock.UtcNow,
                    IsSimulated: false,
                    DataSource: "cost-intelligence"));
            }

            var avgCost = records.Average(r => r.TotalCost);

            var indicators = records.Select(r =>
            {
                var efficiency = ClassifyEfficiency(r.TotalCost, avgCost, wastefulRatio, inefficientRatio);
                var costRatio = avgCost > 0 ? r.TotalCost / avgCost : 1m;
                var costScore = Math.Max(0m, Math.Min(200m, (1m / Math.Max(costRatio, 0.01m)) * 100m));

                var metrics = new List<EfficiencyMetricDto>
                {
                    new("Cost vs Average",
                        EfficiencyCategory.CostPerTransaction,
                        r.TotalCost,
                        avgCost,
                        r.Currency,
                        costRatio > 1.5m ? "Significantly above average"
                            : costRatio > 1.0m ? "Above average"
                            : "At or below average")
                };

                return new ServiceEfficiencyDto(
                    r.ServiceId,
                    r.ServiceName,
                    r.Team ?? "Unknown",
                    efficiency,
                    metrics);
            }).ToList();

            var filtered = indicators.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(s => s.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(s => s.Team.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            var result = filtered.ToList();

            static decimal NormalizeMetric(EfficiencyMetricDto m)
            {
                if (m.TargetValue == 0) return 100m;
                var isLowerBetter = m.Category is EfficiencyCategory.ErrorRate
                    or EfficiencyCategory.CostPerTransaction;
                return isLowerBetter
                    ? Math.Min(m.TargetValue / m.CurrentValue * 100, 200m)
                    : Math.Min(m.CurrentValue / m.TargetValue * 100, 200m);
            }

            var overallScore = result.Count > 0
                ? result.Average(s => s.Metrics.Average(NormalizeMetric))
                : 0m;

            return Result<Response>.Success(new Response(
                OverallEfficiencyScore: Math.Round(overallScore, 1),
                ServiceCount: result.Count,
                Services: result,
                GeneratedAt: clock.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence"));
        }

        private static (decimal wastefulRatio, decimal inefficientRatio) ParseAnomalyThresholds(string? json)
        {
            const decimal defaultWasteful = 2.0m;
            const decimal defaultInefficient = 1.5m;
            if (string.IsNullOrWhiteSpace(json)) return (defaultWasteful, defaultInefficient);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                // critical deviation % → ratio (e.g., 100% deviation = 2.0 ratio)
                var wasteful = root.TryGetProperty("critical", out var c) && c.TryGetDecimal(out var cv)
                    ? 1m + cv / 100m : defaultWasteful;
                // high deviation % → ratio (e.g., 50% deviation = 1.5 ratio)
                var inefficient = root.TryGetProperty("high", out var h) && h.TryGetDecimal(out var hv)
                    ? 1m + hv / 100m : defaultInefficient;
                return (Math.Max(wasteful, 1.1m), Math.Max(inefficient, 1.1m));
            }
            catch { return (defaultWasteful, defaultInefficient); }
        }

        private static CostEfficiency ClassifyEfficiency(decimal cost, decimal avgCost, decimal wastefulRatio, decimal inefficientRatio)
        {
            if (avgCost == 0) return CostEfficiency.Efficient;
            var ratio = cost / avgCost;
            return ratio switch
            {
                _ when ratio > wastefulRatio => CostEfficiency.Wasteful,
                _ when ratio > inefficientRatio => CostEfficiency.Inefficient,
                > 0.8m => CostEfficiency.Acceptable,
                _ => CostEfficiency.Efficient
            };
        }
    }

    /// <summary>Resposta com indicadores de eficiência baseados em dados reais.</summary>
    public sealed record Response(
        decimal OverallEfficiencyScore,
        int ServiceCount,
        IReadOnlyList<ServiceEfficiencyDto> Services,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Eficiência operacional de um serviço.</summary>
    public sealed record ServiceEfficiencyDto(
        string ServiceId,
        string ServiceName,
        string Team,
        CostEfficiency Efficiency,
        IReadOnlyList<EfficiencyMetricDto> Metrics);

    /// <summary>Métrica individual de eficiência.</summary>
    public sealed record EfficiencyMetricDto(
        string Name,
        EfficiencyCategory Category,
        decimal CurrentValue,
        decimal TargetValue,
        string Unit,
        string Assessment);
}
