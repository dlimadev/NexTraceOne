using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authentication;

public sealed class HttpContextCurrentUserTests
{
    private static HttpContextCurrentUser CreateUser(ClaimsPrincipal? principal = null)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        if (principal is not null)
        {
            httpContext.User = principal;
        }
        accessor.HttpContext.Returns(httpContext);
        return new HttpContextCurrentUser(accessor);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string? sub = "user-1",
        string? name = "Test User",
        string? email = "test@example.com",
        IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim>();
        if (sub is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
        if (name is not null) claims.Add(new Claim(ClaimTypes.Name, name));
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));

        if (permissions is not null)
        {
            claims.AddRange(permissions.Select(p => new Claim("permissions", p)));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void Id_WithAuthenticatedUser_ReturnsNameIdentifier()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(sub: "user-42"));

        user.Id.Should().Be("user-42");
    }

    [Fact]
    public void Name_WithAuthenticatedUser_ReturnsName()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(name: "John Doe"));

        user.Name.Should().Be("John Doe");
    }

    [Fact]
    public void Email_WithAuthenticatedUser_ReturnsEmail()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(email: "john@test.com"));

        user.Email.Should().Be("john@test.com");
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal());

        user.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var user = CreateUser(principal);

        user.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WithMatchingPermission_ReturnsTrue()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(permissions: ["services:read", "services:write"]));

        user.HasPermission("services:read").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WithCaseInsensitiveMatch_ReturnsTrue()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(permissions: ["Services:Read"]));

        user.HasPermission("services:read").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WithMissingPermission_ReturnsFalse()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(permissions: ["services:read"]));

        user.HasPermission("services:write").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WithNoPermissions_ReturnsFalse()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal(permissions: []));

        user.HasPermission("services:read").Should().BeFalse();
    }

    [Fact]
    public void Id_WithNoHttpContext_ReturnsEmpty()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var user = new HttpContextCurrentUser(accessor);

        user.Id.Should().BeEmpty();
    }

    [Fact]
    public void HasPermission_ViaPermissionClaim_ReturnsTrue()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new("permission", "admin:manage")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var user = CreateUser(principal);

        user.HasPermission("admin:manage").Should().BeTrue();
    }

    [Fact]
    public void Persona_WithPersonaClaim_ReturnsPersonaValue()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new("x-nxt-persona", "TechLead")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var user = CreateUser(principal);

        user.Persona.Should().Be("TechLead");
    }

    [Fact]
    public void Persona_WithoutPersonaClaim_ReturnsNull()
    {
        var user = CreateUser(CreateAuthenticatedPrincipal());

        user.Persona.Should().BeNull();
    }

    [Fact]
    public void Persona_WithNoHttpContext_ReturnsNull()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var user = new HttpContextCurrentUser(accessor);

        user.Persona.Should().BeNull();
    }
}
