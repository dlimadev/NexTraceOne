using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Application.Features;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes do serviço LoginSessionCreator.
/// Valida criação de sessão, persistência no repositório e retorno de refresh token em texto plano.
/// </summary>
public sealed class LoginSessionCreatorTests
{
    [Fact]
    public void CreateSession_Should_ReturnSessionAndRefreshToken()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var sut = new LoginSessionCreator(jwtTokenGenerator, sessionRepository, new TestDateTimeProvider(now));
        var userId = UserId.New();

        jwtTokenGenerator.GenerateRefreshToken().Returns("test-refresh-token");

        var (session, refreshToken) = sut.CreateSession(userId, "127.0.0.1", "TestAgent");

        session.Should().NotBeNull();
        session.UserId.Should().Be(userId);
        refreshToken.Should().Be("test-refresh-token");
        sessionRepository.Received(1).Add(Arg.Is<Session>(s => s.UserId == userId));
    }

    [Fact]
    public void CreateSession_Should_UseDefaultValues_When_IpAndUserAgentAreNull()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var sut = new LoginSessionCreator(jwtTokenGenerator, sessionRepository, new TestDateTimeProvider(now));
        var userId = UserId.New();

        jwtTokenGenerator.GenerateRefreshToken().Returns("token");

        var (session, _) = sut.CreateSession(userId, null, null);

        session.Should().NotBeNull();
        session.CreatedByIp.Should().Be("unknown");
        session.UserAgent.Should().Be("unknown");
    }

    [Fact]
    public void CreateSession_Should_SetExpiration30DaysFromNow()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var sut = new LoginSessionCreator(jwtTokenGenerator, sessionRepository, new TestDateTimeProvider(now));
        var userId = UserId.New();

        jwtTokenGenerator.GenerateRefreshToken().Returns("token");

        var (session, _) = sut.CreateSession(userId, "ip", "ua");

        session.ExpiresAt.Should().Be(now.AddDays(30));
    }
}
