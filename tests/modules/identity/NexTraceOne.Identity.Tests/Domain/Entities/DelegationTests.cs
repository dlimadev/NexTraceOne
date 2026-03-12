using FluentAssertions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate Delegation.
/// Cobre criação, revogação, expiração, proteção contra auto-delegação e vigência temporal.
/// </summary>
public sealed class DelegationTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_BeActive_When_ValidInputProvided()
    {
        var grantor = UserId.New();
        var delegatee = UserId.New();
        var tenantId = TenantId.New();
        var permissions = new List<string> { "workflow:approve", "promotion:promote" };

        var delegation = Delegation.Create(
            grantor, delegatee, tenantId, permissions,
            "Tech Lead vacation — delegating approval to senior dev.",
            Now, Now.AddDays(7), Now);

        delegation.Status.Should().Be(DelegationStatus.Active);
        delegation.GrantorId.Should().Be(grantor);
        delegation.DelegateeId.Should().Be(delegatee);
        delegation.DelegatedPermissions.Should().HaveCount(2);
        delegation.IsActiveAt(Now.AddDays(3)).Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Throw_When_SelfDelegation()
    {
        var user = UserId.New();

        var act = () => Delegation.Create(
            user, user, TenantId.New(), ["workflow:approve"],
            "Self delegation.", Now, Now.AddDays(1), Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot delegate*themselves*");
    }

    [Fact]
    public void Create_Should_Throw_When_ValidUntilIsBeforeValidFrom()
    {
        var act = () => Delegation.Create(
            UserId.New(), UserId.New(), TenantId.New(), ["workflow:approve"],
            "Reason.", Now.AddDays(5), Now, Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*validUntil*after*validFrom*");
    }

    [Fact]
    public void Create_Should_Throw_When_NoPermissionsProvided()
    {
        var act = () => Delegation.Create(
            UserId.New(), UserId.New(), TenantId.New(),
            Array.Empty<string>(), "Reason.", Now, Now.AddDays(1), Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*permission*delegated*");
    }

    [Fact]
    public void Revoke_Should_EndDelegation_When_Active()
    {
        var delegation = Delegation.Create(
            UserId.New(), UserId.New(), TenantId.New(), ["workflow:approve"],
            "Reason.", Now, Now.AddDays(7), Now);

        var revoker = UserId.New();
        delegation.Revoke(revoker, Now.AddDays(3));

        delegation.Status.Should().Be(DelegationStatus.Revoked);
        delegation.RevokedBy.Should().Be(revoker);
        delegation.IsActiveAt(Now.AddDays(4)).Should().BeFalse();
    }

    [Fact]
    public void Expire_Should_ChangeStatus_When_ValidUntilHasPassed()
    {
        var delegation = Delegation.Create(
            UserId.New(), UserId.New(), TenantId.New(), ["workflow:approve"],
            "Reason.", Now, Now.AddDays(7), Now);

        delegation.Expire(Now.AddDays(8));

        delegation.Status.Should().Be(DelegationStatus.Expired);
    }

    [Fact]
    public void IsActiveAt_Should_ReturnFalse_When_BeforeValidFrom()
    {
        var delegation = Delegation.Create(
            UserId.New(), UserId.New(), TenantId.New(), ["workflow:approve"],
            "Reason.", Now.AddDays(1), Now.AddDays(7), Now);

        delegation.IsActiveAt(Now).Should().BeFalse();
    }
}
