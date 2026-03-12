using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Application.Features.ListMyTenants;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature ListMyTenants — valida resolução de tenants do usuário autenticado.
/// Cobre cenários de múltiplos tenants, tenant único e usuário sem memberships.
/// </summary>
public sealed class ListMyTenantsTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnTenantList_When_UserHasMultipleMemberships()
    {
        var userId = UserId.New();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(userId.Value.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var tenantA = Tenant.Create("Org Alpha", "alpha", Now);
        var tenantB = Tenant.Create("Org Beta", "beta", Now);

        var roleA = Role.CreateSystem(RoleId.New(), Role.Developer, "Dev");
        var roleB = Role.CreateSystem(RoleId.New(), Role.Viewer, "Viewer");

        var membershipA = TenantMembership.Create(userId, tenantA.Id, roleA.Id, Now);
        var membershipB = TenantMembership.Create(userId, tenantB.Id, roleB.Id, Now);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns([membershipA, membershipB]);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<TenantId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TenantId, Tenant> { [tenantA.Id] = tenantA, [tenantB.Id] = tenantB });

        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<RoleId, Role> { [roleA.Id] = roleA, [roleB.Id] = roleB });

        var handler = new ListMyTenants.Handler(currentUser, membershipRepo, tenantRepo, roleRepo);
        var result = await handler.Handle(new ListMyTenants.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(t => t.Name == "Org Alpha" && t.RoleName == Role.Developer);
        result.Value.Should().Contain(t => t.Name == "Org Beta" && t.RoleName == Role.Viewer);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_UserHasNoMemberships()
    {
        var userId = UserId.New();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(userId.Value.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        membershipRepo.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TenantMembership>());

        var tenantRepo = Substitute.For<ITenantRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();

        var handler = new ListMyTenants.Handler(currentUser, membershipRepo, tenantRepo, roleRepo);
        var result = await handler.Handle(new ListMyTenants.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserNotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(string.Empty);

        var handler = new ListMyTenants.Handler(
            currentUser,
            Substitute.For<ITenantMembershipRepository>(),
            Substitute.For<ITenantRepository>(),
            Substitute.For<IRoleRepository>());

        var result = await handler.Handle(new ListMyTenants.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.NotAuthenticated");
    }
}
