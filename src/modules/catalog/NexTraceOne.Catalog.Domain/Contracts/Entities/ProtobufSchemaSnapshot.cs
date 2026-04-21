using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa um snapshot analisado de um schema Protobuf (.proto).
/// Persiste a estrutura extraída (messages, fields, services, RPCs) de uma versão de contrato gRPC/Protobuf,
/// permitindo diff semântico e detecção de breaking changes sem re-parsing.
/// Suporta análise incremental e comparação histórica de schemas.
/// Wave H.1 — Protobuf Schema Analysis (GAP-CTR-02).
/// </summary>
public sealed class ProtobufSchemaSnapshot : AuditableEntity<ProtobufSchemaSnapshotId>
{
    private ProtobufSchemaSnapshot() { }

    /// <summary>Identificador da versão de contrato Protobuf associada.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão semântica do contrato no momento do snapshot.</summary>
    public string ContractVersion { get; private set; } = string.Empty;

    /// <summary>Conteúdo bruto do schema Protobuf analisado (.proto).</summary>
    public string SchemaContent { get; private set; } = string.Empty;

    /// <summary>Número de messages definidas no schema.</summary>
    public int MessageCount { get; private set; }

    /// <summary>Número total de fields definidos em todas as messages do schema.</summary>
    public int FieldCount { get; private set; }

    /// <summary>Número de services gRPC definidos no schema.</summary>
    public int ServiceCount { get; private set; }

    /// <summary>Número de RPCs definidos em todos os services do schema.</summary>
    public int RpcCount { get; private set; }

    /// <summary>JSON com a lista de nomes de messages definidas no schema.</summary>
    public string MessageNamesJson { get; private set; } = "[]";

    /// <summary>JSON com os fields por message: { "MessageName": ["field1", "field2"] }.</summary>
    public string FieldsByMessageJson { get; private set; } = "{}";

    /// <summary>JSON com os RPCs por service: { "ServiceName": ["rpc1", "rpc2"] }.</summary>
    public string RpcsByServiceJson { get; private set; } = "{}";

    /// <summary>Sintaxe do ficheiro Protobuf (proto2 ou proto3).</summary>
    public string Syntax { get; private set; } = "proto3";

    /// <summary>Tenant owner deste snapshot.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Data e hora em que o snapshot foi capturado.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>Cria um novo snapshot de schema Protobuf.</summary>
    public static ProtobufSchemaSnapshot Create(
        Guid apiAssetId,
        string contractVersion,
        string schemaContent,
        int messageCount,
        int fieldCount,
        int serviceCount,
        int rpcCount,
        string messageNamesJson,
        string fieldsByMessageJson,
        string rpcsByServiceJson,
        string syntax,
        Guid tenantId,
        DateTimeOffset capturedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(contractVersion);
        Guard.Against.NullOrWhiteSpace(schemaContent);

        return new ProtobufSchemaSnapshot
        {
            Id = ProtobufSchemaSnapshotId.New(),
            ApiAssetId = apiAssetId,
            ContractVersion = contractVersion,
            SchemaContent = schemaContent,
            MessageCount = messageCount,
            FieldCount = fieldCount,
            ServiceCount = serviceCount,
            RpcCount = rpcCount,
            MessageNamesJson = messageNamesJson ?? "[]",
            FieldsByMessageJson = fieldsByMessageJson ?? "{}",
            RpcsByServiceJson = rpcsByServiceJson ?? "{}",
            Syntax = syntax ?? "proto3",
            TenantId = tenantId,
            CapturedAt = capturedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ProtobufSchemaSnapshot.</summary>
public sealed record ProtobufSchemaSnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static ProtobufSchemaSnapshotId New() => new(Guid.NewGuid());
    public static ProtobufSchemaSnapshotId From(Guid id) => new(id);
}
