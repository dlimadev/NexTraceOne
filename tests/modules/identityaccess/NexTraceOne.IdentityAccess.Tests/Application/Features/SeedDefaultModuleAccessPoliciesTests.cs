using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using SeedFeature = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultModuleAccessPolicies.SeedDefaultModuleAccessPolicies;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários da feature SeedDefaultModuleAccessPolicies.
/// Cobrem cenários de sucesso, idempotência, roles sem catálogo,
/// e validação completa das contagens de políticas por papel.
/// </summary>
public sealed class SeedDefaultModuleAccessPoliciesTests
{
    private static readonly DateTimeOffset Now = new(2026, 03, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IModuleAccessPolicyRepository _policyRepository = Substitute.For<IModuleAccessPolicyRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = new TestDateTimeProvider(Now);

    private SeedFeature.Handler CreateSut() =>
        new(_roleRepository, _policyRepository, _dateTimeProvider);

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
    public async Task Handle_Should_SeedAllSystemRoles_When_NoPreviousPoliciesExist()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(7);
        result.Value.TotalPoliciesCreated.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_Should_PersistCorrectPolicyCount_ForEachRole()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var expectedTotal = roles.Sum(r => ModuleAccessPolicyCatalog.GetPoliciesForRole(r.Name).Count);
        result.Value.TotalPoliciesCreated.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task Handle_Should_CallAddRangeAsync_ForEachRole()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        await _policyRepository.Received(7).AddRangeAsync(
            Arg.Any<IEnumerable<ModuleAccessPolicy>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreatePolicies_WithSystemSeedCreatedBy()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Viewer, "Read-only") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(p => p.CreatedBy == "system-seed").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CreatePolicies_WithNullTenantId()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Developer, "Dev") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(p => p.TenantId == null).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CreatePolicies_WithCorrectTimestamp()
    {
        var roles = new List<Role> { Role.CreateSystem(RoleId.New(), Role.Developer, "Dev") };
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.All(p => p.CreatedAt == Now).Should().BeTrue();
    }

    // ── Cenário de idempotência ────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_SkipRoles_When_PoliciesAlreadyExist()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);
        _policyRepository
            .GetPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var roleId = ci.Arg<RoleId>();
                var module = ci.Arg<string>();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                IReadOnlyList<ModuleAccessPolicy> policies = role is null
                    ? []
                    : ModuleAccessPolicyCatalog.GetPoliciesForRole(role.Name)
                        .Where(p => p.Module == module)
                        .Select(p => ModuleAccessPolicy.Create(role.Id, null, p.Module, p.Page, p.Action, p.IsAllowed, Now, "system-seed"))
                        .ToList();
                return Task.FromResult(policies);
            });

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPoliciesCreated.Should().Be(0);
        await _policyRepository.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<ModuleAccessPolicy>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SeedOnlyNewRoles_When_SomeAlreadySeeded()
    {
        var roles = CreateAllSystemRoles();
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(roles);

        // 3 papéis já existem, 4 são novos
        var seededRoleIds = new HashSet<RoleId> { roles[0].Id, roles[1].Id, roles[2].Id };
        _policyRepository
            .GetPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var roleId = ci.Arg<RoleId>();
                if (!seededRoleIds.Contains(roleId))
                    return Task.FromResult<IReadOnlyList<ModuleAccessPolicy>>([]);
                var module = ci.Arg<string>();
                var role = roles.FirstOrDefault(r => r.Id == roleId)!;
                return Task.FromResult<IReadOnlyList<ModuleAccessPolicy>>(
                    ModuleAccessPolicyCatalog.GetPoliciesForRole(role.Name)
                        .Where(p => p.Module == module)
                        .Select(p => ModuleAccessPolicy.Create(role.Id, null, p.Module, p.Page, p.Action, p.IsAllowed, Now, "system-seed"))
                        .ToList());
            });

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(4);
        await _policyRepository.Received(4).AddRangeAsync(
            Arg.Any<IEnumerable<ModuleAccessPolicy>>(),
            Arg.Any<CancellationToken>());
    }

    // ── Cenário sem papéis ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnZero_When_NoSystemRolesExist()
    {
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role>());

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPoliciesCreated.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_SkipRole_When_CatalogReturnsEmpty()
    {
        var unknownRole = Role.CreateSystem(RoleId.New(), "CustomRole", "Custom");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { unknownRole });
        _policyRepository.HasPoliciesForRoleAsync(Arg.Any<RoleId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolesSeeded.Should().Be(0);
        result.Value.TotalPoliciesCreated.Should().Be(0);
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
    public async Task Handle_Should_SeedCorrectPolicyCount_ForIndividualRole(string roleName)
    {
        var role = Role.CreateSystem(RoleId.New(), roleName, $"{roleName} access");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _policyRepository.HasPoliciesForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var expectedCount = ModuleAccessPolicyCatalog.GetPoliciesForRole(roleName).Count;
        result.Value.TotalPoliciesCreated.Should().Be(expectedCount);
        capturedEntities!.Count().Should().Be(expectedCount);
    }

    // ── Validação de módulos corretos nos mapeamentos ──────────────────────

    [Fact]
    public async Task Handle_Should_SeedPlatformAdminWithAllModules()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.PlatformAdmin, "Admin");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _policyRepository.HasPoliciesForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        var seededModules = capturedEntities!.Select(p => p.Module).Distinct().ToList();
        var expectedModules = ModuleAccessPolicyCatalog.GetAllModules();
        seededModules.Should().BeEquivalentTo(expectedModules);
    }

    // ── Validação de RoleId correto ────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_AssociateCorrectRoleId_InSeededPolicies()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.Auditor, "Auditor");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _policyRepository.HasPoliciesForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        capturedEntities!.All(p => p.RoleId == role.Id).Should().BeTrue();
    }

    // ── Validação de isAllowed correto ─────────────────────────────────────

    [Fact]
    public async Task Handle_Should_SetIsAllowedCorrectly_FromCatalog()
    {
        var role = Role.CreateSystem(RoleId.New(), Role.Viewer, "Viewer");
        _roleRepository.GetSystemRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _policyRepository.HasPoliciesForRoleAsync(role.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        IEnumerable<ModuleAccessPolicy>? capturedEntities = null;
        _policyRepository.AddRangeAsync(
            Arg.Do<IEnumerable<ModuleAccessPolicy>>(e => capturedEntities = e),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CreateSut().Handle(new SeedFeature.Command(), CancellationToken.None);

        // Viewer catalog has only IsAllowed=true entries
        capturedEntities!.All(p => p.IsAllowed).Should().BeTrue();
    }
}
