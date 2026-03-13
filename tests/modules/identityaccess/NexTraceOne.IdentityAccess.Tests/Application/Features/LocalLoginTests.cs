using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature LocalLogin.
/// Cobrem cenários de sucesso, falha de credenciais, lockout e geração de eventos de segurança.
///
/// Refatoração: testes atualizados para usar interfaces injetáveis (ISecurityAuditRecorder,
/// ILoginSessionCreator, ILoginResponseBuilder) em vez de classes estáticas,
/// aderindo ao DIP e melhorando a testabilidade.
/// </summary>
public sealed class LocalLoginTests
{
    [Fact]
    public async Task Handle_Should_ReturnTokens_When_CredentialsAreValid()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Administrative access");
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            roleRepository,
            passwordHasher,
            new TestDateTimeProvider(now),
            auditRecorder,
            sessionCreator,
            responseBuilder);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        passwordHasher.Verify("P@ssw0rd123", user.PasswordHash!.Value).Returns(true);
        sessionCreator.CreateSession(user.Id, Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((Session.Create(user.Id, RefreshTokenHash.Create("refresh-token"), now.AddDays(30), "unknown", "unknown"), "refresh-token"));
        responseBuilder.CreateLoginResponse(user, membership, role, "refresh-token")
            .Returns(new LocalLoginFeature.LoginResponse("access-token", "refresh-token", 3600,
                new LocalLoginFeature.UserResponse(user.Id.Value, "alice@example.com", "Alice Doe", membership.TenantId.Value, Role.PlatformAdmin, [])));

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "P@ssw0rd123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        sessionCreator.Received(1).CreateSession(user.Id, Arg.Any<string?>(), Arg.Any<string?>());
        auditRecorder.Received(1).RecordAuthenticationSuccess(
            membership.TenantId, user.Id, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_When_PasswordIsWrong()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        auditRecorder.ResolveTenantIdForAudit().Returns(TenantId.From(Guid.NewGuid()));
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            roleRepository,
            passwordHasher,
            new TestDateTimeProvider(now),
            auditRecorder,
            sessionCreator,
            responseBuilder);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("wrong-password", user.PasswordHash!.Value).Returns(false);

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.InvalidCredentials");
        sessionCreator.DidNotReceive().CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>());
        auditRecorder.Received(1).RecordAuthenticationFailure(
            Arg.Any<TenantId>(), user.Id, Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_LockAccount_When_FifthAttemptFails()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        for (var attempt = 0; attempt < 4; attempt++)
        {
            user.RegisterFailedLogin(now);
        }

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        auditRecorder.ResolveTenantIdForAudit().Returns(TenantId.From(Guid.NewGuid()));
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            roleRepository,
            passwordHasher,
            new TestDateTimeProvider(now),
            auditRecorder,
            sessionCreator,
            responseBuilder);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("wrong-password", user.PasswordHash!.Value).Returns(false);

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.AccountLocked");
        auditRecorder.Received(1).RecordAccountLocked(
            Arg.Any<TenantId>(), user.Id, Arg.Any<string?>(), Arg.Any<string?>());
    }
}
