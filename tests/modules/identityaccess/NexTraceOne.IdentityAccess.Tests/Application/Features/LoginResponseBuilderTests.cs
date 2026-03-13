using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Application.Features;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes do serviço LoginResponseBuilder.
/// Valida resolução de membership e construção de resposta padronizada de login.
/// </summary>
public sealed class LoginResponseBuilderTests
{
    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnCurrentTenantMembership_When_TenantIsAvailable()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var userId = UserId.New();
        var tenantId = TenantId.From(Guid.NewGuid());
        var membership = TenantMembership.Create(userId, tenantId, RoleId.New(), now);
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var sut = new LoginResponseBuilder(currentTenant, membershipRepository, jwtTokenGenerator);

        membershipRepository.GetByUserAndTenantAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(membership);

        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().Be(membership);
    }

    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnFirstActiveMembership_When_NoTenantContext()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var userId = UserId.New();
        var membership = TenantMembership.Create(userId, TenantId.From(Guid.NewGuid()), RoleId.New(), now);
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var currentTenant = new TestCurrentTenant(Guid.Empty);
        var sut = new LoginResponseBuilder(currentTenant, membershipRepository, jwtTokenGenerator);

        membershipRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([membership]);

        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().Be(membership);
    }

    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnNull_When_NoMembershipsExist()
    {
        var userId = UserId.New();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var currentTenant = new TestCurrentTenant(Guid.Empty);
        var sut = new LoginResponseBuilder(currentTenant, membershipRepository, jwtTokenGenerator);

        membershipRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void CreateLoginResponse_Should_ReturnCompleteResponse()
    {
        var now = new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Admin");
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var currentTenant = new TestCurrentTenant(membership.TenantId.Value);
        var sut = new LoginResponseBuilder(currentTenant, membershipRepository, jwtTokenGenerator);

        jwtTokenGenerator.GenerateAccessToken(user, membership, Arg.Any<IReadOnlyCollection<string>>()).Returns("access-token");
        jwtTokenGenerator.AccessTokenLifetimeSeconds.Returns(3600);

        var result = sut.CreateLoginResponse(user, membership, role, "refresh-token");

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(3600);
        result.User.Email.Should().Be("alice@example.com");
        result.User.RoleName.Should().Be(Role.PlatformAdmin);
        result.User.Permissions.Should().NotBeEmpty();
    }
}
