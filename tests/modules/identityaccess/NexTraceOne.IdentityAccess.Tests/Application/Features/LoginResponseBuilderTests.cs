using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes do serviço LoginResponseBuilder.
/// Valida resolução de membership e construção de resposta padronizada de login.
/// </summary>
public sealed class LoginResponseBuilderTests
{
    private static readonly DateTimeOffset Now = new(2025, 03, 12, 10, 0, 0, TimeSpan.Zero);

    private static LoginResponseBuilder MakeBuilder(
        ICurrentTenant? currentTenant = null,
        ITenantMembershipRepository? membershipRepo = null,
        IJwtTokenGenerator? jwtGenerator = null,
        IPermissionResolver? permissionResolver = null,
        ITenantLicenseRepository? licenseRepo = null)
        => new(
            currentTenant ?? new TestCurrentTenant(Guid.Empty),
            membershipRepo ?? Substitute.For<ITenantMembershipRepository>(),
            jwtGenerator ?? Substitute.For<IJwtTokenGenerator>(),
            permissionResolver ?? Substitute.For<IPermissionResolver>(),
            licenseRepo ?? Substitute.For<ITenantLicenseRepository>());

    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnCurrentTenantMembership_When_TenantIsAvailable()
    {
        var userId = UserId.New();
        var tenantId = TenantId.From(Guid.NewGuid());
        var membership = TenantMembership.Create(userId, tenantId, RoleId.New(), Now);
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        membershipRepository.GetByUserAndTenantAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns(membership);

        var sut = MakeBuilder(currentTenant: new TestCurrentTenant(tenantId.Value), membershipRepo: membershipRepository);
        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().Be(membership);
    }

    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnFirstActiveMembership_When_NoTenantContext()
    {
        var userId = UserId.New();
        var membership = TenantMembership.Create(userId, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        membershipRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([membership]);

        var sut = MakeBuilder(membershipRepo: membershipRepository);
        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().Be(membership);
    }

    [Fact]
    public async Task ResolveMembershipAsync_Should_ReturnNull_When_NoMembershipsExist()
    {
        var userId = UserId.New();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        membershipRepository.ListByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([]);

        var sut = MakeBuilder(membershipRepo: membershipRepository);
        var result = await sut.ResolveMembershipAsync(userId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateLoginResponseAsync_Should_ReturnCompleteResponse()
    {
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var role = Role.CreateSystem(membership.RoleId, Role.PlatformAdmin, "Admin");

        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        var permissionResolver = Substitute.For<IPermissionResolver>();
        var licenseRepo = Substitute.For<ITenantLicenseRepository>();

        var expectedPermissions = RolePermissionCatalog.GetPermissionsForRole(role.Name);
        permissionResolver.ResolvePermissionsAsync(role.Id, role.Name, membership.TenantId, Arg.Any<CancellationToken>())
            .Returns(expectedPermissions);
        jwtGenerator.GenerateAccessToken(
            user, membership.TenantId,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<IReadOnlyCollection<string>?>())
            .Returns("access-token");
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        licenseRepo.GetByTenantIdAsync(membership.TenantId.Value, Arg.Any<CancellationToken>())
            .Returns((TenantLicense?)null);

        var sut = MakeBuilder(
            currentTenant: new TestCurrentTenant(membership.TenantId.Value),
            jwtGenerator: jwtGenerator,
            permissionResolver: permissionResolver,
            licenseRepo: licenseRepo);

        var result = await sut.CreateLoginResponseAsync(user, membership, role, "refresh-token", CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(3600);
        result.User.Email.Should().Be("alice@example.com");
        result.User.RoleName.Should().Be(Role.PlatformAdmin);
        result.User.Permissions.Should().NotBeEmpty();
    }

    // SaaS-01: capability injection tests ─────────────────────────────────

    [Fact]
    public async Task CreateLoginResponseAsync_Should_PassLicenseCapabilities_When_LicenseExists()
    {
        var user = User.CreateLocal(Email.Create("bob@example.com"), FullName.Create("Bob", "Smith"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var role = Role.CreateSystem(membership.RoleId, Role.Developer, "Dev");
        var license = TenantLicense.Provision(membership.TenantId.Value, TenantPlan.Professional, 10, Now, null, Now);

        IReadOnlyCollection<string>? capturedCapabilities = null;

        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtGenerator.GenerateAccessToken(
            user, membership.TenantId,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Do<IReadOnlyCollection<string>?>(c => capturedCapabilities = c))
            .Returns("token");

        var permissionResolver = Substitute.For<IPermissionResolver>();
        permissionResolver.ResolvePermissionsAsync(Arg.Any<RoleId>(), Arg.Any<string>(), Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var licenseRepo = Substitute.For<ITenantLicenseRepository>();
        licenseRepo.GetByTenantIdAsync(membership.TenantId.Value, Arg.Any<CancellationToken>()).Returns(license);

        var sut = MakeBuilder(
            currentTenant: new TestCurrentTenant(membership.TenantId.Value),
            jwtGenerator: jwtGenerator,
            permissionResolver: permissionResolver,
            licenseRepo: licenseRepo);

        await sut.CreateLoginResponseAsync(user, membership, role, "refresh", CancellationToken.None);

        capturedCapabilities.Should().NotBeNull();
        capturedCapabilities.Should().Contain(TenantCapabilities.ContractStudio,
            "Professional plan includes contract_studio capability");
        capturedCapabilities.Should().NotContain(TenantCapabilities.MultiRegion,
            "Professional plan does not include multi_region capability");
    }

    [Fact]
    public async Task CreateLoginResponseAsync_Should_FallBackToEnterprise_When_NoLicenseFound()
    {
        var user = User.CreateLocal(Email.Create("charlie@example.com"), FullName.Create("Charlie", "X"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var membership = TenantMembership.Create(user.Id, TenantId.From(Guid.NewGuid()), RoleId.New(), Now);
        var role = Role.CreateSystem(membership.RoleId, Role.Developer, "Dev");

        IReadOnlyCollection<string>? capturedCapabilities = null;

        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.AccessTokenLifetimeSeconds.Returns(3600);
        jwtGenerator.GenerateAccessToken(
            user, membership.TenantId,
            Arg.Any<IReadOnlyCollection<RoleId>>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Do<IReadOnlyCollection<string>?>(c => capturedCapabilities = c))
            .Returns("token");

        var permissionResolver = Substitute.For<IPermissionResolver>();
        permissionResolver.ResolvePermissionsAsync(Arg.Any<RoleId>(), Arg.Any<string>(), Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var licenseRepo = Substitute.For<ITenantLicenseRepository>();
        licenseRepo.GetByTenantIdAsync(membership.TenantId.Value, Arg.Any<CancellationToken>())
            .Returns((TenantLicense?)null);

        var sut = MakeBuilder(
            currentTenant: new TestCurrentTenant(membership.TenantId.Value),
            jwtGenerator: jwtGenerator,
            permissionResolver: permissionResolver,
            licenseRepo: licenseRepo);

        await sut.CreateLoginResponseAsync(user, membership, role, "refresh", CancellationToken.None);

        var enterpriseCapabilities = TenantCapabilities.ForPlan(TenantPlan.Enterprise);
        capturedCapabilities.Should().NotBeNull();
        capturedCapabilities.Should().BeEquivalentTo(enterpriseCapabilities,
            "unlicensed tenants fall back to Enterprise capabilities for backward compatibility");
    }
}
