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

    private static SelectTenant.Handler MakeHandler(
        ICurrentUser? currentUser = null,
        IUserRepository? userRepo = null,
        ITenantMembershipRepository? membershipRepo = null,
        ITenantRepository? tenantRepo = null,
        IRoleRepository? roleRepo = null,
        IJwtTokenGenerator? jwtGenerator = null,
        IPermissionResolver? permissionResolver = null,
        ITenantLicenseRepository? licenseRepo = null)
        => new(
            currentUser ?? Substitute.For<ICurrentUser>(),
            userRepo ?? Substitute.For<IUserRepository>(),
            membershipRepo ?? Substitute.For<ITenantMembershipRepository>(),
            tenantRepo ?? Substitute.For<ITenantRepository>(),
            roleRepo ?? Substitute.For<IRoleRepository>(),
            jwtGenerator ?? Substitute.For<IJwtTokenGenerator>(),
            permissionResolver ?? Substitute.For<IPermissionResolver>(),
            licenseRepo ?? Substitute.For<ITenantLicenseRepository>());

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
        jwtGenerator.GenerateAccessToken(
            user, tenant.Id,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<IReadOnlyCollection<string>?>())
            .Returns("new-access-token");
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);

        var permissionResolver = Substitute.For<IPermissionResolver>();
        var licenseRepo = Substitute.For<ITenantLicenseRepository>();
        licenseRepo.GetByTenantIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns((TenantLicense?)null);

        var handler = MakeHandler(currentUser, userRepo, membershipRepo, tenantRepo, roleRepo, jwtGenerator, permissionResolver, licenseRepo);
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
        currentUser.Id.Returns(Guid.NewGuid().ToString());
        currentUser.IsAuthenticated.Returns(true);

        var user = User.CreateLocal(Email.Create("alice@test.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = MakeHandler(currentUser: currentUser, userRepo: userRepo, tenantRepo: tenantRepo);
        var result = await handler.Handle(new SelectTenant.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Tenant.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_UserNotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(string.Empty);

        var handler = MakeHandler(currentUser: currentUser);
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

        var handler = MakeHandler(currentUser: currentUser, userRepo: userRepo, membershipRepo: membershipRepo, tenantRepo: tenantRepo);
        var result = await handler.Handle(new SelectTenant.Command(tenant.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.TenantMembership.NotFound");
    }

    // SaaS-01: capability injection tests ─────────────────────────────────

    [Fact]
    public async Task Handle_Should_EmbedPlanCapabilities_When_LicenseExists()
    {
        var user = User.CreateLocal(Email.Create("dave@test.com"), FullName.Create("Dave", "Smith"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var tenant = Tenant.Create("Starter Corp", "starter", Now);
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Dev");
        var membership = TenantMembership.Create(user.Id, tenant.Id, role.Id, Now);
        var license = TenantLicense.Provision(tenant.Id.Value, TenantPlan.Starter, 5, Now, null, Now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(user.Id.Value.ToString());

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.GetByUserAndTenantAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        IReadOnlyCollection<string>? capturedCapabilities = null;
        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtGenerator.GenerateAccessToken(
            user, tenant.Id,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Do<IReadOnlyCollection<string>?>(c => capturedCapabilities = c))
            .Returns("token");

        var licenseRepo = Substitute.For<ITenantLicenseRepository>();
        licenseRepo.GetByTenantIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(license);

        var handler = MakeHandler(currentUser, userRepo, membershipRepo, tenantRepo, roleRepo, jwtGenerator, licenseRepo: licenseRepo);
        await handler.Handle(new SelectTenant.Command(tenant.Id.Value), CancellationToken.None);

        capturedCapabilities.Should().NotBeNull();
        capturedCapabilities.Should().Contain(TenantCapabilities.Apm,
            "Starter plan includes apm capability");
        capturedCapabilities.Should().NotContain(TenantCapabilities.ComplianceAdvanced,
            "Starter plan does not include compliance_advanced capability");
    }

    [Fact]
    public async Task Handle_Should_FallBackToEnterprise_When_NoLicenseFound()
    {
        var user = User.CreateLocal(Email.Create("eve@test.com"), FullName.Create("Eve", "Jones"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var tenant = Tenant.Create("Legacy Corp", "legacy", Now);
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Dev");
        var membership = TenantMembership.Create(user.Id, tenant.Id, role.Id, Now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(user.Id.Value.ToString());

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.GetByUserAndTenantAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        IReadOnlyCollection<string>? capturedCapabilities = null;
        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtGenerator.GenerateAccessToken(
            user, tenant.Id,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Do<IReadOnlyCollection<string>?>(c => capturedCapabilities = c))
            .Returns("token");

        var licenseRepo = Substitute.For<ITenantLicenseRepository>();
        licenseRepo.GetByTenantIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns((TenantLicense?)null);

        var handler = MakeHandler(currentUser, userRepo, membershipRepo, tenantRepo, roleRepo, jwtGenerator, licenseRepo: licenseRepo);
        await handler.Handle(new SelectTenant.Command(tenant.Id.Value), CancellationToken.None);

        var enterpriseCapabilities = TenantCapabilities.ForPlan(TenantPlan.Enterprise);
        capturedCapabilities.Should().BeEquivalentTo(enterpriseCapabilities,
            "tenants without a license fall back to Enterprise capabilities for self-hosted backward compatibility");
    }
}
