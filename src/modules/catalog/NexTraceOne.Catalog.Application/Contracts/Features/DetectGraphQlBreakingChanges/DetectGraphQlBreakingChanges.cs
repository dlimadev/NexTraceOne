using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.DetectGraphQlBreakingChanges;

/// <summary>
/// Feature: DetectGraphQlBreakingChanges — compara dois snapshots de schema GraphQL
/// e devolve a lista de breaking changes detectados.
///
/// Uma mudança é considerada "breaking" quando:
/// - Um tipo é removido do schema
/// - Um campo é removido de um tipo existente
/// - Uma operation (query/mutation/subscription) é removida
///
/// Mudanças aditivas (novos tipos, campos ou operations) são classificadas como "non-breaking".
///
/// Wave G.3 — GraphQL Schema Analysis (GAP-CTR-01).
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class DetectGraphQlBreakingChanges
{
    public sealed record Query(
        Guid BaseSnapshotId,
        Guid TargetSnapshotId,
        Guid TenantId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BaseSnapshotId).NotEmpty();
            RuleFor(x => x.TargetSnapshotId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        IGraphQlSchemaSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseSnapshot = await repository.GetByIdAsync(
                GraphQlSchemaSnapshotId.From(request.BaseSnapshotId), cancellationToken);
            if (baseSnapshot is null)
                return Error.NotFound("GraphQlSnapshotNotFound.Base",
                    $"Base snapshot '{request.BaseSnapshotId}' not found.");

            var targetSnapshot = await repository.GetByIdAsync(
                GraphQlSchemaSnapshotId.From(request.TargetSnapshotId), cancellationToken);
            if (targetSnapshot is null)
                return Error.NotFound("GraphQlSnapshotNotFound.Target",
                    $"Target snapshot '{request.TargetSnapshotId}' not found.");

            var breakingChanges = new List<GraphQlChangeEntry>();
            var nonBreakingChanges = new List<GraphQlChangeEntry>();

            // Deserialize type lists
            var baseTypes = DeserializeStringList(baseSnapshot.TypeNamesJson);
            var targetTypes = DeserializeStringList(targetSnapshot.TypeNamesJson);

            // Deserialize operations
            var baseOps = DeserializeOperations(baseSnapshot.OperationsJson);
            var targetOps = DeserializeOperations(targetSnapshot.OperationsJson);

            // Deserialize fields-by-type
            var baseFields = DeserializeFieldsByType(baseSnapshot.FieldsByTypeJson);
            var targetFields = DeserializeFieldsByType(targetSnapshot.FieldsByTypeJson);

            // --- Removed types (breaking) ---
            foreach (var type in baseTypes.Except(targetTypes))
                breakingChanges.Add(new GraphQlChangeEntry("TypeRemoved", type, null,
                    $"Type '{type}' was removed from the schema."));

            // --- Added types (non-breaking) ---
            foreach (var type in targetTypes.Except(baseTypes))
                nonBreakingChanges.Add(new GraphQlChangeEntry("TypeAdded", type, null,
                    $"Type '{type}' was added to the schema."));

            // --- Removed operations (breaking) ---
            var baseOpKeys = baseOps.Select(o => $"{o.Kind}:{o.Name}").ToHashSet();
            var targetOpKeys = targetOps.Select(o => $"{o.Kind}:{o.Name}").ToHashSet();

            foreach (var opKey in baseOpKeys.Except(targetOpKeys))
            {
                var parts = opKey.Split(':');
                breakingChanges.Add(new GraphQlChangeEntry("OperationRemoved", parts[1], parts[0],
                    $"{parts[0]} '{parts[1]}' was removed."));
            }

            // --- Added operations (non-breaking) ---
            foreach (var opKey in targetOpKeys.Except(baseOpKeys))
            {
                var parts = opKey.Split(':');
                nonBreakingChanges.Add(new GraphQlChangeEntry("OperationAdded", parts[1], parts[0],
                    $"{parts[0]} '{parts[1]}' was added."));
            }

            // --- Field-level changes ---
            foreach (var typeName in baseTypes.Intersect(targetTypes))
            {
                var baseF = baseFields.TryGetValue(typeName, out var bf) ? bf : [];
                var targetF = targetFields.TryGetValue(typeName, out var tf) ? tf : [];

                foreach (var field in baseF.Except(targetF))
                    breakingChanges.Add(new GraphQlChangeEntry("FieldRemoved", field, typeName,
                        $"Field '{field}' was removed from type '{typeName}'."));

                foreach (var field in targetF.Except(baseF))
                    nonBreakingChanges.Add(new GraphQlChangeEntry("FieldAdded", field, typeName,
                        $"Field '{field}' was added to type '{typeName}'."));
            }

            return Result<Response>.Success(new Response(
                BaseSnapshotId: request.BaseSnapshotId,
                TargetSnapshotId: request.TargetSnapshotId,
                BaseVersion: baseSnapshot.ContractVersion,
                TargetVersion: targetSnapshot.ContractVersion,
                HasBreakingChanges: breakingChanges.Count > 0,
                BreakingChangeCount: breakingChanges.Count,
                NonBreakingChangeCount: nonBreakingChanges.Count,
                BreakingChanges: breakingChanges,
                NonBreakingChanges: nonBreakingChanges));
        }

        private static List<string> DeserializeStringList(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
        }

        private static List<OperationRef> DeserializeOperations(string json)
        {
            try { return JsonSerializer.Deserialize<List<OperationRef>>(json) ?? []; }
            catch { return []; }
        }

        private static Dictionary<string, List<string>> DeserializeFieldsByType(string json)
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? []; }
            catch { return []; }
        }
    }

    private sealed record OperationRef(string Name, string Kind);

    public sealed record GraphQlChangeEntry(
        string ChangeType,
        string Name,
        string? ParentType,
        string Description);

    public sealed record Response(
        Guid BaseSnapshotId,
        Guid TargetSnapshotId,
        string BaseVersion,
        string TargetVersion,
        bool HasBreakingChanges,
        int BreakingChangeCount,
        int NonBreakingChangeCount,
        IReadOnlyList<GraphQlChangeEntry> BreakingChanges,
        IReadOnlyList<GraphQlChangeEntry> NonBreakingChanges);
}
