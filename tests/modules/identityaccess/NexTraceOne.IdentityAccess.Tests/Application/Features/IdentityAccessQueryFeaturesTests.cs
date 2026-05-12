using NexTraceOne.BuildingBlocks.Application.Pagination;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.GetTenant;
using NexTraceOne.IdentityAccess.Application.Features.GetUserProfile;
using NexTraceOne.IdentityAccess.Application.Features.ListActiveSessions;
using NexTraceOne.IdentityAccess.Application.Features.ListPermissions;
using NexTraceOne.IdentityAccess.Application.Features.ListRoles;
using NexTraceOne.IdentityAccess.Application.Features.ListTenantUsers;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de consulta do IdentityAccess:
/// ListRoles, ListPermissions, GetUserProfile, ListTenantUsers, GetTenant e ListActiveSessions.
/// </summary>
public sealed class IdentityAccessQueryFeaturesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    // ═══════════════════════════════════════════════════════════════════
    // ListRoles
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListRoles_Should_ReturnAllRolesWithPermissions()
    {
        var systemRole = Role.CreateSystem(RoleId.New(), Role.PlatformAdmin, "Platform administrator");
        var customRole = Role.CreateCustom("Reviewer", "Reviews changes");

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([systemRole, customRole]);

        var permissionResolver = Substitute.For<IPermissionResolver>();
        permissionResolver.ResolvePermissionsAsync(systemRole.Id, systemRole.Name, null, Arg.Any<CancellationToken>())
            .Returns(["catalog:read", "changes:read"]);
        permissionResolver.ResolvePermissionsAsync(customRole.Id, customRole.Name, null, Arg.Any<CancellationToken>())
            .Returns(["changes:review"]);

        var handler = new ListRoles.Handler(roleRepo, permissionResolver);
        var result = await handler.Handle(new ListRoles.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var admin = result.Value.Single(r => r.Name == Role.PlatformAdmin);
        admin.IsSystem.Should().BeTrue();
        admin.Permissions.Should().Contain("catalog:read");

        var reviewer = result.Value.Single(r => r.Name == "Reviewer");
        reviewer.IsSystem.Should().BeFalse();
        reviewer.Permissions.Should().Contain("changes:review");
    }

    [Fact]
    public async Task ListRoles_Should_ReturnEmptyList_When_NoRolesExist()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var permissionResolver = Substitute.For<IPermissionResolver>();

        var handler = new ListRoles.Handler(roleRepo, permissionResolver);
        var result = await handler.Handle(new ListRoles.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListPermissions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListPermissions_Should_ReturnAllPermissions()
    {
        var p1 = Permission.Create(PermissionId.New(), "catalog:read", "Read Catalog", "Catalog");
        var p2 = Permission.Create(PermissionId.New(), "changes:write", "Write Changes", "Changes");
        var p3 = Permission.Create(PermissionId.New(), "governance:admin", "Governance Admin", "Governance");

        var permissionRepo = Substitute.For<IPermissionRepository>();
        permissionRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([p1, p2, p3]);

        var handler = new ListPermissions.Handler(permissionRepo);
        var result = await handler.Handle(new ListPermissions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(p => p.Code).Should().Contain("catalog:read", "changes:write", "governance:admin");
        result.Value.Select(p => p.Module).Should().Contain("Catalog", "Changes", "Governance");
    }

    [Fact]
    public async Task ListPermissions_Should_ReturnEmpty_When_NoneRegistered()
    {
        var permissionRepo = Substitute.For<IPermissionRepository>();
        permissionRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = new ListPermissions.Handler(permissionRepo);
        var result = await handler.Handle(new ListPermissions.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetUserProfile
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUserProfile_Should_ReturnProfile_With_Memberships()
    {
        var user = User.CreateLocal(
            Email.Create("bob@corp.com"),
            FullName.Create("Bob", "Builder"),
            HashedPassword.FromPlainText("P@ssw0rd99!"));
        user.RegisterSuccessfulLogin(FixedNow);

        var tenantId = TenantId.From(Guid.NewGuid());
        var roleId = RoleId.New();
        var role = Role.CreateSystem(roleId, Role.Developer, "Dev role");
        var membership = TenantMembership.Create(user.Id, tenantId, roleId, FixedNow);

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns([membership]);

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var handler = new GetUserProfile.Handler(userRepo, membershipRepo, roleRepo);
        var result = await handler.Handle(new GetUserProfile.Query(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("bob@corp.com");
        result.Value.FirstName.Should().Be("Bob");
        result.Value.LastName.Should().Be("Builder");
        result.Value.IsActive.Should().BeTrue();
        result.Value.LastLoginAt.Should().Be(FixedNow);
        result.Value.Memberships.Should().HaveCount(1);
        result.Value.Memberships[0].TenantId.Should().Be(tenantId.Value);
        result.Value.Memberships[0].RoleName.Should().Be(Role.Developer);
    }

    [Fact]
    public async Task GetUserProfile_Should_ReturnError_When_UserNotFound()
    {
        var userId = Guid.NewGuid();

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(UserId.From(userId), Arg.Any<CancellationToken>()).Returns((User?)null);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();

        var handler = new GetUserProfile.Handler(userRepo, membershipRepo, roleRepo);
        var result = await handler.Handle(new GetUserProfile.Query(userId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetUserProfile_Should_ReturnEmptyMemberships_When_UserHasNone()
    {
        var user = User.CreateLocal(
            Email.Create("solo@corp.com"),
            FullName.Create("Solo", "User"),
            HashedPassword.FromPlainText("P@ssw0rd!1"));

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);

        var roleRepo = Substitute.For<IRoleRepository>();

        var handler = new GetUserProfile.Handler(userRepo, membershipRepo, roleRepo);
        var result = await handler.Handle(new GetUserProfile.Query(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void GetUserProfile_Validator_Should_Reject_EmptyUserId()
    {
        var validator = new GetUserProfile.Validator();
        var result = validator.Validate(new GetUserProfile.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListTenantUsers
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListTenantUsers_Should_ReturnPaginatedUsers_For_Tenant()
    {
        var tenantId = TenantId.From(Guid.NewGuid());

        var userA = User.CreateLocal(
            Email.Create("alice@corp.com"),
            FullName.Create("Alice", "Smith"),
            HashedPassword.FromPlainText("P@ssw0rd!1"));
        var userB = User.CreateLocal(
            Email.Create("bob@corp.com"),
            FullName.Create("Bob", "Jones"),
            HashedPassword.FromPlainText("P@ssw0rd!2"));

        var roleId = RoleId.New();
        var role = Role.CreateSystem(roleId, Role.Developer, "Dev");

        var membershipA = TenantMembership.Create(userA.Id, tenantId, roleId, FixedNow);
        var membershipB = TenantMembership.Create(userB.Id, tenantId, roleId, FixedNow);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByTenantAsync(tenantId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(([membershipA, membershipB], 2));

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<UserId, User>
            {
                [userA.Id] = userA,
                [userB.Id] = userB
            });

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var handler = new ListTenantUsers.Handler(membershipRepo, userRepo, roleRepo);
        var result = await handler.Handle(
            new ListTenantUsers.Query(tenantId.Value, Search: null, Page: 1, PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(u => u.Email).Should().Contain("alice@corp.com", "bob@corp.com");
        result.Value.Items.All(u => u.RoleName == Role.Developer).Should().BeTrue();
    }

    [Fact]
    public async Task ListTenantUsers_Should_ReturnEmpty_When_NoMemberships()
    {
        var tenantId = TenantId.From(Guid.NewGuid());

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByTenantAsync(tenantId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<TenantMembership>(), 0));

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<UserId, User>());

        var roleRepo = Substitute.For<IRoleRepository>();

        var handler = new ListTenantUsers.Handler(membershipRepo, userRepo, roleRepo);
        var result = await handler.Handle(
            new ListTenantUsers.Query(tenantId.Value, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void ListTenantUsers_Validator_Should_Reject_EmptyTenantId()
    {
        var validator = new ListTenantUsers.Validator();
        var result = validator.Validate(new ListTenantUsers.Query(Guid.Empty, null, 1, 20));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListTenantUsers_Validator_Should_Reject_InvalidPageSize()
    {
        var validator = new ListTenantUsers.Validator();
        var result = validator.Validate(new ListTenantUsers.Query(Guid.NewGuid(), null, 1, 0));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetTenant
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTenant_Should_ReturnTenantDetail_When_TenantExists()
    {
        var tenant = Tenant.Create("Acme Corp", "acme", FixedNow);
        tenant.UpdateOrganizationInfo("Acme Corporation Ltd.", "00.000.000/0001-99", FixedNow);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new GetTenant.Handler(tenantRepo);
        var result = await handler.Handle(new GetTenant.Query(tenant.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tenant.Id.Value);
        result.Value.Name.Should().Be("Acme Corp");
        result.Value.Slug.Should().Be("acme");
        result.Value.IsActive.Should().BeTrue();
        result.Value.LegalName.Should().Be("Acme Corporation Ltd.");
        result.Value.TaxId.Should().Be("00.000.000/0001-99");
    }

    [Fact]
    public async Task GetTenant_Should_ReturnError_When_TenantNotFound()
    {
        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = new GetTenant.Handler(tenantRepo);
        var result = await handler.Handle(new GetTenant.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListActiveSessions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListActiveSessions_Should_ReturnSessions_For_ExistingUser()
    {
        var user = User.CreateLocal(
            Email.Create("sess@corp.com"),
            FullName.Create("Session", "User"),
            HashedPassword.FromPlainText("P@ssw0rd!3"));

        var session1 = Session.Create(
            user.Id,
            RefreshTokenHash.Create("token1"),
            FixedNow.AddHours(8),
            "192.168.1.1",
            "Chrome/100");
        var session2 = Session.Create(
            user.Id,
            RefreshTokenHash.Create("token2"),
            FixedNow.AddHours(4),
            "10.0.0.5",
            "Firefox/99");

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sessionRepo = Substitute.For<ISessionRepository>();
        sessionRepo.ListActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([session1, session2]);

        var handler = new ListActiveSessions.Handler(userRepo, sessionRepo);
        var result = await handler.Handle(new ListActiveSessions.Query(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(s => s.IpAddress).Should().Contain("192.168.1.1", "10.0.0.5");
        result.Value.Select(s => s.UserAgent).Should().Contain("Chrome/100", "Firefox/99");
    }

    [Fact]
    public async Task ListActiveSessions_Should_ReturnEmpty_When_NoActiveSessions()
    {
        var user = User.CreateLocal(
            Email.Create("nosess@corp.com"),
            FullName.Create("No", "Sessions"),
            HashedPassword.FromPlainText("P@ssw0rd!4"));

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sessionRepo = Substitute.For<ISessionRepository>();
        sessionRepo.ListActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new ListActiveSessions.Handler(userRepo, sessionRepo);
        var result = await handler.Handle(new ListActiveSessions.Query(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListActiveSessions_Should_ReturnError_When_UserNotFound()
    {
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var sessionRepo = Substitute.For<ISessionRepository>();

        var handler = new ListActiveSessions.Handler(userRepo, sessionRepo);
        var result = await handler.Handle(new ListActiveSessions.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}

