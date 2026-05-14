using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.OidcCallback;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler OidcCallback.
/// Cobre: provider não configurado, JIT provisioning de novo usuário, vinculação de usuário existente,
/// auto-provisão de membership, extractReturnTo de state válido e inválido.
/// </summary>
public sealed class OidcCallbackTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantIdValue = Guid.NewGuid();
    private const string Provider = "azure";

    private static (
        IOidcProvider oidcProvider,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        ITenantMembershipRepository membershipRepo,
        ISecurityAuditRecorder auditRecorder,
        ILoginSessionCreator sessionCreator,
        ILoginResponseBuilder responseBuilder,
        OidcCallback.Handler handler) CreateHandler()
    {
        var oidcProvider = Substitute.For<IOidcProvider>();
        var userRepo = Substitute.For<IUserRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var clock = new TestDateTimeProvider(FixedNow);
        var currentTenant = new TestCurrentTenant(TenantIdValue);

        var handler = new OidcCallback.Handler(
            oidcProvider, userRepo, roleRepo, membershipRepo,
            clock, currentTenant, auditRecorder, sessionCreator, responseBuilder);

        return (oidcProvider, userRepo, roleRepo, membershipRepo, auditRecorder, sessionCreator, responseBuilder, handler);
    }

    private static Role CreateViewerRole()
    {
        return Role.CreateSystem(RoleId.New(), Role.Viewer, "Viewer role");
    }

    private static User CreateFederatedUser()
    {
        return User.CreateFederated(
            Email.Create("test@azure.com"),
            FullName.Create("Test", "User"),
            Provider,
            "ext-id-123");
    }

    private static TenantMembership CreateMembership(UserId userId, RoleId roleId)
    {
        return TenantMembership.Create(userId, TenantId.From(TenantIdValue), roleId, FixedNow);
    }

    private static LocalLoginFeature.LoginResponse CreateFakeLoginResponse(User user, Role role)
    {
        return new LocalLoginFeature.LoginResponse(
            "access-token",
            "refresh-token",
            3600,
            new LocalLoginFeature.UserResponse(user.Id.Value, user.Email.Value, user.FullName.Value, TenantIdValue, role.Name, []));
    }

    private static string BuildValidState(string returnTo = "/dashboard")
    {
        var nonce = Guid.NewGuid().ToString("N");
        var encoded = $"{nonce}:{Uri.EscapeDataString(returnTo)}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(encoded));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenProviderNotConfigured()
    {
        var (oidcProvider, _, _, _, _, _, _, handler) = CreateHandler();
        oidcProvider.IsConfigured(Provider).Returns(false);

        var cmd = new OidcCallback.Command(Provider, "code", "state");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Identity.Oidc.ProviderNotConfigured");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenCodeExchangeFails()
    {
        var (oidcProvider, _, _, _, auditRecorder, _, _, handler) = CreateHandler();
        oidcProvider.IsConfigured(Provider).Returns(true);
        oidcProvider.ExchangeCodeAsync(Provider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OidcUserInfo>(new InvalidOperationException("Exchange failed")));
        auditRecorder.ResolveTenantIdForAudit().Returns(TenantId.From(TenantIdValue));

        var cmd = new OidcCallback.Command(Provider, "invalid-code", BuildValidState());

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Identity.Oidc.CallbackFailed");
        auditRecorder.Received(1).RecordOidcCallbackFailure(
            Arg.Any<TenantId>(),
            Provider,
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ProvisionNewUser_WhenFederatedIdentityNotFound()
    {
        var (oidcProvider, userRepo, roleRepo, membershipRepo, _, sessionCreator, responseBuilder, handler) = CreateHandler();

        oidcProvider.IsConfigured(Provider).Returns(true);
        oidcProvider.ExchangeCodeAsync(Provider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OidcUserInfo("ext-id-new", "new@azure.com", "New User", Provider));

        userRepo.GetByFederatedIdentityAsync(Provider, "ext-id-new", Arg.Any<CancellationToken>()).Returns((User?)null);
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var viewerRole = CreateViewerRole();
        roleRepo.GetByNameAsync(Role.Viewer, Arg.Any<CancellationToken>()).Returns(viewerRole);
        roleRepo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(viewerRole);

        TenantMembership? capturedMembership = null;
        membershipRepo.When(r => r.Add(Arg.Any<TenantMembership>()))
            .Do(call => capturedMembership = call.Arg<TenantMembership>());

        responseBuilder.ResolveMembershipAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((TenantMembership?)null);
        responseBuilder.CreateLoginResponseAsync(
            Arg.Any<User>(), Arg.Any<TenantMembership>(), Arg.Any<Role>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => CreateFakeLoginResponse(call.Arg<User>(), viewerRole));

        User? capturedUser = null;
        userRepo.When(r => r.Add(Arg.Any<User>()))
            .Do(call => capturedUser = call.Arg<User>());

        var sessionId = SessionId.New();
        var fakeSession = Session.Create(UserId.New(), RefreshTokenHash.Create("fake-token"), FixedNow.AddDays(30), "127.0.0.1", "TestAgent");
        sessionCreator.CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((fakeSession, "refresh-token-plain"));

        var cmd = new OidcCallback.Command(Provider, "code", BuildValidState("/dashboard"));

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Received(1).Add(Arg.Any<User>());
        capturedMembership.Should().NotBeNull("auto-provisioning should create a membership");
    }

    [Fact]
    public async Task Handle_Should_LinkFederatedIdentity_WhenUserExistsByEmail()
    {
        var (oidcProvider, userRepo, roleRepo, _, _, sessionCreator, responseBuilder, handler) = CreateHandler();

        oidcProvider.IsConfigured(Provider).Returns(true);
        oidcProvider.ExchangeCodeAsync(Provider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OidcUserInfo("ext-id-999", "existing@test.com", "Existing User", Provider));

        var existingUser = CreateFederatedUser();
        userRepo.GetByFederatedIdentityAsync(Provider, "ext-id-999", Arg.Any<CancellationToken>()).Returns((User?)null);
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(existingUser);

        var viewerRole = CreateViewerRole();
        roleRepo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(viewerRole);

        var membership = CreateMembership(existingUser.Id, viewerRole.Id);
        responseBuilder.ResolveMembershipAsync(existingUser.Id, Arg.Any<CancellationToken>()).Returns(membership);
        responseBuilder.CreateLoginResponseAsync(
            Arg.Any<User>(), membership, viewerRole, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateFakeLoginResponse(existingUser, viewerRole));

        var fakeSession = Session.Create(existingUser.Id, RefreshTokenHash.Create("fake-token"), FixedNow.AddDays(30), "127.0.0.1", "TestAgent");
        sessionCreator.CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((fakeSession, "refresh-token-plain"));

        var cmd = new OidcCallback.Command(Provider, "code", BuildValidState("/home"));

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_Should_ExtractReturnTo_FromValidState()
    {
        var (oidcProvider, userRepo, roleRepo, _, _, sessionCreator, responseBuilder, handler) = CreateHandler();

        oidcProvider.IsConfigured(Provider).Returns(true);
        oidcProvider.ExchangeCodeAsync(Provider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OidcUserInfo("ext-id", "user@test.com", "User", Provider));

        var user = CreateFederatedUser();
        userRepo.GetByFederatedIdentityAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var viewerRole = CreateViewerRole();
        roleRepo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(viewerRole);

        var membership = CreateMembership(user.Id, viewerRole.Id);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        responseBuilder.CreateLoginResponseAsync(
            Arg.Any<User>(), membership, viewerRole, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateFakeLoginResponse(user, viewerRole));

        var fakeSession = Session.Create(user.Id, RefreshTokenHash.Create("fake-token"), FixedNow.AddDays(30), "127.0.0.1", "TestAgent");
        sessionCreator.CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((fakeSession, "refresh-token-plain"));

        var cmd = new OidcCallback.Command(Provider, "code", BuildValidState("/projects/123"));

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReturnTo.Should().Be("/projects/123");
    }

    [Fact]
    public async Task Handle_Should_FallbackReturnToRoot_WhenStateIsMalformed()
    {
        var (oidcProvider, userRepo, roleRepo, _, _, sessionCreator, responseBuilder, handler) = CreateHandler();

        oidcProvider.IsConfigured(Provider).Returns(true);
        oidcProvider.ExchangeCodeAsync(Provider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OidcUserInfo("ext-id", "user@test.com", "User", Provider));

        var user = CreateFederatedUser();
        userRepo.GetByFederatedIdentityAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var viewerRole = CreateViewerRole();
        roleRepo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(viewerRole);

        var membership = CreateMembership(user.Id, viewerRole.Id);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        responseBuilder.CreateLoginResponseAsync(
            Arg.Any<User>(), membership, viewerRole, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateFakeLoginResponse(user, viewerRole));

        var fakeSession = Session.Create(user.Id, RefreshTokenHash.Create("fake-token"), FixedNow.AddDays(30), "127.0.0.1", "TestAgent");
        sessionCreator.CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((fakeSession, "refresh-token-plain"));

        var cmd = new OidcCallback.Command(Provider, "code", "not-valid-base64!!");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReturnTo.Should().Be("/");
    }
}

