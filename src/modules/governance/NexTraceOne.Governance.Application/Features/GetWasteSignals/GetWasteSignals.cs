using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
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

    public sealed class Handler(ICostIntelligenceModule costModule) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
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
            var p75Index = (int)Math.Floor((sortedCosts.Count - 1) * 0.75m);
            var p75Threshold = sortedCosts.Count > 0 ? sortedCosts[Math.Min(p75Index, sortedCosts.Count - 1)] : 0m;

            var signals = new List<WasteSignalDetailDto>();
            var signalIndex = 0;

            foreach (var r in records)
            {
                if (r.TotalCost <= p75Threshold) continue;

                var waste = r.TotalCost - avgCost;
                if (waste <= 0) continue;

                var (type, pattern, description) = ClassifyWaste(r.TotalCost, avgCost, p75Threshold);
                var severity = waste > avgCost ? "High" : waste > avgCost * 0.5m ? "Medium" : "Low";

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

        private static (WasteSignalType Type, string Pattern, string Description) ClassifyWaste(
            decimal cost, decimal avgCost, decimal p75)
        {
            var ratio = avgCost > 0 ? cost / avgCost : 1m;
            return ratio switch
            {
                > 3.0m => (WasteSignalType.OverProvisioned, "over-provisioned",
                    $"Cost {cost:N2} is {ratio:N1}x above average — likely over-provisioned"),
                > 2.0m => (WasteSignalType.IdleCostlyResource, "idle-costly-resource",
                    $"Cost {cost:N2} is significantly above average — possible idle/underutilized resource"),
                _ => (WasteSignalType.DegradedCostAmplification, "cost-amplification",
                    $"Cost {cost:N2} exceeds p75 threshold {p75:N2} — cost amplification detected")
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
