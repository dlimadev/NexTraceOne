using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

public sealed class ServiceCustomFieldTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnField()
    {
        var now = DateTimeOffset.UtcNow;
        var field = ServiceCustomField.Create("tenant1", "Owner Email", "Email", true, "", 1, now);
        Assert.Equal("Owner Email", field.FieldName);
        Assert.Equal("Email", field.FieldType);
        Assert.True(field.IsRequired);
    }

    [Fact]
    public void Create_WithInvalidFieldType_ShouldDefaultToText()
    {
        var now = DateTimeOffset.UtcNow;
        var field = ServiceCustomField.Create("tenant1", "Field", "Unknown", false, "", 0, now);
        Assert.Equal("Text", field.FieldType);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            ServiceCustomField.Create("tenant1", "", "Text", false, "", 0, now));
    }

    [Fact]
    public void UpdateDetails_ShouldChangeName()
    {
        var now = DateTimeOffset.UtcNow;
        var field = ServiceCustomField.Create("tenant1", "Old Name", "Text", false, "", 0, now);
        field.UpdateDetails("New Name", true, "default", 5, now.AddMinutes(1));
        Assert.Equal("New Name", field.FieldName);
        Assert.True(field.IsRequired);
        Assert.Equal(5, field.SortOrder);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            ServiceCustomField.Create("tenant1", "Field", "Text", false, "", -1, now));
    }
}
