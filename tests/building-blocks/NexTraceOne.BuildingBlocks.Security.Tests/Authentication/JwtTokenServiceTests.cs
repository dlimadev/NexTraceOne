using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authentication;

public sealed class JwtTokenServiceTests : IDisposable
{
    private const string TestSigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-validation!!";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly string? _originalEnvironment;

    public JwtTokenServiceTests()
    {
        _originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnvironment);
    }

    private static JwtTokenService CreateService(
        string? signingKey = TestSigningKey,
        string? issuer = TestIssuer,
        string? audience = TestAudience,
        int? lifetimeMinutes = null,
        DateTimeOffset? utcNow = null)
    {
        var configData = new Dictionary<string, string?>();
        if (signingKey is not null) configData["Jwt:Secret"] = signingKey;
        if (issuer is not null) configData["Jwt:Issuer"] = issuer;
        if (audience is not null) configData["Jwt:Audience"] = audience;
        if (lifetimeMinutes.HasValue) configData["Security:Jwt:AccessTokenLifetimeMinutes"] = lifetimeMinutes.Value.ToString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow ?? DateTimeOffset.UtcNow);

        return new JwtTokenService(configuration, dateTimeProvider);
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ProducesValidJwt()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var token = service.GenerateAccessToken("user-1", "user@test.com", "Test User", tenantId, ["read", "write"]);

        token.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();
        var permissions = new List<string> { "identity:users:read", "identity:users:write" };

        var token = service.GenerateAccessToken("user-42", "john@test.com", "John Doe", tenantId, permissions);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(TestIssuer);
        jwt.Audiences.Should().Contain(TestAudience);
        jwt.Subject.Should().Be("user-42");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("john@test.com");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value.Should().Be("John Doe");
        jwt.Claims.First(c => c.Type == "tenant_id").Value.Should().Be(tenantId.ToString());
        jwt.Claims.Where(c => c.Type == "permissions").Select(c => c.Value)
            .Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectExpiry()
    {
        var fixedNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(lifetimeMinutes: 30, utcNow: fixedNow);

        var token = service.GenerateAccessToken("user-1", "u@t.com", "U", Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidFrom.Should().BeCloseTo(fixedNow.UtcDateTime, TimeSpan.FromSeconds(1));
        jwt.ValidTo.Should().BeCloseTo(fixedNow.AddMinutes(30).UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        var service = CreateService();

        var token = service.GenerateAccessToken("user-1", "u@t.com", "User", Guid.NewGuid(), ["perm1"]);
        var principal = service.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ReturnsNull()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken("user-1", "u@t.com", "U", Guid.NewGuid(), []);

        var tampered = token + "tampered";

        service.ValidateToken(tampered).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithDifferentSigningKey_ReturnsNull()
    {
        var service1 = CreateService(signingKey: "key-one-that-is-long-enough-for-hmac-sha256-validation!!");
        var service2 = CreateService(signingKey: "key-two-that-is-long-enough-for-hmac-sha256-validation!!");

        var token = service1.GenerateAccessToken("user-1", "u@t.com", "U", Guid.NewGuid(), []);

        service2.ValidateToken(token).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateToken_WithNullOrEmptyToken_ReturnsNull(string? token)
    {
        var service = CreateService();

        service.ValidateToken(token!).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsNull()
    {
        var pastDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var service = CreateService(lifetimeMinutes: 1, utcNow: pastDate);

        var token = service.GenerateAccessToken("user-1", "u@t.com", "U", Guid.NewGuid(), []);

        // Token expired in 2020, validation should fail now
        service.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesNonEmptyUniqueTokens()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void Constructor_InDevelopment_WithNoKey_UsesFallbackKey()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Test",
                ["Jwt:Audience"] = "Test"
            })
            .Build();

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var service = new JwtTokenService(config, dateTimeProvider);
        var token = service.GenerateAccessToken("user", "e@t.com", "N", Guid.NewGuid(), []);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Constructor_UsesSecurityJwtSigningKeyFallback()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Jwt:SigningKey"] = TestSigningKey,
                ["Security:Jwt:Issuer"] = "FallbackIssuer",
                ["Security:Jwt:Audience"] = "FallbackAudience"
            })
            .Build();

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var service = new JwtTokenService(config, dateTimeProvider);
        var token = service.GenerateAccessToken("user", "e@t.com", "N", Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("FallbackIssuer");
    }

    [Fact]
    public void GenerateAccessToken_WithEmptyPermissions_ProducesTokenWithoutPermissionClaims()
    {
        var service = CreateService();

        var token = service.GenerateAccessToken("user-1", "u@t.com", "U", Guid.NewGuid(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Where(c => c.Type == "permissions").Should().BeEmpty();
    }
}
