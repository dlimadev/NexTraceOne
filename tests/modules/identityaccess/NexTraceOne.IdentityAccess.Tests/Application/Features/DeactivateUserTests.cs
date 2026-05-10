using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.DeactivateUser;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler DeactivateUser.
/// Cobre: utilizador não encontrado, desactivação com sessão activa, desactivação sem sessão,
/// registo de evento de segurança em todos os caminhos de sucesso.
/// </summary>
public sealed class DeactivateUserTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (
        IUserRepository userRepo,
        ISessionRepository sessionRepo,
        ISecurityEventRepository evtRepo,
        ISecurityEventTracker evtTracker,
        DeactivateUser.Handler handler) CreateHandler()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var sessionRepo = Substitute.For<ISessionRepository>();
        var evtRepo = Substitute.For<ISecurityEventRepository>();
        var evtTracker = Substitute.For<ISecurityEventTracker>();
        var clock = new TestDateTimeProvider(FixedNow);

        var handler = new DeactivateUser.Handler(
            userRepo, sessionRepo, evtRepo, evtTracker, clock);

        return (userRepo, sessionRepo, evtRepo, evtTracker, handler);
    }

    private static User CreateActiveUser()
        => User.CreateLocal(
            Domain.ValueObjects.Email.Create("user@test.com"),
            Domain.ValueObjects.FullName.Create("Test", "User"),
            Domain.ValueObjects.HashedPassword.FromPlainText("Password123!"));

    [Fact]
    public async Task Handle_Should_ReturnError_WhenUserNotFound()
    {
        var (userRepo, _, _, _, handler) = CreateHandler();
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await handler.Handle(
            new DeactivateUser.Command(UserId, TenantId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserNotFound");
    }

    [Fact]
    public async Task Handle_Should_DeactivateUser_AndRevokeActiveSession()
    {
        var (userRepo, sessionRepo, _, _, handler) = CreateHandler();

        var user = CreateActiveUser();
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var session = Session.Create(
            user.Id,
            Domain.ValueObjects.RefreshTokenHash.Create("token-hash"),
            FixedNow.AddDays(30),
            "127.0.0.1",
            "TestAgent");
        sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await handler.Handle(
            new DeactivateUser.Command(UserId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.RevokedAt.Should().NotBeNull("active session must be revoked when user is deactivated");
    }

    [Fact]
    public async Task Handle_Should_DeactivateUser_WhenNoActiveSession()
    {
        var (userRepo, sessionRepo, _, _, handler) = CreateHandler();

        var user = CreateActiveUser();
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var result = await handler.Handle(
            new DeactivateUser.Command(UserId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_RecordSecurityEvent_WithUserDeactivatedType()
    {
        var (userRepo, sessionRepo, evtRepo, evtTracker, handler) = CreateHandler();

        var user = CreateActiveUser();
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);
        sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        await handler.Handle(
            new DeactivateUser.Command(UserId, TenantId),
            CancellationToken.None);

        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.UserDeactivated));
        evtTracker.Received(1).Track(Arg.Any<SecurityEvent>());
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyUserId()
    {
        var validator = new DeactivateUser.Validator();

        var result = validator.Validate(new DeactivateUser.Command(Guid.Empty, TenantId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(DeactivateUser.Command.UserId));
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyTenantId()
    {
        var validator = new DeactivateUser.Validator();

        var result = validator.Validate(new DeactivateUser.Command(UserId, Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(DeactivateUser.Command.TenantId));
    }
}
