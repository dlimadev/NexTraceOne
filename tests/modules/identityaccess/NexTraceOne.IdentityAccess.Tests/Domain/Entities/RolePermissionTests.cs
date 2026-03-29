using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade RolePermission.
/// Valida criação, ativação/desativação e regras de negócio.
/// </summary>
public sealed class RolePermissionTests
{
    private static readonly DateTimeOffset Now = new(2025, 8, 20, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_SetPropertiesCorrectly_When_ValidInput()
    {
        // Arrange
        var roleId = RoleId.New();

        // Act
        var rp = RolePermission.Create(
            RolePermissionId.New(),
            roleId,
            "identity:users:read",
            null,
            Now,
            "admin@example.com");

        // Assert
        rp.RoleId.Should().Be(roleId);
        rp.PermissionCode.Should().Be("identity:users:read");
        rp.TenantId.Should().BeNull();
        rp.GrantedAt.Should().Be(Now);
        rp.GrantedBy.Should().Be("admin@example.com");
        rp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_SetTenantId_When_TenantSpecific()
    {
        var tenantId = TenantId.New();
        var rp = RolePermission.Create(
            RolePermissionId.New(),
            RoleId.New(),
            "contracts:write",
            tenantId,
            Now,
            null);

        rp.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_Should_ThrowArgumentNullException_When_RoleIdIsNull()
    {
        var act = () => RolePermission.Create(
            RolePermissionId.New(),
            null!,
            "identity:users:read",
            null,
            Now,
            null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_PermissionCodeIsEmpty()
    {
        var act = () => RolePermission.Create(
            RolePermissionId.New(),
            RoleId.New(),
            "",
            null,
            Now,
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse_When_Called()
    {
        var rp = RolePermission.Create(
            RolePermissionId.New(),
            RoleId.New(),
            "identity:users:read",
            null,
            Now,
            null);

        rp.Deactivate();

        rp.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrue_When_PreviouslyDeactivated()
    {
        var rp = RolePermission.Create(
            RolePermissionId.New(),
            RoleId.New(),
            "identity:users:read",
            null,
            Now,
            null);

        rp.Deactivate();
        rp.Activate();

        rp.IsActive.Should().BeTrue();
    }
}
