using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.RequestJitAccess;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler RequestJitAccess.
/// Cobre: acesso sem MFA, step-up MFA obrigatório, código MFA inválido, acesso concedido com MFA.
/// </summary>
public sealed class RequestJitAccessTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (
        IJitAccessRepository jitRepo,
        IUserRepository userRepo,
        ISecurityEventRepository evtRepo,
        ISecurityEventTracker evtTracker,
        ITotpVerifier totp,
        ICurrentUser currentUser,
        RequestJitAccess.Handler handler) CreateHandler()
    {
        var jitRepo = Substitute.For<IJitAccessRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var evtRepo = Substitute.For<ISecurityEventRepository>();
        var evtTracker = Substitute.For<ISecurityEventTracker>();
        var totp = Substitute.For<ITotpVerifier>();
        var clock = new TestDateTimeProvider(FixedNow);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(UserId.ToString());

        var currentTenant = new TestCurrentTenant(TenantId);

        var handler = new RequestJitAccess.Handler(
            jitRepo, userRepo, evtRepo, evtTracker,
            totp, currentUser, currentTenant, clock);

        return (jitRepo, userRepo, evtRepo, evtTracker, totp, currentUser, handler);
    }

    private static User CreateLocalUser(bool mfaEnabled = false, string? mfaSecret = null)
    {
        var user = User.CreateLocal(
            Email.Create($"user-{Guid.NewGuid():N}@test.com"),
            FullName.Create("Test", "User"),
            HashedPassword.FromPlainText("Password123!"));
        if (mfaEnabled)
            user.EnableMfa("TOTP", mfaSecret ?? "JBSWY3DPEHPK3PXP");
        return user;
    }

    [Fact]
    public async Task Handle_Should_CreateJitRequest_WhenUserHasNoMfa()
    {
        var (jitRepo, userRepo, _, _, _, _, handler) = CreateHandler();

        var user = CreateLocalUser(mfaEnabled: false);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new RequestJitAccess.Command(
            "deploy:production",
            "service:payment-gateway",
            "Need to deploy hotfix for critical payment issue.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestId.Should().NotBeEmpty();
        result.Value.ApprovalDeadline.Should().BeAfter(FixedNow);
        jitRepo.Received(1).Add(Arg.Any<JitAccessRequest>());
    }

    [Fact]
    public async Task Handle_Should_RecordJitAccessRequestedEvent()
    {
        var (_, userRepo, evtRepo, evtTracker, _, _, handler) = CreateHandler();

        var user = CreateLocalUser(mfaEnabled: false);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new RequestJitAccess.Command(
            "deploy:staging",
            "service:api",
            "Deploying security patch urgently.");

        await handler.Handle(cmd, CancellationToken.None);

        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitAccessRequested));
        evtTracker.Received(1).Track(Arg.Any<SecurityEvent>());
    }

    [Fact]
    public async Task Handle_Should_ReturnMfaStepUpRequired_WhenMfaEnabledButNoCode()
    {
        var (_, userRepo, evtRepo, _, _, _, handler) = CreateHandler();

        var user = CreateLocalUser(mfaEnabled: true);
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new RequestJitAccess.Command(
            "admin:database",
            "db:users",
            "Need to investigate data integrity issue.",
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
        var (_, userRepo, evtRepo, _, totp, _, handler) = CreateHandler();

        var user = CreateLocalUser(mfaEnabled: true, mfaSecret: "JBSWY3DPEHPK3PXP");
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        totp.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var cmd = new RequestJitAccess.Command(
            "admin:database",
            "db:users",
            "Need to investigate data integrity issue.",
            MfaCode: "000000");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.mfaCodeInvalid");
        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.MfaStepUpDenied));
    }

    [Fact]
    public async Task Handle_Should_CreateJitRequest_WhenMfaVerifies()
    {
        var (jitRepo, userRepo, _, _, totp, _, handler) = CreateHandler();

        var user = CreateLocalUser(mfaEnabled: true, mfaSecret: "JBSWY3DPEHPK3PXP");
        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        totp.Verify(Arg.Any<string>(), "123456").Returns(true);

        var cmd = new RequestJitAccess.Command(
            "infra:restart-pods",
            "cluster:prod-01",
            "Pods stuck after deployment, need restart.",
            MfaCode: "123456");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jitRepo.Received(1).Add(Arg.Any<JitAccessRequest>());
    }

    [Fact]
    public async Task Handle_Should_ReturnUserNotFound_WhenUserDoesNotExist()
    {
        var (_, userRepo, _, _, _, _, handler) = CreateHandler();

        userRepo.GetByIdAsync(Arg.Any<Domain.Entities.UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var cmd = new RequestJitAccess.Command(
            "deploy:production",
            "service:auth",
            "Deploy fix for authentication regression.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.userNotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_WhenCurrentUserIdIsEmpty()
    {
        var (_, _, _, _, _, currentUser, handler) = CreateHandler();
        currentUser.Id.Returns(string.Empty);

        var cmd = new RequestJitAccess.Command(
            "deploy:production",
            "service:auth",
            "Deploy fix for authentication regression.");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("identity.notAuthenticated");
    }
}
