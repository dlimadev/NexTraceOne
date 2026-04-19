using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.ConfigurationKeys;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using System.Text.Json;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetWasteSignals;

/// <summary>
/// Feature: GetWasteSignals — sinais de desperdício operacional filtrados por serviço, equipa ou domínio.
/// Desperdício no NexTraceOne está ligado a comportamento operacional, não a billing cloud genérico.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// Heurística: serviços com custo acima do percentil 75 do tenant são sinalizados como desperdício potencial.
/// </summary>
public static class GetWasteSignals
{
    /// <summary>Query para obter sinais de desperdício.</summary>
    public sealed record Query(
        string? ServiceId = null,
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>Handler que retorna sinais de desperdício operacional baseados em dados reais de custo.</summary>
    /// <summary>Valida os filtros opcionais da query de sinais de desperdício.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).MaximumLength(200)
                .When(x => x.ServiceId is not null);
            RuleFor(x => x.TeamId).MaximumLength(200)
                .When(x => x.TeamId is not null);
            RuleFor(x => x.DomainId).MaximumLength(200)
                .When(x => x.DomainId is not null);
        }
    }

    public sealed class Handler(ICostIntelligenceModule costModule, IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // ── Ler configuração de detecção de desperdício ──
            var detectionEnabledCfg = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteDetectionEnabled, ConfigurationScope.Tenant, null, cancellationToken);
            var detectionEnabled = detectionEnabledCfg?.EffectiveValue != "false";

            if (!detectionEnabled)
            {
                return Result<Response>.Success(new Response(
                    TotalWaste: 0m,
                    SignalCount: 0,
                    Signals: Array.Empty<WasteSignalDetailDto>(),
                    ByType: Array.Empty<WasteByTypeDto>(),
                    GeneratedAt: DateTimeOffset.UtcNow,
                    IsSimulated: false,
                    DataSource: "cost-intelligence"));
            }

            // ── Ler thresholds de desperdício ──
            var thresholdsCfg = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteThresholds, ConfigurationScope.Tenant, null, cancellationToken);
            var (percentileThreshold, overProvisionedRatio, idleCostlyRatio, mediumSeverityFraction)
                = ParseWasteThresholds(thresholdsCfg?.EffectiveValue);

            // ── Ler categorias activas ──
            var categoriesCfg = await configService.ResolveEffectiveValueAsync(
                GovernanceConfigKeys.FinOpsWasteCategories, ConfigurationScope.Tenant, null, cancellationToken);
            var allowedCategories = ParseStringSet(categoriesCfg?.EffectiveValue);

            var records = await costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            if (records.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    TotalWaste: 0m,
                    SignalCount: 0,
                    Signals: Array.Empty<WasteSignalDetailDto>(),
                    ByType: Array.Empty<WasteByTypeDto>(),
                    GeneratedAt: DateTimeOffset.UtcNow,
                    IsSimulated: false,
                    DataSource: "cost-intelligence"));
            }

            var avgCost = records.Average(r => r.TotalCost);
            var sortedCosts = records.Select(r => r.TotalCost).OrderBy(c => c).ToList();
            var p75Index = (int)Math.Floor((sortedCosts.Count - 1) * (percentileThreshold / 100m));
            var p75Threshold = sortedCosts.Count > 0 ? sortedCosts[Math.Min(p75Index, sortedCosts.Count - 1)] : 0m;

            var signals = new List<WasteSignalDetailDto>();
            var signalIndex = 0;

            foreach (var r in records)
            {
                if (r.TotalCost <= p75Threshold) continue;

                var waste = r.TotalCost - avgCost;
                if (waste <= 0) continue;

                var (type, pattern, description) = ClassifyWaste(r.TotalCost, avgCost, p75Threshold, overProvisionedRatio, idleCostlyRatio);

                // Filtrar por categorias activas quando configurado
                if (allowedCategories.Count > 0 && !allowedCategories.Contains(type.ToString()))
                    continue;

                var severity = waste > avgCost ? "High" : waste > avgCost * mediumSeverityFraction ? "Medium" : "Low";

                signalIndex++;
                signals.Add(new WasteSignalDetailDto(
                    SignalId: $"ws-{signalIndex:D3}",
                    ServiceId: r.ServiceId,
                    ServiceName: r.ServiceName,
                    Domain: r.Domain ?? "Unknown",
                    Team: r.Team ?? "Unknown",
                    Type: type,
                    Description: description,
                    Pattern: pattern,
                    EstimatedWaste: Math.Round(waste, 2),
                    Severity: severity,
                    DetectedAt: DateTimeOffset.UtcNow.ToString("o"),
                    CorrelatedCause: null));
            }

            var filtered = signals.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(s => s.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(s => s.Team.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                filtered = filtered.Where(s => s.Domain.Equals(request.DomainId, StringComparison.OrdinalIgnoreCase));

            var result = filtered.ToList();

            return Result<Response>.Success(new Response(
                TotalWaste: result.Sum(s => s.EstimatedWaste),
                SignalCount: result.Count,
                Signals: result,
                ByType: result.GroupBy(s => s.Type)
                    .Select(g => new WasteByTypeDto(g.Key, g.Count(), g.Sum(s => s.EstimatedWaste)))
                    .OrderByDescending(t => t.TotalWaste)
                    .ToList(),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence"));
        }

        private static (decimal percentileThreshold, decimal overProvisionedRatio, decimal idleCostlyRatio, decimal mediumSeverityFraction)
            ParseWasteThresholds(string? json)
        {
            const decimal defaultPercentile = 75m;
            const decimal defaultOverProvisioned = 3.0m;
            const decimal defaultIdleCostly = 2.0m;
            const decimal defaultMediumFraction = 0.5m;
            if (string.IsNullOrWhiteSpace(json)) return (defaultPercentile, defaultOverProvisioned, defaultIdleCostly, defaultMediumFraction);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var percentile = root.TryGetProperty("percentileThreshold", out var p) && p.TryGetDecimal(out var pv) ? pv : defaultPercentile;
                var overProvisioned = root.TryGetProperty("overProvisionedCostRatio", out var o) && o.TryGetDecimal(out var ov) ? ov : defaultOverProvisioned;
                var idleCostly = root.TryGetProperty("idleCostlyRatio", out var i) && i.TryGetDecimal(out var iv) ? iv : defaultIdleCostly;
                var mediumFraction = root.TryGetProperty("mediumSeverityFraction", out var m) && m.TryGetDecimal(out var mv) ? mv : defaultMediumFraction;
                return (Math.Clamp(percentile, 1m, 99m), Math.Max(overProvisioned, 1.1m), Math.Max(idleCostly, 1.1m), Math.Clamp(mediumFraction, 0.01m, 0.99m));
            }
            catch
            {
                return (defaultPercentile, defaultOverProvisioned, defaultIdleCostly, defaultMediumFraction);
            }
        }

        private static HashSet<string> ParseStringSet(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list is { Count: > 0 } ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase) : [];
            }
            catch { return []; }
        }

        private static (WasteSignalType Type, string Pattern, string Description) ClassifyWaste(
            decimal cost, decimal avgCost, decimal p75, decimal overProvisionedRatio, decimal idleCostlyRatio)
        {
            var ratio = avgCost > 0 ? cost / avgCost : 1m;
            return ratio switch
            {
                _ when ratio > overProvisionedRatio => (WasteSignalType.OverProvisioned, "over-provisioned",
                    $"Cost {cost:N2} is {ratio:N1}x above average — likely over-provisioned"),
                _ when ratio > idleCostlyRatio => (WasteSignalType.IdleCostlyResource, "idle-costly-resource",
                    $"Cost {cost:N2} is significantly above average — possible idle/underutilized resource"),
                _ => (WasteSignalType.DegradedCostAmplification, "cost-amplification",
                    $"Cost {cost:N2} exceeds p{(int)(p75)}% threshold — cost amplification detected")
            };
        }
    }

    /// <summary>Resposta com sinais de desperdício baseados em dados reais.</summary>
    public sealed record Response(
        decimal TotalWaste,
        int SignalCount,
        IReadOnlyList<WasteSignalDetailDto> Signals,
        IReadOnlyList<WasteByTypeDto> ByType,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Sinal de desperdício detalhado.</summary>
    public sealed record WasteSignalDetailDto(
        string SignalId,
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        WasteSignalType Type,
        string Description,
        string Pattern,
        decimal EstimatedWaste,
        string Severity,
        string DetectedAt,
        string? CorrelatedCause);

    /// <summary>Desperdício agregado por tipo.</summary>
    public sealed record WasteByTypeDto(
        WasteSignalType Type,
        int Count,
        decimal TotalWaste);
}
