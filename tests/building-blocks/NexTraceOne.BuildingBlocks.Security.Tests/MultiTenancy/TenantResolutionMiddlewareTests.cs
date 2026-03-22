using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.MultiTenancy;

public sealed class TenantResolutionMiddlewareTests
{
    private static (TenantResolutionMiddleware Middleware, CurrentTenantAccessor Accessor) CreateMiddleware(
        bool nextCalled = true)
    {
        var wasCalled = false;
        RequestDelegate next = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };
        var logger = Substitute.For<ILogger<TenantResolutionMiddleware>>();
        var middleware = new TenantResolutionMiddleware(next, logger);
        var accessor = new CurrentTenantAccessor();
        return (middleware, accessor);
    }

    [Fact]
    public async Task InvokeAsync_WithTenantIdHeader_ResolvesFromHeader()
    {
        var (middleware, accessor) = CreateMiddleware();
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(tenantId);
        accessor.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithJwtTenantClaim_ResolvesFromJwt()
    {
        var (middleware, accessor) = CreateMiddleware();
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();

        var claims = new List<Claim> { new("tenant_id", tenantId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task InvokeAsync_JwtTakesPriorityOverHeader()
    {
        var (middleware, accessor) = CreateMiddleware();
        var jwtTenantId = Guid.NewGuid();
        var headerTenantId = Guid.NewGuid();

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = headerTenantId.ToString();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("tenant_id", jwtTenantId.ToString())], "Test"));

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(jwtTenantId, "JWT claim should take priority over header");
    }

    [Fact]
    public async Task InvokeAsync_WithNoTenantInfo_LeavesAccessorEmpty()
    {
        var (middleware, accessor) = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(Guid.Empty);
        accessor.Slug.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidHeaderGuid_DoesNotResolve()
    {
        var (middleware, accessor) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "not-a-guid";

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyGuidHeader_DoesNotResolve()
    {
        var (middleware, accessor) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = Guid.Empty.ToString();

        await middleware.InvokeAsync(context, accessor);

        // Guid.TryParse succeeds for Guid.Empty — the middleware sets it but
        // downstream TenantIsolationBehavior rejects Guid.Empty as "no tenant".
        accessor.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task InvokeAsync_WithSubdomain_ResolvesFromHost()
    {
        var (middleware, accessor) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.app.example.com");

        await middleware.InvokeAsync(context, accessor);

        accessor.Slug.Should().Be("acme");
        accessor.Id.Should().NotBe(Guid.Empty);
        accessor.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithTwoPartHost_DoesNotResolveSubdomain()
    {
        var (middleware, accessor) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("example.com");

        await middleware.InvokeAsync(context, accessor);

        accessor.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task InvokeAsync_SameSubdomain_ProducesDeterministicGuid()
    {
        var (middleware1, accessor1) = CreateMiddleware();
        var (middleware2, accessor2) = CreateMiddleware();

        var context1 = new DefaultHttpContext();
        context1.Request.Host = new HostString("tenant-a.app.example.com");
        await middleware1.InvokeAsync(context1, accessor1);

        var context2 = new DefaultHttpContext();
        context2.Request.Host = new HostString("tenant-a.app.example.com");
        await middleware2.InvokeAsync(context2, accessor2);

        accessor1.Id.Should().Be(accessor2.Id, "same subdomain should produce same GUID");
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var logger = Substitute.For<ILogger<TenantResolutionMiddleware>>();
        var middleware = new TenantResolutionMiddleware(next, logger);
        var accessor = new CurrentTenantAccessor();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, accessor);

        nextCalled.Should().BeTrue();
    }
}
