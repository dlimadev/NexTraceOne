using FluentAssertions;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;

namespace NexTraceOne.BuildingBlocks.Security.Tests.MultiTenancy;

public sealed class CurrentTenantAccessorTests
{
    [Fact]
    public void Default_HasEmptyState()
    {
        var accessor = new CurrentTenantAccessor();

        accessor.Id.Should().Be(Guid.Empty);
        accessor.Slug.Should().BeEmpty();
        accessor.Name.Should().BeEmpty();
        accessor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Set_UpdatesAllProperties()
    {
        var accessor = new CurrentTenantAccessor();
        var tenantId = Guid.NewGuid();

        accessor.Set(tenantId, "acme-corp", "Acme Corporation", isActive: true);

        accessor.Id.Should().Be(tenantId);
        accessor.Slug.Should().Be("acme-corp");
        accessor.Name.Should().Be("Acme Corporation");
        accessor.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Set_WithCapabilities_CanCheckCapabilities()
    {
        var accessor = new CurrentTenantAccessor();

        accessor.Set(Guid.NewGuid(), "t", "T", true, capabilities: ["feature-a", "feature-b"]);

        accessor.HasCapability("feature-a").Should().BeTrue();
        accessor.HasCapability("feature-b").Should().BeTrue();
        accessor.HasCapability("feature-c").Should().BeFalse();
    }

    [Fact]
    public void Set_CalledTwice_ReplacesState()
    {
        var accessor = new CurrentTenantAccessor();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        accessor.Set(id1, "first", "First", true, ["cap-1"]);
        accessor.Set(id2, "second", "Second", false, ["cap-2"]);

        accessor.Id.Should().Be(id2);
        accessor.Slug.Should().Be("second");
        accessor.Name.Should().Be("Second");
        accessor.IsActive.Should().BeFalse();
        accessor.HasCapability("cap-1").Should().BeFalse();
        accessor.HasCapability("cap-2").Should().BeTrue();
    }

    [Fact]
    public void Set_WithNullCapabilities_ClearsExisting()
    {
        var accessor = new CurrentTenantAccessor();
        accessor.Set(Guid.NewGuid(), "t", "T", true, ["cap-1"]);

        accessor.Set(Guid.NewGuid(), "t2", "T2", true, capabilities: null);

        accessor.HasCapability("cap-1").Should().BeFalse();
    }

    [Fact]
    public void HasCapability_WithEmptyCapabilities_ReturnsFalse()
    {
        var accessor = new CurrentTenantAccessor();
        accessor.Set(Guid.NewGuid(), "t", "T", true, capabilities: []);

        accessor.HasCapability("anything").Should().BeFalse();
    }
}
