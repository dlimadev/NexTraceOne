using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Security.CookieSession;

namespace NexTraceOne.BuildingBlocks.Security.Tests.CookieSession;

public sealed class CsrfTokenValidatorTests
{
    private static CookieSessionOptions DefaultOptions => new()
    {
        Enabled = true,
        AccessTokenCookieName = "nxt_at",
        CsrfCookieName = "nxt_csrf",
        CsrfHeaderName = "X-Csrf-Token"
    };

    private static HttpContext CreateHttpContext(
        string method = "POST",
        string? authCookieValue = null,
        string? csrfCookieValue = null,
        string? csrfHeaderValue = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;

        if (authCookieValue is not null || csrfCookieValue is not null)
        {
            var cookies = new List<string>();
            if (authCookieValue is not null)
                cookies.Add($"nxt_at={authCookieValue}");
            if (csrfCookieValue is not null)
                cookies.Add($"nxt_csrf={csrfCookieValue}");
            context.Request.Headers["Cookie"] = string.Join("; ", cookies);
        }

        if (csrfHeaderValue is not null)
        {
            context.Request.Headers["X-Csrf-Token"] = csrfHeaderValue;
        }

        return context;
    }

    [Fact]
    public void Generate_ProducesNonEmptyToken()
    {
        var token = CsrfTokenValidator.Generate();

        token.Should().NotBeNullOrWhiteSpace();
        token.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Generate_ProducesUniqueTokens()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => CsrfTokenValidator.Generate()).ToHashSet();

        tokens.Should().HaveCount(50);
    }

    [Fact]
    public void Generate_ProducesUrlSafeToken()
    {
        var token = CsrfTokenValidator.Generate();

        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public void IsValid_ForSafeMethods_ReturnsTrue(string method)
    {
        var context = CreateHttpContext(method: method);

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithNoAuthCookie_ReturnsTrue()
    {
        var context = CreateHttpContext(method: "POST");

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithMatchingTokens_ReturnsTrue()
    {
        var csrfToken = CsrfTokenValidator.Generate();
        var context = CreateHttpContext(
            method: "POST",
            authCookieValue: "some-jwt-token",
            csrfCookieValue: csrfToken,
            csrfHeaderValue: csrfToken);

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithMismatchedTokens_ReturnsFalse()
    {
        var context = CreateHttpContext(
            method: "POST",
            authCookieValue: "some-jwt-token",
            csrfCookieValue: "token-in-cookie",
            csrfHeaderValue: "different-token-in-header");

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMissingCsrfHeader_ReturnsFalse()
    {
        var context = CreateHttpContext(
            method: "POST",
            authCookieValue: "some-jwt-token",
            csrfCookieValue: "some-csrf-token");

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMissingCsrfCookie_ReturnsFalse()
    {
        var context = CreateHttpContext(
            method: "POST",
            authCookieValue: "some-jwt-token",
            csrfHeaderValue: "some-csrf-token");

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeFalse();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void IsValid_UnsafeMethods_RequireCsrf(string method)
    {
        var csrfToken = CsrfTokenValidator.Generate();
        var context = CreateHttpContext(
            method: method,
            authCookieValue: "jwt-token",
            csrfCookieValue: csrfToken,
            csrfHeaderValue: csrfToken);

        CsrfTokenValidator.IsValid(context, DefaultOptions).Should().BeTrue();
    }

    [Fact]
    public void ApplyCookies_SetsAccessTokenAndCsrfCookies()
    {
        var context = new DefaultHttpContext();
        var options = DefaultOptions;

        var csrfToken = CsrfTokenValidator.ApplyCookies(context.Response, "test-jwt-token", options);

        csrfToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ClearCookies_DoesNotThrow()
    {
        var context = new DefaultHttpContext();

        var act = () => CsrfTokenValidator.ClearCookies(context.Response, DefaultOptions);

        act.Should().NotThrow();
    }
}
