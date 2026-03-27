using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using LogoutFeature = NexTraceOne.IdentityAccess.Application.Features.Logout.Logout;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature Logout.
/// Cobre revogação de sessão, geração de SecurityEvent e cenários de erro.
/// </summary>
public sealed class LogoutTests
{
    private readonly DateTimeOffset _now = new(2025, 03, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_RevokeSessionAndRecordEvent_When_SessionExists()
    {
        var userId = Guid.NewGuid();
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("P@ssw0rd123"));

        var session = Session.Create(
            UserId.From(userId),
            RefreshTokenHash.Create("token"),
            _now.AddDays(1),
            "127.0.0.1",
            "test-agent");

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(userId.ToString());

        var tenantId = Guid.NewGuid();
        var currentTenant = new TestCurrentTenant(tenantId);
        var sessionRepository = Substitute.For<ISessionRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        sessionRepository.GetActiveByUserIdAsync(UserId.From(userId), Arg.Any<CancellationToken>())
            .Returns(session);

        var sut = new LogoutFeature.Handler(
            currentUser, currentTenant, sessionRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(new LogoutFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.RevokedAt.Should().Be(_now);
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.LogoutPerformed));
    }

    [Fact]
    public async Task Handle_Should_RecordEventOnly_When_NoActiveSessionExists()
    {
        var userId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(userId.ToString());

        var tenantId = Guid.NewGuid();
        var currentTenant = new TestCurrentTenant(tenantId);
        var sessionRepository = Substitute.For<ISessionRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        sessionRepository.GetActiveByUserIdAsync(UserId.From(userId), Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var sut = new LogoutFeature.Handler(
            currentUser, currentTenant, sessionRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(new LogoutFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.LogoutPerformed));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_NotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);

        var currentTenant = new TestCurrentTenant(Guid.NewGuid());
        var sessionRepository = Substitute.For<ISessionRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        var sut = new LogoutFeature.Handler(
            currentUser, currentTenant, sessionRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(new LogoutFeature.Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.NotAuthenticated");
    }
}
