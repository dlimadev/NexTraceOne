using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;
using VerifyMfaChallengeFeature = NexTraceOne.IdentityAccess.Application.Features.VerifyMfaChallenge.VerifyMfaChallenge;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature VerifyMfaChallenge.
/// Cobre sucesso, token inválido, token expirado e código TOTP inválido.
/// </summary>
public sealed class VerifyMfaChallengeTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnLoginResponse_When_ChallengeTokenAndCodeAreValid()
    {
        // Arrange
        var user = TestUserFactory.CreateMfaUser();
        var userId = user.Id.Value;
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Administrative access");

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var sessionCreator = Substitute.For<ILoginSessionCreator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();
        var totpVerifier = Substitute.For<ITotpVerifier>();

        var challengeToken = "valid-challenge-token";
        mfaChallengeService.TryValidate(challengeToken, out Arg.Any<Guid>())
            .Returns(x => { x[1] = userId; return true; });

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        totpVerifier.Verify(user.MfaSecret!, "123456").Returns(true);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        auditRecorder.ResolveTenantIdForAudit().Returns(membership.TenantId);
        sessionCreator.CreateSession(user.Id, Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((Session.Create(user.Id, RefreshTokenHash.Create("refresh-token"), Now.AddDays(30), "unknown", "unknown"), "refresh-token"));
        responseBuilder.CreateLoginResponseAsync(user, membership, role, "refresh-token", Arg.Any<CancellationToken>())
            .Returns(new LocalLoginFeature.LoginResponse("access-token", "refresh-token", 3600,
                new LocalLoginFeature.UserResponse(user.Id.Value, user.Email.Value, user.FullName.Value, membership.TenantId.Value, Role.PlatformAdmin, [])));

        var sut = new VerifyMfaChallengeFeature.Handler(
            userRepository, roleRepository, new TestDateTimeProvider(Now),
            auditRecorder, sessionCreator, responseBuilder, mfaChallengeService, totpVerifier);

        // Act
        var result = await sut.Handle(
            new VerifyMfaChallengeFeature.Command(challengeToken, "123456"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.MfaRequired.Should().BeFalse();
        auditRecorder.Received(1).RecordMfaChallengeSuccess(
            membership.TenantId, user.Id, Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_ChallengeTokenIsInvalid()
    {
        // Arrange
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();
        mfaChallengeService.TryValidate(Arg.Any<string>(), out Arg.Any<Guid>()).Returns(false);

        var sut = new VerifyMfaChallengeFeature.Handler(
            Substitute.For<IUserRepository>(),
            Substitute.For<IRoleRepository>(),
            new TestDateTimeProvider(Now),
            Substitute.For<ISecurityAuditRecorder>(),
            Substitute.For<ILoginSessionCreator>(),
            Substitute.For<ILoginResponseBuilder>(),
            mfaChallengeService,
            Substitute.For<ITotpVerifier>());

        // Act
        var result = await sut.Handle(
            new VerifyMfaChallengeFeature.Command("invalid-token", "123456"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Mfa.ChallengeExpiredOrInvalid");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_TotpCodeIsInvalid()
    {
        // Arrange
        var user = TestUserFactory.CreateMfaUser();
        var userId = user.Id.Value;

        var userRepository = Substitute.For<IUserRepository>();
        var auditRecorder = Substitute.For<ISecurityAuditRecorder>();
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();
        var totpVerifier = Substitute.For<ITotpVerifier>();

        var challengeToken = "valid-token";
        mfaChallengeService.TryValidate(challengeToken, out Arg.Any<Guid>())
            .Returns(x => { x[1] = userId; return true; });

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        totpVerifier.Verify(user.MfaSecret!, "999999").Returns(false);
        auditRecorder.ResolveTenantIdForAudit().Returns(TenantId.From(Guid.NewGuid()));

        var sut = new VerifyMfaChallengeFeature.Handler(
            userRepository,
            Substitute.For<IRoleRepository>(),
            new TestDateTimeProvider(Now),
            auditRecorder,
            Substitute.For<ILoginSessionCreator>(),
            Substitute.For<ILoginResponseBuilder>(),
            mfaChallengeService,
            totpVerifier);

        // Act
        var result = await sut.Handle(
            new VerifyMfaChallengeFeature.Command(challengeToken, "999999"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Mfa.CodeInvalid");
        auditRecorder.Received(1).RecordMfaChallengeFailed(
            Arg.Any<TenantId>(), user.Id, Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userRepository = Substitute.For<IUserRepository>();
        var mfaChallengeService = Substitute.For<IMfaChallengeTokenService>();

        mfaChallengeService.TryValidate(Arg.Any<string>(), out Arg.Any<Guid>())
            .Returns(x => { x[1] = userId; return true; });

        userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var sut = new VerifyMfaChallengeFeature.Handler(
            userRepository,
            Substitute.For<IRoleRepository>(),
            new TestDateTimeProvider(Now),
            Substitute.For<ISecurityAuditRecorder>(),
            Substitute.For<ILoginSessionCreator>(),
            Substitute.For<ILoginResponseBuilder>(),
            mfaChallengeService,
            Substitute.For<ITotpVerifier>());

        // Act
        var result = await sut.Handle(
            new VerifyMfaChallengeFeature.Command("valid-token", "123456"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Mfa.ChallengeExpiredOrInvalid");
    }
}
