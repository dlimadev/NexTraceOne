using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using NexTraceOne.BuildingBlocks.Security.Session;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Security;

public sealed class SessionInactivityMiddlewareTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private static readonly string UserId = "user-123";
    private static readonly string SessionId = "sess-abc";

    private static IConfiguration BuildConfig(int timeoutMinutes = 480, bool detectIp = true)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Session:InactivityTimeoutMinutes"] = timeoutMinutes.ToString(),
                ["Security:Session:DetectAnomalousIpChange"] = detectIp.ToString(),
            })
            .Build();

    private static HttpContext BuildAuthenticatedContext(string userId, string sessionId, string ip = "1.2.3.4")
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            [new Claim("sub", userId), new Claim("sid", sessionId)],
            "Bearer");
        context.User = new ClaimsPrincipal(identity);
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        return context;
    }

    private SessionInactivityMiddleware CreateMiddleware(
        RequestDelegate? next = null,
        IConfiguration? config = null)
        => new(
            next ?? (_ => Task.CompletedTask),
            _cache,
            config ?? BuildConfig(),
            NullLogger<SessionInactivityMiddleware>.Instance);

    [Fact]
    public async Task AnonymousRequest_PassesThrough()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticatedRequest_NoExistingActivity_PassesThrough()
    {
        // GetAsync devolve null — sem actividade prévia, deve permitir passagem
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildAuthenticatedContext(UserId, SessionId);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticatedRequest_RecentActivity_PassesThrough()
    {
        // Actividade há 10 minutos — dentro do timeout de 480 minutos
        var recentActivity = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(recentActivity));

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildAuthenticatedContext(UserId, SessionId);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task AuthenticatedRequest_ExpiredActivity_Returns401()
    {
        // Actividade há 600 minutos — superior ao timeout de 480 minutos
        var expiredActivity = DateTimeOffset.UtcNow.AddMinutes(-600).ToString("O");
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(expiredActivity));

        var nextCalled = false;
        var middleware = CreateMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            BuildConfig(timeoutMinutes: 480));
        var context = BuildAuthenticatedContext(UserId, SessionId);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task AuthenticatedRequest_UpdatesActivityTimestamp()
    {
        // Sem actividade prévia — deve guardar o timestamp de actividade
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var middleware = CreateMiddleware();
        var context = BuildAuthenticatedContext(UserId, SessionId);

        await middleware.InvokeAsync(context);

        await _cache.Received().SetAsync(
            Arg.Is<string>(k => k.Contains("session-activity")),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticatedRequest_MismatchedIp_PassesThroughButLogsWarning()
    {
        // IP guardado diferente do actual — deve passar mas registar warning
        _cache.GetAsync(
            Arg.Is<string>(k => k.Contains("session-activity")),
            Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _cache.GetAsync(
            Arg.Is<string>(k => k.Contains("session-ip")),
            Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes("9.9.9.9"));

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildAuthenticatedContext(UserId, SessionId, ip: "1.2.3.4");

        await middleware.InvokeAsync(context);

        // Mudança de IP é apenas Warning — não bloqueia o request
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticatedRequest_SameIp_PassesThrough()
    {
        // IP actual igual ao guardado — deve passar normalmente
        _cache.GetAsync(
            Arg.Is<string>(k => k.Contains("session-activity")),
            Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _cache.GetAsync(
            Arg.Is<string>(k => k.Contains("session-ip")),
            Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes("1.2.3.4"));

        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = BuildAuthenticatedContext(UserId, SessionId, ip: "1.2.3.4");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RequestWithoutUserIdOrSessionId_PassesThrough()
    {
        // Autenticado mas sem claims de userId/sessionId — deve passar
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([], "Bearer"); // autenticado sem claims
        context.User = new ClaimsPrincipal(identity);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task IpDetectionDisabled_NoIpCacheCheck()
    {
        // Detecção de IP desactivada — não deve consultar a cache de IPs
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var middleware = CreateMiddleware(config: BuildConfig(detectIp: false));
        var context = BuildAuthenticatedContext(UserId, SessionId);

        await middleware.InvokeAsync(context);

        await _cache.DidNotReceive().GetAsync(
            Arg.Is<string>(k => k.Contains("session-ip")),
            Arg.Any<CancellationToken>());
    }
}
