using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authentication;

public sealed class ApiKeyAuthenticationTests
{
    private const string ValidApiKey = "test-api-key-12345";
    private const string ValidClientId = "client-1";
    private const string ValidClientName = "Test Client";
    private const string ValidTenantId = "b5e1c9a0-1234-5678-9abc-def012345678";

    private static readonly List<ApiKeyConfiguration> DefaultKeys =
    [
        new()
        {
            Key = ValidApiKey,
            ClientId = ValidClientId,
            ClientName = ValidClientName,
            TenantId = ValidTenantId,
            Permissions = ["services:read", "services:write"]
        }
    ];

    private static async Task<AuthenticateResult> RunHandler(
        HttpContext httpContext,
        List<ApiKeyConfiguration>? keys = null)
    {
        var options = new ApiKeyAuthenticationOptions
        {
            ConfiguredKeys = keys ?? DefaultKeys
        };

        var optionsMonitor = Substitute.For<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        optionsMonitor.Get(ApiKeyAuthenticationOptions.SchemeName).Returns(options);
        optionsMonitor.CurrentValue.Returns(options);

        var loggerFactory = new NullLoggerFactory();
        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationOptions.SchemeName,
            ApiKeyAuthenticationOptions.SchemeName,
            typeof(ApiKeyAuthenticationHandler));

        var handler = new ApiKeyAuthenticationHandler(optionsMonitor, loggerFactory, UrlEncoder.Default);
        await handler.InitializeAsync(scheme, httpContext);

        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task HandleAuthenticate_WithValidApiKey_ReturnsSuccess()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = ValidApiKey;

        var result = await RunHandler(context);

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(ValidClientId);
        result.Principal.FindFirstValue(ClaimTypes.Name).Should().Be(ValidClientName);
        result.Principal.FindFirstValue("tenant_id").Should().Be(ValidTenantId);
        result.Principal.FindFirstValue("auth_method").Should().Be("api_key");
    }

    [Fact]
    public async Task HandleAuthenticate_WithValidApiKey_IncludesPermissionClaims()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = ValidApiKey;

        var result = await RunHandler(context);

        var permissions = result.Principal!.FindAll("permissions").Select(c => c.Value).ToList();
        permissions.Should().Contain("services:read");
        permissions.Should().Contain("services:write");
    }

    [Fact]
    public async Task HandleAuthenticate_WithInvalidApiKey_ReturnsFail()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "invalid-key";

        var result = await RunHandler(context);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task HandleAuthenticate_WithMissingHeader_ReturnsNoResult()
    {
        var context = new DefaultHttpContext();

        var result = await RunHandler(context);

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticate_WithEmptyHeaderValue_ReturnsNoResult()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "";

        var result = await RunHandler(context);

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticate_WithNoConfiguredKeys_ReturnsFail()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = ValidApiKey;

        var result = await RunHandler(context, keys: []);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAuthenticate_WithMultipleKeys_MatchesCorrectOne()
    {
        var keys = new List<ApiKeyConfiguration>
        {
            new() { Key = "key-alpha", ClientId = "alpha", ClientName = "Alpha", TenantId = "t1", Permissions = ["a"] },
            new() { Key = "key-beta", ClientId = "beta", ClientName = "Beta", TenantId = "t2", Permissions = ["b"] },
        };

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "key-beta";

        var result = await RunHandler(context, keys);

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("beta");
    }
}
