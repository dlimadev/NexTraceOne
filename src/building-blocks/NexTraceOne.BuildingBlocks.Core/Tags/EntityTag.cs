using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Core.Tags;

/// <summary>
/// Representa uma tag associada a uma entidade da plataforma.
/// Tags são pares key:value que permitem classificação transversal.
///
/// Invariantes:
/// - Key não pode exceder 50 caracteres.
/// - Value não pode exceder 100 caracteres.
/// - EntityType deve ser um dos valores suportados.
/// </summary>
public sealed class EntityTag : AuditableEntity<EntityTagId>
{
    private const int MaxKeyLength = 50;
    private const int MaxValueLength = 100;

    private EntityTag() { }

    public string TenantId { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    public static EntityTag Create(
        string tenantId,
        string entityType,
        string entityId,
        string key,
        string value,
        string createdBy,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(entityType);
        Guard.Against.NullOrWhiteSpace(entityId);
        Guard.Against.NullOrWhiteSpace(key);
        Guard.Against.OutOfRange(key.Length, nameof(key), 1, MaxKeyLength);
        Guard.Against.OutOfRange(value?.Length ?? 0, nameof(value), 0, MaxValueLength);
        Guard.Against.NullOrWhiteSpace(createdBy);

        var tag = new EntityTag
        {
            Id = new EntityTagId(Guid.NewGuid()),
            TenantId = tenantId,
            EntityType = entityType.Trim().ToLower(),
            EntityId = entityId,
            Key = key.Trim().ToLower(),
            Value = value?.Trim() ?? string.Empty,
        };

        tag.SetCreated(createdAt, createdBy);
        return tag;
    }

    public void UpdateValue(string value, DateTimeOffset updatedAt)
    {
        Guard.Against.OutOfRange(value?.Length ?? 0, nameof(value), 0, MaxValueLength);
        Value = value?.Trim() ?? string.Empty;
        SetUpdated(updatedAt, CreatedBy);
    }
}
