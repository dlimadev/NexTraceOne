using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.SelectTenant;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature SelectTenant — valida seleção de tenant após login multi-tenant.
/// Cobre cenários de sucesso, tenant inexistente, membership inativa e usuário não autenticado.
/// </summary>
public sealed class SelectTenantTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnNewToken_When_UserHasMembership()
    {
        var user = User.CreateLocal(Email.Create("alice@test.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var tenant = Tenant.Create("Org Alpha", "alpha", Now);
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Dev");
        var membership = TenantMembership.Create(user.Id, tenant.Id, role.Id, Now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(user.Id.Value.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.GetByUserAndTenantAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.GenerateAccessToken(user, membership, Arg.Any<IReadOnlyCollection<string>>()).Returns("new-access-token");
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);

        var handler = new SelectTenant.Handler(currentUser, userRepo, membershipRepo, tenantRepo, roleRepo, jwtGenerator);
        var result = await handler.Handle(new SelectTenant.Command(tenant.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.TenantName.Should().Be("Org Alpha");
        result.Value.RoleName.Should().Be(Role.Developer);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TenantNotFound()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.Id.Returns(userId.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var user = User.CreateLocal(Email.Create("alice@test.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = new SelectTenant.Handler(
            currentUser, userRepo,
            Substitute.For<ITenantMembershipRepository>(),
            tenantRepo,
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>());

        var result = await handler.Handle(new SelectTenant.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Tenant.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_UserNotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(string.Empty);

        var handler = new SelectTenant.Handler(
            currentUser,
            Substitute.For<IUserRepository>(),
            Substitute.For<ITenantMembershipRepository>(),
            Substitute.For<ITenantRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>());

        var result = await handler.Handle(new SelectTenant.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.NotAuthenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_MembershipDoesNotExist()
    {
        var user = User.CreateLocal(Email.Create("alice@test.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var tenant = Tenant.Create("Org", "org", Now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(user.Id.Value.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.GetByUserAndTenantAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>())
            .Returns((TenantMembership?)null);

        var handler = new SelectTenant.Handler(
            currentUser, userRepo, membershipRepo, tenantRepo,
            Substitute.For<IRoleRepository>(),
            Substitute.For<IJwtTokenGenerator>());

        var result = await handler.Handle(new SelectTenant.Command(tenant.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.TenantMembership.NotFound");
    }
}
