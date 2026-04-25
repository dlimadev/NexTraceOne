using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para StorageBucket.
/// </summary>
public sealed record StorageBucketId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Bucket de storage que define para onde os dados de telemetria são encaminhados
/// com base numa condição de filtro e retenção configurável.
///
/// O StorageBucketRouter avalia os buckets por Priority ascendente e encaminha
/// cada evento ao primeiro bucket cuja condição seja satisfeita.
///
/// Buckets default por tenant: audit (2555 dias, ES), debug (3 dias, CH), default (90 dias, ES).
/// Owner: módulo Integrations (Pipeline).
/// </summary>
public sealed class StorageBucket : Entity<StorageBucketId>
{
    /// <summary>Tenant proprietário do bucket.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome do bucket (ex: "audit", "debug", "default").</summary>
    public string BucketName { get; private set; } = string.Empty;

    /// <summary>Backend de storage de destino.</summary>
    public StorageBucketBackendType BackendType { get; private set; }

    /// <summary>Número de dias de retenção dos dados neste bucket.</summary>
    public int RetentionDays { get; private set; }

    /// <summary>
    /// Condição de filtro em JSON para decidir se um evento vai para este bucket.
    /// Formato: {"field": "$.level", "operator": "eq", "value": "debug"}
    /// Null ou {} significa "aceita todos".
    /// </summary>
    public string? FilterJson { get; private set; }

    /// <summary>Prioridade de avaliação (menor = avaliado primeiro).</summary>
    public int Priority { get; private set; }

    /// <summary>Indica se o bucket está activo.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Indica se é o bucket de fallback (aceita tudo, último a ser avaliado).</summary>
    public bool IsFallback { get; private set; }

    /// <summary>Descrição opcional.</summary>
    public string? Description { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private StorageBucket() { }

    /// <summary>Cria um novo bucket de storage.</summary>
    public static StorageBucket Create(
        string tenantId,
        string bucketName,
        StorageBucketBackendType backendType,
        int retentionDays,
        string? filterJson,
        int priority,
        bool isEnabled,
        bool isFallback,
        string? description,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
        Guard.Against.StringTooLong(bucketName, 100, nameof(bucketName));
        Guard.Against.NegativeOrZero(retentionDays, nameof(retentionDays));
        Guard.Against.NegativeOrZero(priority, nameof(priority));

        return new StorageBucket
        {
            Id = new StorageBucketId(Guid.NewGuid()),
            TenantId = tenantId,
            BucketName = bucketName.Trim(),
            BackendType = backendType,
            RetentionDays = retentionDays,
            FilterJson = string.IsNullOrWhiteSpace(filterJson) ? null : filterJson,
            Priority = priority,
            IsEnabled = isEnabled,
            IsFallback = isFallback,
            Description = description?.Trim(),
            CreatedAt = utcNow
        };
    }

    /// <summary>Actualiza a configuração do bucket.</summary>
    public void Update(
        string bucketName,
        StorageBucketBackendType backendType,
        int retentionDays,
        string? filterJson,
        int priority,
        bool isEnabled,
        bool isFallback,
        string? description,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
        Guard.Against.StringTooLong(bucketName, 100, nameof(bucketName));
        Guard.Against.NegativeOrZero(retentionDays, nameof(retentionDays));
        Guard.Against.NegativeOrZero(priority, nameof(priority));

        BucketName = bucketName.Trim();
        BackendType = backendType;
        RetentionDays = retentionDays;
        FilterJson = string.IsNullOrWhiteSpace(filterJson) ? null : filterJson;
        Priority = priority;
        IsEnabled = isEnabled;
        IsFallback = isFallback;
        Description = description?.Trim();
        UpdatedAt = utcNow;
    }
}
