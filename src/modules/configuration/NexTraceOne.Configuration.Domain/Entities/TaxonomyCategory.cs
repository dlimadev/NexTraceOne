using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Categoria de taxonomia definida pelo admin do tenant.
/// Exemplos: "Business Domain", "Data Classification", "Tier".
/// </summary>
public sealed class TaxonomyCategory : AuditableEntity<TaxonomyCategoryId>
{
    private const int MaxNameLength = 80;

    private TaxonomyCategory() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsRequired { get; private set; }

    public static TaxonomyCategory Create(
        string tenantId,
        string name,
        string description,
        bool isRequired,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);

        var category = new TaxonomyCategory
        {
            Id = new TaxonomyCategoryId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            Description = description ?? string.Empty,
            IsRequired = isRequired,
        };

        category.SetCreated(createdAt, tenantId);
        return category;
    }

    public void UpdateDetails(string name, string description, bool isRequired, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Name = name;
        Description = description ?? string.Empty;
        IsRequired = isRequired;
        SetUpdated(updatedAt, CreatedBy);
    }
}
