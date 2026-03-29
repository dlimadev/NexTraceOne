using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using SeedFeature = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultRolePermissions.SeedDefaultRolePermissions;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários da feature SeedDefaultRolePermissions.
/// Cobrem cenários de sucesso, idempotência, roles sem catálogo,
/// e validação completa das contagens de permissões por papel.
/// </summary>
public sealed class SeedDefaultRolePermissionsTests
{
    private static readonly DateTimeOffset Now = new(2026, 03, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IRolePermissionRepository _rolePermissionRepository = Substitute.For<IRolePermissionRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = new TestDateTimeProvider(Now);

    private SeedFeature.Handler CreateSut() =>
        new(_roleRepository, _rolePermissionRepository, _dateTimeProvider);

    private static IReadOnlyList<Role> CreateAllSystemRoles()
    {
        return
        [
            Role.CreateSystem(RoleId.New(), Role.PlatformAdmin, "Full platform access"),
            Role.CreateSystem(RoleId.New(), Role.TechLead, "Technical leadership"),
            Role.CreateSystem(RoleId.New(), Role.Developer, "Developer access"),
            Role.CreateSystem(RoleId.New(), Role.Viewer, "Read-only access"),
            Role.CreateSystem(RoleId.New(), Role.Auditor, "Audit and compliance"),
            Role.CreateSystem(RoleId.New(), Role.SecurityReview, "Security review"),
            Role.CreateSystem(RoleId.New(), Role.ApprovalOnly, "Approval-only access")
        ];
    }

    // ── Cenário de sucesso: seed completo ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_SeedAllSystemRoles_When_NoPreviousMappingsExist()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(7);
        result.Value.TotalPermissionsCreated.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_Should_PersistCorrectPermissionCount_ForEachRole()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        // Calcula a contagem esperada a partir do catálogo
        var expectedTotal = roles.Sum(r => RolePermissionCatalog.GetPermissionsForRole(r.Name).Count);
        result.Value.TotalPermissionsCreated.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task Handle_Should_CallAddRangeAsync_ForEachRole()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        // Cada papel deve receber exatamente uma chamada AddRangeAsync
        await _rolePermissionRepository.Received(7).AddRangeAsync(
            Arg.Any<IEnumerable<RolePermission>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateRolePermissions_WithSystemSeedGrantedBy()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Viewer, "Read-only") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(rp => rp.GrantedBy == "system-seed").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CreateRolePermissions_WithNullTenantId()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Developer, "Dev") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(rp => rp.TenantId == null).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CreateRolePermissions_WithCorrectTimestamp()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Developer, "Dev") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(rp => rp.GrantedAt == Now).Should().BeTrue();
    }

    // ── Cenário de idempotência ────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_SkipRoles_When_MappingsAlreadyExist()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPermissionsCreated.Should().Be(0);
        await _rolePermissionRepository.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<RolePermission>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SeedOnlyNewRoles_When_SomeAlreadySeeded()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);

        // PlatformAdmin, TechLead e Developer já existem; restantes são novos
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[0].Id, null, Arg.Any<CancellationToken>()).Returns(true);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[1].Id, null, Arg.Any<CancellationToken>()).Returns(true);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[2].Id, null, Arg.Any<CancellationToken>()).Returns(true);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[3].Id, null, Arg.Any<CancellationToken>()).Returns(false);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[4].Id, null, Arg.Any<CancellationToken>()).Returns(false);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[5].Id, null, Arg.Any<CancellationToken>()).Returns(false);
        _rolePermissionRepository.HasMappingsForRoleAsync(roles[6].Id, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(4);
        await _rolePermissionRepository.Received(4).AddRangeAsync(
            Arg.Any<IEnumerable<RolePermission>>(),
            Arg.Any<CancellationToken>());
    }

    // ── Cenário de catálogo vazio ───────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_SkipRole_When_CatalogReturnsEmpty()
    {
        // Simula um papel custom que não está no catálogo estático
        var unknownRole = Role.CreateSystem(RoleId.New(), "CustomRole", "Custom");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { unknownRole });
        _rolePermissionRepository.HasMappingsForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPermissionsCreated.Should().Be(0);
    }

    // ── Cenário sem papéis no sistema ──────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnZero_When_NoSystemRolesExist()
    {
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role>());

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPermissionsCreated.Should().Be(0);
    }

    // ── Validação de contagens por papel individual ─────────────────────────

    [Theory]
    [InlineData(Role.PlatformAdmin)]
    [InlineData(Role.TechLead)]
    [InlineData(Role.Developer)]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Auditor)]
    [InlineData(Role.SecurityReview)]
    [InlineData(Role.ApprovalOnly)]
    public async Task Handle_Should_SeedCorrectPermissionCount_ForIndividualRole(string roleName)
    {
        var role = Role.CreateSystem(RoleId.New(), roleName, $"{roleName} access");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _rolePermissionRepository.HasMappingsForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var expectedCount = RolePermissionCatalog.GetPermissionsForRole(roleName).Count;
        result.Value.TotalPermissionsCreated.Should().Be(expectedCount);
        capturedEntities!.Count().Should().Be(expectedCount);
    }

    // ── Validação de permissões individuais ─────────────────────────────────

    [Fact]
    public async Task Handle_Should_SeedPlatformAdminWithCatalogPermissions()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.PlatformAdmin, "Admin");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _rolePermissionRepository.HasMappingsForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var expectedCodes = RolePermissionCatalog.GetPermissionsForRole(Role.PlatformAdmin);
        var seededCodes = capturedEntities!.Select(rp => rp.PermissionCode).ToList();
        seededCodes.Should().BeEquivalentTo(expectedCodes);
    }

    [Fact]
    public async Task Handle_Should_SeedDeveloperWithCatalogPermissions()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Developer");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _rolePermissionRepository.HasMappingsForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var expectedCodes = RolePermissionCatalog.GetPermissionsForRole(Role.Developer);
        var seededCodes = capturedEntities!.Select(rp => rp.PermissionCode).ToList();
        seededCodes.Should().BeEquivalentTo(expectedCodes);
    }

    // ── Validação de RoleId correto nos mapeamentos ─────────────────────────

    [Fact]
    public async Task Handle_Should_AssociateCorrectRoleId_InSeededPermissions()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.Auditor, "Auditor");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _rolePermissionRepository.HasMappingsForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<RolePermission>? capturedEntities = null;
        _rolePermissionRepository.AddRangeAsync(Arg.Do<IEnumerable<RolePermission>>(e => capturedEntities = e), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities!.All(rp => rp.RoleId == role.Id).Should().BeTrue();
    }
}
