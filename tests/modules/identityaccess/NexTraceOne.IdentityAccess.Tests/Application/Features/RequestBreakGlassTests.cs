using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.RequestBreakGlass;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler RequestBreakGlass.
/// Cobre: activação sem MFA, step-up MFA obrigatório, código MFA inválido, quota excedida.
/// </summary>
public sealed class RequestBreakGlassTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (
        IBreakGlassRepository bgRepo,
        IUserRepository userRepo,
        ISecurityEventRepository evtRepo,
        ISecurityEventTracker evtTracker,
        INotificationModule notifications,
        ITotpVerifier totp,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        RequestBreakGlass.Handler handler) CreateHandler()
    {
        var bgRepo = Substitute.For<IBreakGlassRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var evtRepo = Substitute.For<ISecurityEventRepository>();
        var evtTracker = Substitute.For<ISecurityEventTracker>();
        var notifications = Substitute.For<INotificationModule>();
        var totp = Substitute.For<ITotpVerifier>();
        var clock = new TestDateTimeProvider(FixedNow);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(UserId.ToString());

        var currentTenant = new TestCurrentTenant(TenantId);

        var handler = new RequestBreakGlass.Handler(
            bgRepo, userRepo, evtRepo, evtTracker,
            notifications, totp, currentUser, currentTenant, clock);

        return (bgRepo, userRepo, evtRepo, evtTracker, notifications, totp, currentUser, currentTenant, handler);
    }

    private static User CreateActiveUserWithMfa(bool mfaEnabled, string? mfaSecret = null)
    {
        var email = Domain.ValueObjects.Email.Create($"user-{Guid.NewGuid():N}@test.com");
        var fullName = Domain.ValueObjects.FullName.Create("Test", "User");
        var hashedPwd = Domain.ValueObjects.HashedPassword.FromPlainText("Password123!");
        var user = User.CreateLocal(email, fullName, hashedPwd);
        if (mfaEnabled)
            user.EnableMfa("TOTP", mfaSecret ?? "JBSWY3DPEHPK3PXP");
        return user;
    }

    [Fact]
    public async Task Handle_Should_ActivateBreakGlass_WhenUserHasNoMfa()
    {
        var (bgRepo, userRepo, _, _, _, _, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: false);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        bgRepo.CountQuarterlyUsageAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var cmd = new RequestBreakGlass.Command(
            "Emergency access needed for production incident investigation.",
            MfaCode: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuarterlyUsageCount.Should().Be(1);
        result.Value.ExpiresAt.Should().BeAfter(FixedNow);
        bgRepo.Received(1).Add(Arg.Any<BreakGlassRequest>());
    }

    [Fact]
    public async Task Handle_Should_ReturnMfaStepUpRequired_WhenUserHasMfaButNoCodeProvided()
    {
        var (_, userRepo, evtRepo, _, _, _, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: true);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new RequestBreakGlass.Command(
            "Emergency access needed for production incident.",
            MfaCode: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.mfaStepUpRequired");
        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.StepUpMfaRequired));
    }

    [Fact]
    public async Task Handle_Should_ReturnMfaCodeInvalid_WhenTotpFails()
    {
        var (_, userRepo, evtRepo, _, _, totp, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: true, mfaSecret: "JBSWY3DPEHPK3PXP");
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        totp.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var cmd = new RequestBreakGlass.Command(
            "Emergency access needed for incident resolution.",
            MfaCode: "000000");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.mfaCodeInvalid");
        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.MfaStepUpDenied));
    }

    [Fact]
    public async Task Handle_Should_ActivateBreakGlass_WhenMfaVerifies()
    {
        var (bgRepo, userRepo, _, _, _, totp, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: true, mfaSecret: "JBSWY3DPEHPK3PXP");
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        totp.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        bgRepo.CountQuarterlyUsageAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(1);

        var cmd = new RequestBreakGlass.Command(
            "MFA-verified emergency access for security incident.",
            MfaCode: "123456");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuarterlyUsageCount.Should().Be(2);
        bgRepo.Received(1).Add(Arg.Any<BreakGlassRequest>());
    }

    [Fact]
    public async Task Handle_Should_ReturnQuotaExceeded_WhenLimitReached()
    {
        var (bgRepo, userRepo, _, _, _, _, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: false);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        bgRepo.CountQuarterlyUsageAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BreakGlassRequest.QuarterlyUsageLimit);

        var cmd = new RequestBreakGlass.Command("Emergency access justification text for testing.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.breakGlassQuotaExceeded");
        bgRepo.DidNotReceive().Add(Arg.Any<BreakGlassRequest>());
    }

    [Fact]
    public async Task Handle_Should_ReturnUserNotFound_WhenUserIsInactive()
    {
        var (_, userRepo, _, _, _, _, _, _, handler) = CreateHandler();

        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var cmd = new RequestBreakGlass.Command("Emergency access justification text here.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.userNotFound");
    }

    [Fact]
    public async Task Handle_Should_SendNotification_AfterSuccessfulActivation()
    {
        var (bgRepo, userRepo, _, _, notifications, _, _, _, handler) = CreateHandler();

        var user = CreateActiveUserWithMfa(mfaEnabled: false);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        bgRepo.CountQuarterlyUsageAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var cmd = new RequestBreakGlass.Command(
            "Emergency access required for critical production issue resolution.",
            IpAddress: "192.168.1.1",
            UserAgent: "TestAgent/1.0");

        await handler.Handle(cmd, CancellationToken.None);

        await notifications.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(n =>
                n.EventType == "BreakGlassActivated" &&
                n.Severity == "Critical"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_WhenCurrentUserIdIsEmpty()
    {
        var (_, _, _, _, _, _, currentUser, _, handler) = CreateHandler();

        currentUser.Id.Returns(string.Empty);

        var cmd = new RequestBreakGlass.Command("Emergency access justification text here.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.notAuthenticated");
    }
}
