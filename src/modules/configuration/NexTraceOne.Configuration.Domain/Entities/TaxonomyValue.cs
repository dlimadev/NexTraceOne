using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Valor dentro de uma categoria de taxonomia.
/// Exemplos: Tier → "Tier 1", "Tier 2", "Tier 3".
/// </summary>
public sealed class TaxonomyValue : AuditableEntity<TaxonomyValueId>
{
    private const int MaxLabelLength = 100;

    private TaxonomyValue() { }

    public TaxonomyCategoryId CategoryId { get; private set; } = default!;
    public string TenantId { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static TaxonomyValue Create(
        TaxonomyCategoryId categoryId,
        string tenantId,
        string label,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Guard.Against.Null(categoryId);
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(label);
        Guard.Against.OutOfRange(label.Length, nameof(label), 1, MaxLabelLength);
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var value = new TaxonomyValue
        {
            Id = new TaxonomyValueId(Guid.NewGuid()),
            CategoryId = categoryId,
            TenantId = tenantId,
            Label = label,
            SortOrder = sortOrder,
        };

        value.SetCreated(createdAt, tenantId);
        return value;
    }
}
