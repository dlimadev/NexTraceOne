using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetWidgetDelta;

/// <summary>
/// Feature: GetWidgetDelta — retorna as variações de um widget desde um timestamp.
///
/// Usado pelo frontend para fazer polling leve quando SSE está indisponível,
/// ou para sincronizar o estado após reconexão.
///
/// As variações reais requerem bridge com a fonte de dados do widget.
/// O retorno é honestamente simulado (IsSimulated=true) até que esses bridges
/// estejam disponíveis.
///
/// Wave V3.3 — Live, Cross-filter, Drill-down.
/// </summary>
public static class GetWidgetDelta
{
    // ── Types ─────────────────────────────────────────────────────────────

    public sealed record Query(
        Guid DashboardId,
        string WidgetId,
        string TenantId,
        DateTimeOffset Since) : IQuery<Response>;

    public sealed record DeltaRow(
        string RowKey,
        string ChangeType,   // "added" | "changed" | "removed"
        IReadOnlyDictionary<string, string?> Fields);

    public sealed record Response(
        string WidgetId,
        DateTimeOffset Since,
        DateTimeOffset AsOf,
        int AddedCount,
        int RemovedCount,
        int ChangedCount,
        IReadOnlyList<DeltaRow> Changes,
        bool IsSimulated,
        string? SimulatedNote);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().WithMessage("TenantId is required.");
            RuleFor(q => q.WidgetId).NotEmpty().WithMessage("WidgetId is required.");
            RuleFor(q => q.Since).LessThan(DateTimeOffset.MaxValue).WithMessage("Since must be a valid timestamp.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query.TenantId))
                return Task.FromResult<Result<Response>>(
                    Error.Validation("WidgetDelta.TenantId", "TenantId is required."));

            if (string.IsNullOrWhiteSpace(query.WidgetId))
                return Task.FromResult<Result<Response>>(
                    Error.Validation("WidgetDelta.WidgetId", "WidgetId is required."));

            var asOf = DateTimeOffset.UtcNow;
            var elapsed = (asOf - query.Since).TotalSeconds;

            // Simulate: more changes the longer it's been since last poll
            var addedCount   = elapsed > 60  ? Random.Shared.Next(1, 4)  : elapsed > 20 ? 1 : 0;
            var changedCount = elapsed > 10  ? Random.Shared.Next(0, 6)  : 0;
            var removedCount = elapsed > 120 ? Random.Shared.Next(0, 2)  : 0;

            var changes = new List<DeltaRow>(addedCount + changedCount + removedCount);

            for (var i = 0; i < addedCount; i++)
                changes.Add(new DeltaRow(
                    RowKey: $"row-new-{i}",
                    ChangeType: "added",
                    Fields: new Dictionary<string, string?> { ["status"] = "active", ["createdAt"] = asOf.ToString("O") }));

            for (var i = 0; i < changedCount; i++)
                changes.Add(new DeltaRow(
                    RowKey: $"row-upd-{i}",
                    ChangeType: "changed",
                    Fields: new Dictionary<string, string?> { ["status"] = "updated", ["updatedAt"] = asOf.ToString("O") }));

            for (var i = 0; i < removedCount; i++)
                changes.Add(new DeltaRow(
                    RowKey: $"row-del-{i}",
                    ChangeType: "removed",
                    Fields: new Dictionary<string, string?> { ["reason"] = "expired" }));

            return Task.FromResult<Result<Response>>(new Response(
                WidgetId: query.WidgetId,
                Since: query.Since,
                AsOf: asOf,
                AddedCount: addedCount,
                RemovedCount: removedCount,
                ChangedCount: changedCount,
                Changes: changes,
                IsSimulated: true,
                SimulatedNote: "Widget delta data is simulated — real-time ingestion bridge required for live deltas."));
        }
    }
}
