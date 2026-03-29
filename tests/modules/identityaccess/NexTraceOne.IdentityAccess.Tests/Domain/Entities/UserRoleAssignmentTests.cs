using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes da entidade UserRoleAssignment — validam criação, vigência temporal,
/// ativação/desativação e regras de validação.
/// </summary>
public sealed class UserRoleAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 03, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId TestUserId = UserId.From(Guid.NewGuid());
    private static readonly TenantId TestTenantId = TenantId.From(Guid.NewGuid());
    private static readonly RoleId TestRoleId = RoleId.From(Guid.NewGuid());

    // ── Factory ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_Should_ReturnActiveAssignment_WithCorrectFields()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin@test.com");

        assignment.Id.Should().NotBe(default);
        assignment.UserId.Should().Be(TestUserId);
        assignment.TenantId.Should().Be(TestTenantId);
        assignment.RoleId.Should().Be(TestRoleId);
        assignment.AssignedAt.Should().Be(Now);
        assignment.AssignedBy.Should().Be("admin@test.com");
        assignment.IsActive.Should().BeTrue();
        assignment.ValidFrom.Should().BeNull();
        assignment.ValidUntil.Should().BeNull();
    }

    [Fact]
    public void Create_Should_AcceptTemporalValidity()
    {
        var from = Now.AddDays(1);
        var until = Now.AddDays(30);

        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin@test.com",
            validFrom: from, validUntil: until);

        assignment.ValidFrom.Should().Be(from);
        assignment.ValidUntil.Should().Be(until);
    }

    [Fact]
    public void Create_Should_ThrowWhenUserIdIsNull()
    {
        var act = () => UserRoleAssignment.Create(
            null!, TestTenantId, TestRoleId, Now, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowWhenTenantIdIsNull()
    {
        var act = () => UserRoleAssignment.Create(
            TestUserId, null!, TestRoleId, Now, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowWhenRoleIdIsNull()
    {
        var act = () => UserRoleAssignment.Create(
            TestUserId, TestTenantId, null!, Now, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowWhenAssignedByIsEmpty()
    {
        var act = () => UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowWhenValidUntilIsBeforeValidFrom()
    {
        var act = () => UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin",
            validFrom: Now.AddDays(10), validUntil: Now.AddDays(5));
        act.Should().Throw<ArgumentException>()
            .WithMessage("ValidUntil must be after ValidFrom.");
    }

    // ── IsEffectivelyActive ──────────────────────────────────────────────

    [Fact]
    public void IsEffectivelyActive_Should_ReturnTrue_WhenActiveAndNoTemporalRestriction()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");

        assignment.IsEffectivelyActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsEffectivelyActive_Should_ReturnFalse_WhenDeactivated()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");
        assignment.Deactivate();

        assignment.IsEffectivelyActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyActive_Should_ReturnFalse_WhenBeforeValidFrom()
    {
        var from = Now.AddDays(5);
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin",
            validFrom: from);

        assignment.IsEffectivelyActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyActive_Should_ReturnTrue_WhenAfterValidFrom()
    {
        var from = Now.AddDays(-5);
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin",
            validFrom: from);

        assignment.IsEffectivelyActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsEffectivelyActive_Should_ReturnFalse_WhenAfterValidUntil()
    {
        var until = Now.AddDays(-1);
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now.AddDays(-10), "admin",
            validUntil: until);

        assignment.IsEffectivelyActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyActive_Should_ReturnTrue_WhenWithinValidRange()
    {
        var from = Now.AddDays(-5);
        var until = Now.AddDays(5);
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now.AddDays(-10), "admin",
            validFrom: from, validUntil: until);

        assignment.IsEffectivelyActive(Now).Should().BeTrue();
    }

    // ── Deactivate / Activate ────────────────────────────────────────────

    [Fact]
    public void Deactivate_Should_SetIsActiveToFalse()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");

        assignment.Deactivate();

        assignment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_SetIsActiveToTrue()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");
        assignment.Deactivate();

        assignment.Activate();

        assignment.IsActive.Should().BeTrue();
    }

    // ── UpdateValidity ───────────────────────────────────────────────────

    [Fact]
    public void UpdateValidity_Should_ChangeTemporalRange()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");

        var newFrom = Now.AddDays(1);
        var newUntil = Now.AddDays(60);
        assignment.UpdateValidity(newFrom, newUntil);

        assignment.ValidFrom.Should().Be(newFrom);
        assignment.ValidUntil.Should().Be(newUntil);
    }

    [Fact]
    public void UpdateValidity_Should_AllowNullValues()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin",
            validFrom: Now.AddDays(1), validUntil: Now.AddDays(30));

        assignment.UpdateValidity(null, null);

        assignment.ValidFrom.Should().BeNull();
        assignment.ValidUntil.Should().BeNull();
    }

    [Fact]
    public void UpdateValidity_Should_ThrowWhenUntilBeforeFrom()
    {
        var assignment = UserRoleAssignment.Create(
            TestUserId, TestTenantId, TestRoleId, Now, "admin");

        var act = () => assignment.UpdateValidity(
            Now.AddDays(10), Now.AddDays(5));

        act.Should().Throw<ArgumentException>()
            .WithMessage("ValidUntil must be after ValidFrom.");
    }
}
