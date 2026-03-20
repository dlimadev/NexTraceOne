using NexTraceOne.BuildingBlocks.Application.Context;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Context;

public sealed class ContextPropagationHeadersTests
{
    [Fact]
    public void TenantId_ShouldBeCorrectHeader()
    {
        ContextPropagationHeaders.TenantId.Should().Be("X-Tenant-Id");
    }

    [Fact]
    public void EnvironmentId_ShouldBeCorrectHeader()
    {
        ContextPropagationHeaders.EnvironmentId.Should().Be("X-Environment-Id");
    }

    [Fact]
    public void CorrelationId_ShouldBeCorrectHeader()
    {
        ContextPropagationHeaders.CorrelationId.Should().Be("X-Correlation-Id");
    }

    [Fact]
    public void PropagatedHeaders_ShouldContainAllPropagatedHeaders()
    {
        ContextPropagationHeaders.PropagatedHeaders.Should().Contain(ContextPropagationHeaders.TenantId);
        ContextPropagationHeaders.PropagatedHeaders.Should().Contain(ContextPropagationHeaders.EnvironmentId);
        ContextPropagationHeaders.PropagatedHeaders.Should().Contain(ContextPropagationHeaders.CorrelationId);
    }

    [Fact]
    public void PropagatedHeaders_ShouldNotContainRequestId()
    {
        // RequestId is NOT propagated downstream by design
        ContextPropagationHeaders.PropagatedHeaders.Should().NotContain(ContextPropagationHeaders.RequestId);
    }
}
