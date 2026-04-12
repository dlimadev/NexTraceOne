using NexTraceOne.BuildingBlocks.Core.Tags;

namespace NexTraceOne.Configuration.Tests.Domain;

public sealed class EntityTagTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnTag()
    {
        var now = DateTimeOffset.UtcNow;
        var tag = EntityTag.Create("tenant1", "service", "svc-1", "team", "payments", "user1", now);
        Assert.Equal("team", tag.Key);
        Assert.Equal("payments", tag.Value);
        Assert.Equal("service", tag.EntityType);
    }

    [Fact]
    public void Create_WithEmptyKey_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            EntityTag.Create("tenant1", "service", "svc-1", "", "val", "user1", now));
    }

    [Fact]
    public void Create_WithKeyTooLong_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var longKey = new string('a', 51);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EntityTag.Create("tenant1", "service", "svc-1", longKey, "val", "user1", now));
    }

    [Fact]
    public void UpdateValue_ShouldChangeValue()
    {
        var now = DateTimeOffset.UtcNow;
        var tag = EntityTag.Create("tenant1", "service", "svc-1", "cost-center", "platform", "user1", now);
        tag.UpdateValue("engineering", now.AddMinutes(1));
        Assert.Equal("engineering", tag.Value);
    }

    [Fact]
    public void Create_ShouldNormalizeKeyToLowercase()
    {
        var now = DateTimeOffset.UtcNow;
        var tag = EntityTag.Create("tenant1", "service", "svc-1", "TEAM", "Payments", "user1", now);
        Assert.Equal("team", tag.Key);
    }

    [Fact]
    public void Create_ShouldNormalizeEntityTypeToLowercase()
    {
        var now = DateTimeOffset.UtcNow;
        var tag = EntityTag.Create("tenant1", "SERVICE", "svc-1", "tier", "1", "user1", now);
        Assert.Equal("service", tag.EntityType);
    }
}
