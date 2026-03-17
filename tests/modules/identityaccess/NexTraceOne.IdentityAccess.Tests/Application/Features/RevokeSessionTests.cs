using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using RevokeSessionFeature = NexTraceOne.IdentityAccess.Application.Features.RevokeSession.RevokeSession;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature RevokeSession.
/// </summary>
public sealed class RevokeSessionTests
{
    [Fact]
    public async Task Handle_Should_RevokeSession_When_SessionExists()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var session = Session.Create(user.Id, RefreshTokenHash.Create("token"), now.AddDays(1), "127.0.0.1", "test");

        var sessionRepository = Substitute.For<ISessionRepository>();
        sessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var sut = new RevokeSessionFeature.Handler(sessionRepository, new TestDateTimeProvider(now));

        var result = await sut.Handle(new RevokeSessionFeature.Command(session.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Verifica que a sessão foi revogada
        session.IsActive(now).Should().BeFalse();
        session.RevokedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_SessionDoesNotExist()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var sessionId = Guid.NewGuid();

        var sessionRepository = Substitute.For<ISessionRepository>();
        sessionRepository.GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>()).Returns((Session?)null);

        var sut = new RevokeSessionFeature.Handler(sessionRepository, new TestDateTimeProvider(now));

        var result = await sut.Handle(new RevokeSessionFeature.Command(sessionId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.NotFound");
    }
}
