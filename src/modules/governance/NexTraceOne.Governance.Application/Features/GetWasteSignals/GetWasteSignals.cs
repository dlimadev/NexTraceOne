using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetWasteSignals;

/// <summary>
/// Feature: GetWasteSignals — sinais de desperdício operacional filtrados por serviço, equipa ou domínio.
/// Desperdício no NexTraceOne está ligado a comportamento operacional, não a billing cloud genérico.
/// IMPLEMENTATION STATUS: Demo — returns illustrative data.
/// </summary>
public static class GetWasteSignals
{
    /// <summary>Query para obter sinais de desperdício.</summary>
    public sealed record Query(
        string? ServiceId = null,
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>Handler que retorna sinais de desperdício operacional.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var signals = new List<WasteSignalDetailDto>
            {
                new("ws-001", "svc-order-processor", "Order Processor", "Commerce", "Team Commerce",
                    WasteSignalType.RepeatedReprocessing, "Frequent rollbacks causing reprocessing",
                    "rollback-waste", 5400m, "High", "2026-03-14T10:00:00Z",
                    "Change chg-2026-0312 introduced instability"),
                new("ws-002", "svc-catalog-sync", "Catalog Sync", "Catalog", "Team Platform",
                    WasteSignalType.RepeatedReprocessing, "Duplicate data processing pipelines",
                    "duplicate-etl", 4200m, "High", "2026-03-13T08:00:00Z",
                    null),
                new("ws-003", "svc-payment-api", "Payment API", "Payments", "Team Payments",
                    WasteSignalType.ExcessiveRetries, "Excessive retries on timeout",
                    "retry-pattern", 3200m, "Medium", "2026-03-10T08:00:00Z",
                    "Upstream latency causing retry storms"),
                new("ws-004", "svc-inventory-sync", "Inventory Sync", "Commerce", "Team Commerce",
                    WasteSignalType.RepeatedReprocessing, "Redundant sync cycles",
                    "redundant-sync", 2800m, "Medium", "2026-03-11T12:00:00Z",
                    null),
                new("ws-005", "svc-catalog-sync", "Catalog Sync", "Catalog", "Team Platform",
                    WasteSignalType.IdleCostlyResource, "Idle staging environment",
                    "idle-staging", 2500m, "Low", "2026-03-09T06:00:00Z",
                    null),
                new("ws-006", "svc-order-processor", "Order Processor", "Commerce", "Team Commerce",
                    WasteSignalType.IdleCostlyResource, "Idle compute during off-peak",
                    "idle-compute", 2100m, "Low", "2026-03-08T20:00:00Z",
                    null),
                new("ws-007", "svc-payment-api", "Payment API", "Payments", "Team Payments",
                    WasteSignalType.OverProvisioned, "Over-provisioned compute instances",
                    "over-provisioned", 2100m, "Medium", "2026-03-12T14:00:00Z",
                    "CPU utilization below 45% consistently")
            };

            var filtered = signals.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(s => s.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(s => s.Team.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                filtered = filtered.Where(s => s.Domain.Equals(request.DomainId, StringComparison.OrdinalIgnoreCase));

            var result = filtered.ToList();

            var response = new Response(
                TotalWaste: result.Sum(s => s.EstimatedWaste),
                SignalCount: result.Count,
                Signals: result,
                ByType: result.GroupBy(s => s.Type)
                    .Select(g => new WasteByTypeDto(g.Key, g.Count(), g.Sum(s => s.EstimatedWaste)))
                    .OrderByDescending(t => t.TotalWaste)
                    .ToList(),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: true,
                DataSource: "demo");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com sinais de desperdício. IsSimulated=true indica dados demonstrativos.</summary>
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
