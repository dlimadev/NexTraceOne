using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa um snapshot analisado de um schema GraphQL.
/// Persiste a estrutura extraída (types, fields, operations) de uma versão de contrato GraphQL,
/// permitindo diff semântico e detecção de breaking changes sem re-parsing.
/// Suporta análise incremental e comparação histórica de schemas.
/// Wave G.3 — GraphQL Schema Analysis (GAP-CTR-01).
/// </summary>
public sealed class GraphQlSchemaSnapshot : AuditableEntity<GraphQlSchemaSnapshotId>
{
    private GraphQlSchemaSnapshot() { }

    /// <summary>Identificador da versão de contrato GraphQL associada.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão semântica do contrato no momento do snapshot.</summary>
    public string ContractVersion { get; private set; } = string.Empty;

    /// <summary>Conteúdo bruto do schema GraphQL analisado (SDL).</summary>
    public string SchemaContent { get; private set; } = string.Empty;

    /// <summary>Número de tipos definidos no schema (object, input, enum, interface, union, scalar).</summary>
    public int TypeCount { get; private set; }

    /// <summary>Número total de campos definidos em todos os tipos do schema.</summary>
    public int FieldCount { get; private set; }

    /// <summary>Número de operations (queries, mutations, subscriptions) definidas no schema.</summary>
    public int OperationCount { get; private set; }

    /// <summary>JSON com a lista de nomes de tipos definidos no schema.</summary>
    public string TypeNamesJson { get; private set; } = "[]";

    /// <summary>JSON com as operações definidas: [ { "name": "...", "kind": "Query|Mutation|Subscription" } ].</summary>
    public string OperationsJson { get; private set; } = "[]";

    /// <summary>JSON com os campos por tipo: { "TypeName": ["field1", "field2"] }.</summary>
    public string FieldsByTypeJson { get; private set; } = "{}";

    /// <summary>Indica se o schema tem queries definidas.</summary>
    public bool HasQueryType { get; private set; }

    /// <summary>Indica se o schema tem mutations definidas.</summary>
    public bool HasMutationType { get; private set; }

    /// <summary>Indica se o schema tem subscriptions definidas.</summary>
    public bool HasSubscriptionType { get; private set; }

    /// <summary>Tenant owner deste snapshot.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Data e hora em que o snapshot foi capturado.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>Cria um novo snapshot de schema GraphQL.</summary>
    public static GraphQlSchemaSnapshot Create(
        Guid apiAssetId,
        string contractVersion,
        string schemaContent,
        int typeCount,
        int fieldCount,
        int operationCount,
        string typeNamesJson,
        string operationsJson,
        string fieldsByTypeJson,
        bool hasQueryType,
        bool hasMutationType,
        bool hasSubscriptionType,
        Guid tenantId,
        DateTimeOffset capturedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(contractVersion);
        Guard.Against.NullOrWhiteSpace(schemaContent);

        return new GraphQlSchemaSnapshot
        {
            Id = GraphQlSchemaSnapshotId.New(),
            ApiAssetId = apiAssetId,
            ContractVersion = contractVersion,
            SchemaContent = schemaContent,
            TypeCount = typeCount,
            FieldCount = fieldCount,
            OperationCount = operationCount,
            TypeNamesJson = typeNamesJson ?? "[]",
            OperationsJson = operationsJson ?? "[]",
            FieldsByTypeJson = fieldsByTypeJson ?? "{}",
            HasQueryType = hasQueryType,
            HasMutationType = hasMutationType,
            HasSubscriptionType = hasSubscriptionType,
            TenantId = tenantId,
            CapturedAt = capturedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de GraphQlSchemaSnapshot.</summary>
public sealed record GraphQlSchemaSnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static GraphQlSchemaSnapshotId New() => new(Guid.NewGuid());
    public static GraphQlSchemaSnapshotId From(Guid id) => new(id);
}
