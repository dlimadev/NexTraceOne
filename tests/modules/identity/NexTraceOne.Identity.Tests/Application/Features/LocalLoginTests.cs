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
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var currentTenant = new TestCurrentTenant(membership.TenantId.Value);
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            membershipRepository,
            roleRepository,
            sessionRepository,
            passwordHasher,
            jwtTokenGenerator,
            new TestDateTimeProvider(now),
            currentTenant,
            securityEventRepository);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        membershipRepository.GetByUserAndTenantAsync(user.Id, membership.TenantId, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        passwordHasher.Verify("P@ssw0rd123", user.PasswordHash!.Value).Returns(true);
        jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");
        jwtTokenGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtTokenGenerator.GenerateAccessToken(user, membership, Arg.Any<IReadOnlyCollection<string>>()).Returns("access-token");

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "P@ssw0rd123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        sessionRepository.Received(1).Add(Arg.Any<Session>());
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.AuthenticationSucceeded));
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_When_PasswordIsWrong()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var userRepository = Substitute.For<IUserRepository>();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            membershipRepository,
            roleRepository,
            sessionRepository,
            passwordHasher,
            jwtTokenGenerator,
            new TestDateTimeProvider(now),
            new TestCurrentTenant(Guid.NewGuid()),
            securityEventRepository);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("wrong-password", user.PasswordHash!.Value).Returns(false);

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.InvalidCredentials");
        sessionRepository.DidNotReceive().Add(Arg.Any<Session>());
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.AuthenticationFailed));
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
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var sessionRepository = Substitute.For<ISessionRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var sut = new LocalLoginFeature.Handler(
            userRepository,
            membershipRepository,
            roleRepository,
            sessionRepository,
            passwordHasher,
            jwtTokenGenerator,
            new TestDateTimeProvider(now),
            new TestCurrentTenant(Guid.NewGuid()),
            securityEventRepository);

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("wrong-password", user.PasswordHash!.Value).Returns(false);

        var result = await sut.Handle(new LocalLoginFeature.Command("alice@example.com", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.AccountLocked");
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.AccountLocked));
    }
}
