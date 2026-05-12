using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using AssignRoleFeature = NexTraceOne.IdentityAccess.Application.Features.AssignRole.AssignRole;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature AssignRole.
/// Cobrem criação de vínculo, atualização de papel existente, erros e geração de SecurityEvent.
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
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        membershipRepository.GetByUserAndTenantAsync(user.Id, TenantId.From(tenantId), Arg.Any<CancellationToken>())
            .Returns((TenantMembership?)null);

        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        var sut = new AssignRoleFeature.Handler(userRepository, roleRepository, membershipRepository, securityEventRepository, securityEventTracker, new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(user.Id.Value, tenantId, role.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        membershipRepository.Received(1).Add(Arg.Any<TenantMembership>());
        // Verifica que o SecurityEvent de role change foi gerado
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.RoleAssigned));
        // Verifica que o evento foi rastreado para propagação ao Audit central
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.RoleAssigned));
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
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        roleRepository.GetByIdAsync(newRole.Id, Arg.Any<CancellationToken>()).Returns(newRole);
        membershipRepository.GetByUserAndTenantAsync(user.Id, TenantId.From(tenantId), Arg.Any<CancellationToken>())
            .Returns(existingMembership);

        var sut = new AssignRoleFeature.Handler(userRepository, roleRepository, membershipRepository, securityEventRepository, securityEventTracker, new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(user.Id.Value, tenantId, newRole.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Verifica que o papel foi atualizado no vínculo existente
        existingMembership.RoleId.Should().Be(newRole.Id);
        membershipRepository.DidNotReceive().Add(Arg.Any<TenantMembership>());
        // Verifica que o SecurityEvent de mudança de papel foi gerado
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.RoleAssigned));
        // Verifica que o evento foi rastreado para propagação ao Audit central
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.RoleAssigned));
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
            Substitute.For<ISecurityEventRepository>(),
            Substitute.For<ISecurityEventTracker>(),
            new TestDateTimeProvider(now));

        var result = await sut.Handle(new AssignRoleFeature.Command(userId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }
}

