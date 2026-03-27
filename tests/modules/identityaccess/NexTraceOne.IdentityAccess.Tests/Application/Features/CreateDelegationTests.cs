using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using CreateDelegationFeature = NexTraceOne.IdentityAccess.Application.Features.CreateDelegation.CreateDelegation;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature CreateDelegation.
/// Cobre self-delegation prevention, validação de escopo e criação legítima de delegação.
/// </summary>
public sealed class CreateDelegationTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<string> ValidPermissions = ["workflow:approve", "promotion:read"];

    [Fact]
    public async Task Handle_Should_ReturnValidationError_When_SelfDelegationAttempted()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var delegationRepo = Substitute.For<IDelegationRepository>();
        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var securityEventRepo = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(userId.ToString());

        var sut = new CreateDelegationFeature.Handler(
            delegationRepo,
            membershipRepo,
            roleRepo,
            securityEventRepo,
            securityEventTracker,
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new CreateDelegationFeature.Command(
                DelegateeId: userId,
                Permissions: ValidPermissions,
                Reason: "Trying to delegate to myself.",
                ValidFrom: Now,
                ValidUntil: Now.AddDays(7)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Delegation.SelfNotAllowed");
        securityEventRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.DelegationToSelfDenied));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.DelegationToSelfDenied));
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_When_NonDelegablePermissionRequested()
    {
        var grantorId = Guid.NewGuid();
        var delegateeId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(grantorId.ToString());

        var sut = new CreateDelegationFeature.Handler(
            Substitute.For<IDelegationRepository>(),
            Substitute.For<ITenantMembershipRepository>(),
            Substitute.For<IRoleRepository>(),
            Substitute.For<ISecurityEventRepository>(),
            Substitute.For<ISecurityEventTracker>(),
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new CreateDelegationFeature.Command(
                DelegateeId: delegateeId,
                Permissions: ["identity:users:write"],
                Reason: "Trying to delegate admin permission.",
                ValidFrom: Now,
                ValidUntil: Now.AddDays(1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Delegation.SystemAdminNotAllowed");
    }

    [Fact]
    public async Task Handle_Should_CreateDelegation_When_ValidInputAndGrantorHasPermissions()
    {
        var grantorId = Guid.NewGuid();
        var delegateeId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleId = RoleId.New();
        var role = Role.CreateSystem(roleId, Role.TechLead, "Tech Lead");

        var membership = TenantMembership.Create(
            UserId.From(grantorId),
            TenantId.From(tenantId),
            roleId,
            Now);

        var delegationRepo = Substitute.For<IDelegationRepository>();
        var membershipRepo = Substitute.For<ITenantMembershipRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var securityEventRepo = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        membershipRepo.GetByUserAndTenantAsync(UserId.From(grantorId), TenantId.From(tenantId), Arg.Any<CancellationToken>())
            .Returns(membership);
        roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(grantorId.ToString());

        var sut = new CreateDelegationFeature.Handler(
            delegationRepo,
            membershipRepo,
            roleRepo,
            securityEventRepo,
            securityEventTracker,
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var techLeadPermissions = Role.GetPermissionsForRole(Role.TechLead);
        var allowedPermission = techLeadPermissions.First(p =>
            p is not "identity:users:write" and not "identity:roles:assign"
            and not "identity:sessions:revoke" and not "platform:settings:write");

        var result = await sut.Handle(
            new CreateDelegationFeature.Command(
                DelegateeId: delegateeId,
                Permissions: [allowedPermission],
                Reason: "Tech Lead on vacation, delegating approval rights for one week.",
                ValidFrom: Now,
                ValidUntil: Now.AddDays(7)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegationRepo.Received(1).Add(Arg.Any<Delegation>());
        securityEventRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.DelegationCreated));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.DelegationCreated));
    }
}
