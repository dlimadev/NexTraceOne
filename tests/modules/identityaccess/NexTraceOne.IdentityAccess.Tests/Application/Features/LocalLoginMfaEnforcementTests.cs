using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes do MFA enforcement em LocalLogin.
/// Cobre o caminho em que o utilizador tem MFA habilitado e recebe um desafio.
/// </summary>
public sealed class LocalLoginMfaEnforcementTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnMfaChallenge_When_UserHasMfaEnabled()
    {
        // Arrange: utilizador com MFA habilitado
        var user = TestUserFactory.CreateMfaUser();
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        mfaChallengeService.Issue(user.Id.Value, Arg.Any<DateTimeOffset>()).Returns("mfa-challenge-token");

        var sut = new LocalLoginFeature.Handler(
            userRepository, roleRepository, passwordHasher,
            new TestDateTimeProvider(Now), auditRecorder, sessionCreator,
            responseBuilder, mfaChallengeService);

        // Act
        var result = await sut.Handle(
            new LocalLoginFeature.Command("mfa-user@example.com", "P@ssw0rd123"),
            CancellationToken.None);

        // Assert: retorna sucesso mas com MfaRequired = true e ChallengeToken
        result.IsSuccess.Should().BeTrue();
        result.Value.MfaRequired.Should().BeTrue();
        result.Value.MfaChallengeToken.Should().Be("mfa-challenge-token");
        result.Value.AccessToken.Should().BeEmpty();
        result.Value.RefreshToken.Should().BeEmpty();

        // A sessão NÃO deve ser criada — o fluxo fica interrompido no challenge
        sessionCreator.DidNotReceive().CreateSession(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFullTokens_When_UserHasNoMfa()
    {
        // Arrange: utilizador sem MFA
        var user = TestUserFactory.CreateRegularUser();
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Administrative access");

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();

        userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        sessionCreator.CreateSession(user.Id, Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((Session.Create(user.Id, RefreshTokenHash.Create("rt"), Now.AddDays(30), "unknown", "unknown"), "rt"));
        responseBuilder.CreateLoginResponseAsync(user, membership, role, "rt", Arg.Any<CancellationToken>())
            .Returns(new LocalLoginFeature.LoginResponse("access", "rt", 3600,
                new LocalLoginFeature.UserResponse(user.Id.Value, user.Email.Value, user.FullName.Value, membership.TenantId.Value, Role.PlatformAdmin, [])));

        var sut = new LocalLoginFeature.Handler(
            userRepository, roleRepository, passwordHasher,
            new TestDateTimeProvider(Now), auditRecorder, sessionCreator,
            responseBuilder, mfaChallengeService);

        // Act
        var result = await sut.Handle(
            new LocalLoginFeature.Command("user@example.com", "P@ssw0rd123"),
            CancellationToken.None);

        // Assert: retorna tokens completos sem MFA challenge
        result.IsSuccess.Should().BeTrue();
        result.Value.MfaRequired.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access");

        // MFA challenge service NÃO deve ser chamado para utilizadores sem MFA
        mfaChallengeService.DidNotReceive().Issue(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>());
    }
}
