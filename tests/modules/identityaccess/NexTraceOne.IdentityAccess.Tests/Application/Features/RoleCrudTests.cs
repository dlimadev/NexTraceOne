using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

using CreateRoleFeature = NexTraceOne.IdentityAccess.Application.Features.CreateRole.CreateRole;
using UpdateRoleFeature = NexTraceOne.IdentityAccess.Application.Features.UpdateRole.UpdateRole;
using DeleteRoleFeature = NexTraceOne.IdentityAccess.Application.Features.DeleteRole.DeleteRole;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes CRUD de papéis customizados — parametrização de roles configuráveis por tenant.
/// </summary>
public sealed class RoleCrudTests
{
    private static (IRoleRepository repo, IUnitOfWork uow) CreateMocks()
    {
        var repo = Substitute.For<IRoleRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
        return (repo, uow);
    }

    // ── CreateRole ──────────────────────────────────────────

    [Fact]
    public async Task CreateRole_Should_Create_Custom_Role()
    {
        var (repo, uow) = CreateMocks();
        repo.GetByNameAsync("Reviewer", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new CreateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(new CreateRoleFeature.Command("Reviewer", "Reviews changes"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Reviewer");
        result.Value.IsSystem.Should().BeFalse();
        await repo.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRole_Should_Fail_When_Name_Already_Exists()
    {
        var (repo, uow) = CreateMocks();
        var existing = Role.CreateCustom("Reviewer", "Existing");
        repo.GetByNameAsync("Reviewer", Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new CreateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(new CreateRoleFeature.Command("Reviewer", "Reviews changes"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    // ── UpdateRole ──────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_Should_Update_Custom_Role()
    {
        var (repo, uow) = CreateMocks();
        var role = Role.CreateCustom("OldName", "Old description");
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);
        repo.GetByNameAsync("NewName", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new UpdateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(
            new UpdateRoleFeature.Command(role.Id.Value, "NewName", "New description"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("NewName");
        result.Value.Description.Should().Be("New description");
    }

    [Fact]
    public async Task UpdateRole_Should_Fail_For_System_Role()
    {
        var (repo, uow) = CreateMocks();
        var systemRole = Role.CreateSystem(RoleId.New(), "PlatformAdmin", "System admin");
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(systemRole);

        var sut = new UpdateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(
            new UpdateRoleFeature.Command(systemRole.Id.Value, "Hacked", "Hacked"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("SystemRole");
    }

    [Fact]
    public async Task UpdateRole_Should_Fail_When_Not_Found()
    {
        var (repo, uow) = CreateMocks();
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new UpdateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(
            new UpdateRoleFeature.Command(Guid.NewGuid(), "X", "Y"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task UpdateRole_Should_Fail_When_Name_Conflicts()
    {
        var (repo, uow) = CreateMocks();
        var role = Role.CreateCustom("OldName", "Old desc");
        var conflicting = Role.CreateCustom("NewName", "Other");
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);
        repo.GetByNameAsync("NewName", Arg.Any<CancellationToken>()).Returns(conflicting);

        var sut = new UpdateRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(
            new UpdateRoleFeature.Command(role.Id.Value, "NewName", "New desc"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameAlreadyExists");
    }

    // ── DeleteRole ──────────────────────────────────────────

    [Fact]
    public async Task DeleteRole_Should_Delete_Custom_Role()
    {
        var (repo, uow) = CreateMocks();
        var role = Role.CreateCustom("Reviewer", "Reviews");
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);

        var sut = new DeleteRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(new DeleteRoleFeature.Command(role.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).RemoveAsync(role, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRole_Should_Fail_For_System_Role()
    {
        var (repo, uow) = CreateMocks();
        var systemRole = Role.CreateSystem(RoleId.New(), "Developer", "Dev role");
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(systemRole);

        var sut = new DeleteRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(new DeleteRoleFeature.Command(systemRole.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("SystemRole");
    }

    [Fact]
    public async Task DeleteRole_Should_Fail_When_Not_Found()
    {
        var (repo, uow) = CreateMocks();
        repo.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new DeleteRoleFeature.Handler(repo, uow);
        var result = await sut.Handle(new DeleteRoleFeature.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
