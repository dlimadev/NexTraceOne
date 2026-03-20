using NexTraceOne.BuildingBlocks.Application.Integrations;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Integrations;

public sealed class IntegrationBindingDescriptorTests
{
    [Fact]
    public void IntegrationBindingDescriptor_WithRequiredFields_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var binding = new IntegrationBindingDescriptor
        {
            BindingId = Guid.NewGuid(),
            TenantId = tenantId,
            IntegrationType = "kafka",
            BindingName = "primary-kafka",
            Endpoint = "kafka-qa.internal:9092",
            IsProductionBinding = false,
            IsActive = true
        };

        binding.TenantId.Should().Be(tenantId);
        binding.IntegrationType.Should().Be("kafka");
        binding.IsActive.Should().BeTrue();
        binding.IsProductionBinding.Should().BeFalse();
        binding.EnvironmentId.Should().BeNull(); // global binding
    }

    [Fact]
    public void NullResolver_ShouldReturnNullForResolve()
    {
        var resolver = new NullIntegrationContextResolver();
        var result = resolver.ResolveAsync("kafka", Guid.NewGuid(), null).Result;
        result.Should().BeNull();
    }

    [Fact]
    public void NullResolver_ShouldReturnEmptyForListActive()
    {
        var resolver = new NullIntegrationContextResolver();
        var result = resolver.ListActiveBindingsAsync(Guid.NewGuid(), null).Result;
        result.Should().BeEmpty();
    }

    [Fact]
    public void NullResolver_HasActiveBinding_ShouldBeFalse()
    {
        var resolver = new NullIntegrationContextResolver();
        var result = resolver.HasActiveBindingAsync("kafka", Guid.NewGuid(), null).Result;
        result.Should().BeFalse();
    }
}
