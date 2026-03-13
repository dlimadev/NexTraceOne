using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using RefreshTokenFeature = NexTraceOne.Identity.Application.Features.RefreshToken.RefreshToken;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature RefreshToken.
///
/// Refatoração: testes atualizados para usar ILoginResponseBuilder em vez de
/// classes estáticas (IdentityFeatureSupport), aderindo ao DIP.
/// </summary>
public sealed class RefreshTokenTests
{
    [Fact]
    public async Task Handle_Should_ReturnNewTokens_When_RefreshTokenIsValid()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Administrative access");
        var session = Session.Create(user.Id, RefreshTokenHash.Create("refresh-token"), now.AddDays(1), "127.0.0.1", "tests");
        var sessionRepository = Substitute.For<ISessionRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();
        var sut = new RefreshTokenFeature.Handler(
            sessionRepository,
            userRepository,
            roleRepository,
            jwtTokenGenerator,
            new TestDateTimeProvider(now),
            responseBuilder);

        sessionRepository.GetByRefreshTokenHashAsync(Arg.Any<RefreshTokenHash>(), Arg.Any<CancellationToken>()).Returns(session);
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        jwtTokenGenerator.GenerateRefreshToken().Returns("new-refresh-token");
        responseBuilder.CreateLoginResponse(user, membership, role, "new-refresh-token")
            .Returns(new LocalLoginFeature.LoginResponse("new-access-token", "new-refresh-token", 3600,
                new LocalLoginFeature.UserResponse(user.Id.Value, "alice@example.com", "Alice Doe", membership.TenantId.Value, Role.PlatformAdmin, [])));

        var result = await sut.Handle(new RefreshTokenFeature.Command("refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_Should_ReturnExpiredError_When_SessionIsExpired()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var session = Session.Create(user.Id, RefreshTokenHash.Create("refresh-token"), now.AddMinutes(-1), "127.0.0.1", "tests");
        var sessionRepository = Substitute.For<ISessionRepository>();
        var sut = new RefreshTokenFeature.Handler(
            sessionRepository,
            Substitute.For<IUserRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>(),
            new TestDateTimeProvider(now),
            Substitute.For<ILoginResponseBuilder>());

        sessionRepository.GetByRefreshTokenHashAsync(Arg.Any<RefreshTokenHash>(), Arg.Any<CancellationToken>()).Returns(session);

        var result = await sut.Handle(new RefreshTokenFeature.Command("refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.Expired");
    }

    [Fact]
    public async Task Handle_Should_ReturnRevokedError_When_SessionWasRevoked()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var session = Session.Create(user.Id, RefreshTokenHash.Create("refresh-token"), now.AddDays(1), "127.0.0.1", "tests");
        session.Revoke(now.AddMinutes(-5));
        var sessionRepository = Substitute.For<ISessionRepository>();
        var sut = new RefreshTokenFeature.Handler(
            sessionRepository,
            Substitute.For<IUserRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>(),
            new TestDateTimeProvider(now),
            Substitute.For<ILoginResponseBuilder>());

        sessionRepository.GetByRefreshTokenHashAsync(Arg.Any<RefreshTokenHash>(), Arg.Any<CancellationToken>()).Returns(session);

        var result = await sut.Handle(new RefreshTokenFeature.Command("refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.Revoked");
    }
}
