using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Infrastructure.Middleware;
using NSubstitute;
using System.Security.Claims;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Middleware;

/// <summary>
/// Testes unitários para EnvironmentAuthorizationMiddleware (W5-05).
/// </summary>
public class EnvironmentAuthorizationMiddlewareTests
{
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IEnvironmentAccessPolicyRepository _policyRepository = Substitute.For<IEnvironmentAccessPolicyRepository>();
    private readonly IJitAccessRequestRepository _jitRepository = Substitute.For<IJitAccessRequestRepository>();
    private readonly ILogger<EnvironmentAuthorizationMiddleware> _logger = Substitute.For<ILogger<EnvironmentAuthorizationMiddleware>>();
    private readonly DefaultHttpContext _httpContext;

    public EnvironmentAuthorizationMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoTenant_ShouldPassThrough()
    {
        // Arrange
        _currentTenant.Id.Returns(Guid.Empty);
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
        _policyRepository.DidNotReceiveWithAnyArgs().ListByTenantAsync(default, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserNotAuthenticated_ShouldPassThrough()
    {
        // Arrange
        _currentTenant.Id.Returns(Guid.NewGuid());
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Não autenticado
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoEnvironmentHeader_ShouldPassThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.User = CreateAuthenticatedUser("user1", new[] { "Developer" });
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
        _policyRepository.DidNotReceiveWithAnyArgs().ListByTenantAsync(default, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoPolicyForEnvironment_ShouldPassThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser("user1", new[] { "Developer" });
        
        _policyRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentAccessPolicy>()); // Nenhuma política
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserHasAllowedRole_ShouldPassThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser("user1", new[] { "Developer" });
        
        var policy = CreatePolicy("Prod-Policy", tenantId, 
            environments: new[] { "Production" }, 
            allowedRoles: new[] { "Developer", "TechLead" },
            requireJitRoles: Array.Empty<string>());
        
        _policyRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { policy });
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserDoesNotHaveAllowedRole_ShouldReturn403()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser("user1", new[] { "Intern" }); // Role não permitida
        
        var policy = CreatePolicy("Prod-Policy", tenantId, 
            environments: new[] { "Production" }, 
            allowedRoles: new[] { "Developer", "TechLead" },
            requireJitRoles: Array.Empty<string>());
        
        _policyRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { policy });
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; }; // Não deve ser chamado
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_WhenRoleRequiresJitAndNoActiveSession_ShouldCreateJitRequestAndReturn403()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser(userId.ToString(), new[] { "PlatformAdmin" });
        
        var policy = CreatePolicy("Prod-Policy", tenantId, 
            environments: new[] { "Production" }, 
            allowedRoles: new[] { "PlatformAdmin" },
            requireJitRoles: new[] { "PlatformAdmin" }); // Requer JIT
        
        _policyRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { policy });
        
        _jitRepository.ListActiveByUserAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<JitAccessRequest>()); // Sem sessão JIT activa
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeFalse();
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _jitRepository.Received(1).AddAsync(Arg.Any<JitAccessRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenRoleRequiresJitAndHasActiveSession_ShouldPassThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser(userId.ToString(), new[] { "PlatformAdmin" });
        
        var policy = CreatePolicy("Prod-Policy", tenantId, 
            environments: new[] { "Production" }, 
            allowedRoles: new[] { "PlatformAdmin" },
            requireJitRoles: new[] { "PlatformAdmin" });
        
        _policyRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new[] { policy });
        
        // Simular sessão JIT activa
        var activeJitRequest = JitAccessRequest.Create(
            new UserId(userId),
            new TenantId(tenantId),
            "environment:Production:PlatformAdmin",
            "Test scope",
            "Test justification",
            DateTimeOffset.UtcNow.AddHours(-1));
        activeJitRequest.Approve(new UserId(Guid.NewGuid()), DateTimeOffset.UtcNow.AddHours(-1));
        
        _jitRepository.ListActiveByUserAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { activeJitRequest });
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserHasNoRoles_ShouldPassThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);
        _httpContext.Request.Headers[EnvironmentAuthorizationMiddleware.EnvironmentHeader] = "Production";
        _httpContext.User = CreateAuthenticatedUser("user1", Array.Empty<string>()); // Sem roles
        
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new EnvironmentAuthorizationMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_httpContext, _currentTenant, _policyRepository, _jitRepository);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // Helper methods

    private static ClaimsPrincipal CreateAuthenticatedUser(string userId, string[] roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"User {userId}")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static EnvironmentAccessPolicy CreatePolicy(
        string name,
        Guid tenantId,
        string[] environments,
        string[] allowedRoles,
        string[] requireJitRoles)
    {
        var policy = EnvironmentAccessPolicy.Create(
            name,
            tenantId,
            environments.ToList(),
            allowedRoles.ToList(),
            requireJitRoles.ToList(),
            jitApprovalRequiredFrom: null,
            now: DateTimeOffset.UtcNow);

        // Usar reflection para definir o ID já que é protected
        var idProperty = typeof(EnvironmentAccessPolicy).GetProperty("Id");
        idProperty?.SetValue(policy, EnvironmentAccessPolicyId.New());

        return policy;
    }
}
