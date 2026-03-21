using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Context;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Context;

public sealed class DistributedExecutionContextTests
{
    [Fact]
    public void Constructor_WithNullCorrelationId_ShouldGenerateCorrelationId()
    {
        var ctx = new DistributedExecutionContext(tenantId: Guid.NewGuid());
        ctx.CorrelationId.Should().NotBeNullOrWhiteSpace();
        ctx.CorrelationId.Should().HaveLength(32); // Guid.NewGuid().ToString("N")
    }

    [Fact]
    public void Constructor_WithExplicitValues_ShouldPreserveAllValues()
    {
        var tenantId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var corrId = "test-correlation";

        var ctx = new DistributedExecutionContext(
            tenantId: tenantId,
            environmentId: envId,
            correlationId: corrId,
            userId: "user-1",
            serviceOrigin: "TestModule");

        ctx.TenantId.Should().Be(tenantId);
        ctx.EnvironmentId.Should().Be(envId);
        ctx.CorrelationId.Should().Be(corrId);
        ctx.UserId.Should().Be("user-1");
        ctx.ServiceOrigin.Should().Be("TestModule");
    }

    [Fact]
    public void IsOperational_WhenTenantIdPresent_ShouldBeTrue()
    {
        var ctx = new DistributedExecutionContext(tenantId: Guid.NewGuid());
        ctx.IsOperational.Should().BeTrue();
    }

    [Fact]
    public void IsOperational_WhenTenantIdNull_ShouldBeFalse()
    {
        var ctx = new DistributedExecutionContext(tenantId: null);
        ctx.IsOperational.Should().BeFalse();
    }

    [Fact]
    public void IsOperational_WhenTenantIdEmpty_ShouldBeFalse()
    {
        var ctx = new DistributedExecutionContext(tenantId: Guid.Empty);
        ctx.IsOperational.Should().BeFalse();
    }

    [Fact]
    public void From_WithResolvedEnvironment_ShouldPopulateBothIds()
    {
        var tenantId = Guid.NewGuid();
        var envId = Guid.NewGuid();

        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(tenantId);

        var environment = Substitute.For<ICurrentEnvironment>();
        environment.IsResolved.Returns(true);
        environment.EnvironmentId.Returns(envId);

        var ctx = DistributedExecutionContext.From(tenant, environment);

        ctx.TenantId.Should().Be(tenantId);
        ctx.EnvironmentId.Should().Be(envId);
    }

    [Fact]
    public void From_WithUnresolvedEnvironment_ShouldHaveNullEnvironmentId()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());

        var environment = Substitute.For<ICurrentEnvironment>();
        environment.IsResolved.Returns(false);

        var ctx = DistributedExecutionContext.From(tenant, environment);

        ctx.EnvironmentId.Should().BeNull();
    }

    [Fact]
    public void From_WithEmptyTenant_ShouldHaveNullTenantId()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.Empty);

        var environment = Substitute.For<ICurrentEnvironment>();
        environment.IsResolved.Returns(false);

        var ctx = DistributedExecutionContext.From(tenant, environment);

        ctx.TenantId.Should().BeNull();
    }
}
