using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

public sealed class TaxonomyTests
{
    [Fact]
    public void CreateCategory_WithValidData_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var cat = TaxonomyCategory.Create("tenant1", "Business Domain", "Domain classification", false, now);
        Assert.Equal("Business Domain", cat.Name);
        Assert.False(cat.IsRequired);
    }

    [Fact]
    public void CreateCategory_WithEmptyName_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            TaxonomyCategory.Create("tenant1", "", "Desc", false, now));
    }

    [Fact]
    public void CreateValue_WithValidData_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var categoryId = new TaxonomyCategoryId(Guid.NewGuid());
        var value = TaxonomyValue.Create(categoryId, "tenant1", "Tier 1", 0, now);
        Assert.Equal("Tier 1", value.Label);
        Assert.Equal(categoryId.Value, value.CategoryId.Value);
    }

    [Fact]
    public void UpdateCategory_ShouldChangeDetails()
    {
        var now = DateTimeOffset.UtcNow;
        var cat = TaxonomyCategory.Create("tenant1", "Old Name", "Old Desc", false, now);
        cat.UpdateDetails("New Name", "New Desc", true, now.AddMinutes(1));
        Assert.Equal("New Name", cat.Name);
        Assert.True(cat.IsRequired);
    }
}
