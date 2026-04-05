using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Domain.Portal.Entities;

/// <summary>
/// API Key gerada para acesso programático ao Developer Portal.
/// NUNCA armazena o valor raw — apenas o hash SHA-256 e o prefixo mascarado.
/// </summary>
public sealed class ApiKey : AggregateRoot<ApiKeyId>
{
    private ApiKey() { }

    public Guid OwnerId { get; private set; }
    public Guid? ApiAssetId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByUserId { get; private set; }
    public long RequestCount { get; private set; }

    public static Result<ApiKey> Create(
        Guid ownerId,
        Guid? apiAssetId,
        string name,
        string keyHash,
        string keyPrefix,
        string? description,
        DateTimeOffset? expiresAt,
        DateTimeOffset createdAt)
    {
        if (ownerId == Guid.Empty)
            return Error.Validation("API_KEY_INVALID_OWNER", "Owner ID must not be empty.");
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("API_KEY_INVALID_NAME", "API key name must not be empty.");
        if (name.Length > 200)
            return Error.Validation("API_KEY_NAME_TOO_LONG", "API key name must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(keyHash))
            return Error.Validation("API_KEY_INVALID_HASH", "Key hash must not be empty.");
        if (keyHash.Length > 64)
            return Error.Validation("API_KEY_HASH_TOO_LONG", "Key hash must not exceed 64 characters.");
        if (string.IsNullOrWhiteSpace(keyPrefix))
            return Error.Validation("API_KEY_INVALID_PREFIX", "Key prefix must not be empty.");
        if (keyPrefix.Length > 20)
            return Error.Validation("API_KEY_PREFIX_TOO_LONG", "Key prefix must not exceed 20 characters.");

        return Result<ApiKey>.Success(new ApiKey
        {
            Id = ApiKeyId.New(),
            OwnerId = ownerId,
            ApiAssetId = apiAssetId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Description = description,
            IsActive = true,
            RequestCount = 0,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        });
    }

    /// <summary>Revoga a API Key, impedindo o uso futuro.</summary>
    public Result<Unit> Revoke(string revokedByUserId, DateTimeOffset revokedAt)
    {
        if (!IsActive)
            return DeveloperPortalErrors.ApiKeyAlreadyRevoked(Id.Value.ToString());

        IsActive = false;
        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
        return Unit.Value;
    }

    /// <summary>Regista uso da API Key, incrementando contador e atualizando timestamp.</summary>
    public void RecordUsage(DateTimeOffset usedAt)
    {
        LastUsedAt = usedAt;
        RequestCount++;
    }
}

/// <summary>Identificador fortemente tipado de ApiKey.</summary>
public sealed record ApiKeyId(Guid Value) : TypedIdBase(Value)
{
    public static ApiKeyId New() => new(Guid.NewGuid());
    public static ApiKeyId From(Guid id) => new(id);
}
