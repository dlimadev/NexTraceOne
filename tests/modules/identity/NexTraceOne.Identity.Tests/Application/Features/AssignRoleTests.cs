using FluentAssertions;
using NSubstitute;
using NexTraceOne.Identity.Application.Abstractions;
using AssignRoleFeature = NexTraceOne.Identity.Application.Features.AssignRole.AssignRole;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature AssignRole.
/// </summary>
public sealed class AssignRoleTests
{
    [Fact]
    public async Task Handle_Should_CreateMembership_When_UserHasNoMembershipInTenant()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Developer access");
        var tenantId = Guid.NewGuid();

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        membershipRepository.GetByUserAndTenantAsync(user.Id, TenantId.From(tenantId), Arg.Any<CancellationToken>())
            .Returns((TenantMembership?)null);

        var sut = new AssignRoleFeature.Handler(userRepository, roleRepository, membershipRepository, new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(user.Id.Value, tenantId, role.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        membershipRepository.Received(1).Add(Arg.Any<TenantMembership>());
    }

    [Fact]
    public async Task Handle_Should_UpdateExistingMembership_When_UserAlreadyHasMembership()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var user = User.CreateLocal(Email.Create("alice@example.com"), FullName.Create("Alice", "Doe"), HashedPassword.FromPlainText("P@ssw0rd123"));
        var oldRole = Role.CreateSystem(RoleId.New(), Role.Viewer, "Viewer access");
        var newRole = Role.CreateSystem(RoleId.New(), Role.Developer, "Developer access");
        var tenantId = Guid.NewGuid();
        var existingMembership = TenantMembership.Create(user.Id, TenantId.From(tenantId), oldRole.Id, now);

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        roleRepository.GetByIdAsync(newRole.Id, Arg.Any<CancellationToken>()).Returns(newRole);
        membershipRepository.GetByUserAndTenantAsync(user.Id, TenantId.From(tenantId), Arg.Any<CancellationToken>())
            .Returns(existingMembership);

        var sut = new AssignRoleFeature.Handler(userRepository, roleRepository, membershipRepository, new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(user.Id.Value, tenantId, newRole.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Verifica que o papel foi atualizado no vínculo existente
        existingMembership.RoleId.Should().Be(newRole.Id);
        membershipRepository.DidNotReceive().Add(Arg.Any<TenantMembership>());
    }

    [Fact]
    public async Task Handle_Should_ReturnUserNotFound_When_UserDoesNotExist()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var sut = new AssignRoleFeature.Handler(
            userRepository,
            Substitute.For<IRoleRepository>(),
            Substitute.For<ITenantMembershipRepository>(),
            new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(userId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }
}
