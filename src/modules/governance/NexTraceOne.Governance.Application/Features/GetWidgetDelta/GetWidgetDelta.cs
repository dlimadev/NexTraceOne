using System.Text.Json;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetWidgetDelta;

/// <summary>
/// Feature: GetWidgetDelta — retorna as variações de um widget desde um timestamp.
///
/// Compara snapshots reais armazenados na tabela gov_widget_snapshots.
/// Se não existirem snapshots suficientes, retorna IsSimulated=true com nota honesta.
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

    public sealed class Handler(IWidgetSnapshotRepository snapshots) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query.TenantId))
                return Error.Validation("WidgetDelta.TenantId", "TenantId is required.");
            if (string.IsNullOrWhiteSpace(query.WidgetId))
                return Error.Validation("WidgetDelta.WidgetId", "WidgetId is required.");

            var asOf = DateTimeOffset.UtcNow;

            var recentSnapshots = (await snapshots.ListSinceAsync(
                query.TenantId, query.DashboardId, query.WidgetId, query.Since, ct)) ?? [];

            if (recentSnapshots.Count == 0)
            {
                return new Response(
                    WidgetId: query.WidgetId,
                    Since: query.Since,
                    AsOf: asOf,
                    AddedCount: 0,
                    RemovedCount: 0,
                    ChangedCount: 0,
                    Changes: [],
                    IsSimulated: true,
                    SimulatedNote: "No snapshots captured yet for this widget. Deltas will be available after the first snapshot cycle completes.");
            }

            var baseSnapshot = await snapshots.GetLatestBeforeAsync(
                query.TenantId, query.DashboardId, query.WidgetId, query.Since, ct);

            var changes = new List<DeltaRow>();

            if (baseSnapshot is null)
            {
                // All recent snapshots are "new" relative to this window
                foreach (var snap in recentSnapshots)
                {
                    var fields = ParseFlatFields(snap.DataJson);
                    changes.Add(new DeltaRow(
                        RowKey: snap.Id.Value.ToString("N"),
                        ChangeType: "added",
                        Fields: fields));
                }

                return new Response(
                    WidgetId: query.WidgetId,
                    Since: query.Since,
                    AsOf: asOf,
                    AddedCount: changes.Count,
                    RemovedCount: 0,
                    ChangedCount: 0,
                    Changes: changes,
                    IsSimulated: false,
                    SimulatedNote: null);
            }

            // Compare base snapshot hash with each subsequent snapshot
            var prevHash = baseSnapshot.DataHash;
            var prevFields = ParseFlatFields(baseSnapshot.DataJson);

            foreach (var snap in recentSnapshots)
            {
                if (snap.DataHash == prevHash)
                    continue;

                var currentFields = ParseFlatFields(snap.DataJson);
                var changedFields = new Dictionary<string, string?>();

                foreach (var kv in currentFields)
                {
                    if (!prevFields.TryGetValue(kv.Key, out var prevVal) || prevVal != kv.Value)
                        changedFields[kv.Key] = kv.Value;
                }

                foreach (var key in prevFields.Keys.Except(currentFields.Keys))
                    changedFields[key] = null;

                if (changedFields.Count > 0)
                    changes.Add(new DeltaRow(
                        RowKey: snap.Id.Value.ToString("N"),
                        ChangeType: "changed",
                        Fields: changedFields));

                prevHash = snap.DataHash;
                prevFields = currentFields;
            }

            return new Response(
                WidgetId: query.WidgetId,
                Since: query.Since,
                AsOf: asOf,
                AddedCount: 0,
                RemovedCount: 0,
                ChangedCount: changes.Count,
                Changes: changes,
                IsSimulated: false,
                SimulatedNote: null);
        }

        private static Dictionary<string, string?> ParseFlatFields(string dataJson)
        {
            var result = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(dataJson))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(dataJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    result[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                        ? null
                        : prop.Value.ToString();
            }
            catch (JsonException)
            {
                result["raw"] = dataJson;
            }

            return result;
        }
    }
}
