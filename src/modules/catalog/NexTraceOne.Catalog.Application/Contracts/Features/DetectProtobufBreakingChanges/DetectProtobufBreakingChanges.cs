using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.DetectProtobufBreakingChanges;

/// <summary>
/// Feature: DetectProtobufBreakingChanges — compara dois snapshots de schema Protobuf
/// e devolve a lista de breaking changes detectados.
///
/// Uma mudança é considerada "breaking" quando:
/// - Uma message é removida do schema
/// - Um field é removido de uma message existente
/// - Um RPC é removido de um service existente
/// - Um service é removido do schema
///
/// Mudanças aditivas (novas messages, fields, services ou RPCs) são classificadas como "non-breaking".
///
/// Wave H.1 — Protobuf Schema Analysis (GAP-CTR-02).
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class DetectProtobufBreakingChanges
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
        IProtobufSchemaSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseSnapshot = await repository.GetByIdAsync(
                ProtobufSchemaSnapshotId.From(request.BaseSnapshotId), cancellationToken);
            if (baseSnapshot is null)
                return Error.NotFound("ProtobufSnapshotNotFound.Base",
                    $"Base snapshot '{request.BaseSnapshotId}' not found.");

            var targetSnapshot = await repository.GetByIdAsync(
                ProtobufSchemaSnapshotId.From(request.TargetSnapshotId), cancellationToken);
            if (targetSnapshot is null)
                return Error.NotFound("ProtobufSnapshotNotFound.Target",
                    $"Target snapshot '{request.TargetSnapshotId}' not found.");

            var breakingChanges = new List<ProtobufChangeEntry>();
            var nonBreakingChanges = new List<ProtobufChangeEntry>();

            var baseMessages = DeserializeStringList(baseSnapshot.MessageNamesJson);
            var targetMessages = DeserializeStringList(targetSnapshot.MessageNamesJson);

            var baseFields = DeserializeNestedMap(baseSnapshot.FieldsByMessageJson);
            var targetFields = DeserializeNestedMap(targetSnapshot.FieldsByMessageJson);

            var baseRpcs = DeserializeNestedMap(baseSnapshot.RpcsByServiceJson);
            var targetRpcs = DeserializeNestedMap(targetSnapshot.RpcsByServiceJson);

            // --- Removed messages (breaking) ---
            foreach (var msg in baseMessages.Except(targetMessages))
                breakingChanges.Add(new("MessageRemoved", msg, null,
                    $"Message '{msg}' was removed from the schema."));

            // --- Added messages (non-breaking) ---
            foreach (var msg in targetMessages.Except(baseMessages))
                nonBreakingChanges.Add(new("MessageAdded", msg, null,
                    $"Message '{msg}' was added to the schema."));

            // --- Field-level changes within existing messages ---
            foreach (var msgName in baseMessages.Intersect(targetMessages))
            {
                var baseF = baseFields.TryGetValue(msgName, out var bf) ? bf : [];
                var targetF = targetFields.TryGetValue(msgName, out var tf) ? tf : [];

                foreach (var field in baseF.Except(targetF))
                    breakingChanges.Add(new("FieldRemoved", field, msgName,
                        $"Field '{field}' was removed from message '{msgName}'."));

                foreach (var field in targetF.Except(baseF))
                    nonBreakingChanges.Add(new("FieldAdded", field, msgName,
                        $"Field '{field}' was added to message '{msgName}'."));
            }

            // --- Removed services (breaking) ---
            var baseServices = baseRpcs.Keys.ToHashSet();
            var targetServices = targetRpcs.Keys.ToHashSet();

            foreach (var svc in baseServices.Except(targetServices))
                breakingChanges.Add(new("ServiceRemoved", svc, null,
                    $"Service '{svc}' was removed from the schema."));

            // --- Added services (non-breaking) ---
            foreach (var svc in targetServices.Except(baseServices))
                nonBreakingChanges.Add(new("ServiceAdded", svc, null,
                    $"Service '{svc}' was added to the schema."));

            // --- RPC-level changes within existing services ---
            foreach (var svcName in baseServices.Intersect(targetServices))
            {
                var baseR = baseRpcs.TryGetValue(svcName, out var br) ? br : [];
                var targetR = targetRpcs.TryGetValue(svcName, out var tr) ? tr : [];

                foreach (var rpc in baseR.Except(targetR))
                    breakingChanges.Add(new("RpcRemoved", rpc, svcName,
                        $"RPC '{rpc}' was removed from service '{svcName}'."));

                foreach (var rpc in targetR.Except(baseR))
                    nonBreakingChanges.Add(new("RpcAdded", rpc, svcName,
                        $"RPC '{rpc}' was added to service '{svcName}'."));
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

        private static Dictionary<string, List<string>> DeserializeNestedMap(string json)
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? []; }
            catch { return []; }
        }
    }

    public sealed record ProtobufChangeEntry(
        string ChangeType,
        string Name,
        string? ParentName,
        string Description);

    public sealed record Response(
        Guid BaseSnapshotId,
        Guid TargetSnapshotId,
        string BaseVersion,
        string TargetVersion,
        bool HasBreakingChanges,
        int BreakingChangeCount,
        int NonBreakingChangeCount,
        IReadOnlyList<ProtobufChangeEntry> BreakingChanges,
        IReadOnlyList<ProtobufChangeEntry> NonBreakingChanges);
}
