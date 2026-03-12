using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using RefreshTokenFeature = NexTraceOne.Identity.Application.Features.RefreshToken.RefreshToken;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature RefreshToken.
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
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var sut = new RefreshTokenFeature.Handler(
            sessionRepository,
            userRepository,
            membershipRepository,
            roleRepository,
            jwtTokenGenerator,
            new TestDateTimeProvider(now),
            new TestCurrentTenant(membership.TenantId.Value));

        sessionRepository.GetByRefreshTokenHashAsync(Arg.Any<RefreshTokenHash>(), Arg.Any<CancellationToken>()).Returns(session);
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        membershipRepository.GetByUserAndTenantAsync(user.Id, membership.TenantId, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(membership.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        jwtTokenGenerator.GenerateRefreshToken().Returns("new-refresh-token");
        jwtTokenGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtTokenGenerator.GenerateAccessToken(user, membership, Arg.Any<IReadOnlyCollection<string>>()).Returns("new-access-token");

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
            Substitute.For<ITenantMembershipRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>(),
            new TestDateTimeProvider(now),
            new TestCurrentTenant(Guid.NewGuid()));

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
            Substitute.For<ITenantMembershipRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>(),
            new TestDateTimeProvider(now),
            new TestCurrentTenant(Guid.NewGuid()));

        sessionRepository.GetByRefreshTokenHashAsync(Arg.Any<RefreshTokenHash>(), Arg.Any<CancellationToken>()).Returns(session);

        var result = await sut.Handle(new RefreshTokenFeature.Command("refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.Revoked");
    }
}
