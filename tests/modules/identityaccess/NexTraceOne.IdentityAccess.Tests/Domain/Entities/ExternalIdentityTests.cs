using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para as entidades ExternalIdentity e SsoGroupMapping.
/// Cobre criação, atualização de grupos e ciclo de vida do mapeamento SSO.
/// </summary>
public sealed class ExternalIdentityTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_StoreProviderAndExternalId_When_Valid()
    {
        var identity = ExternalIdentity.Create(
            UserId.New(), "AzureAD", "azure-oid-12345",
            "user@company.com", Now);

        identity.Provider.Should().Be("AzureAD");
        identity.ExternalUserId.Should().Be("azure-oid-12345");
        identity.ExternalEmail.Should().Be("user@company.com");
        identity.LastSyncAt.Should().Be(Now);
    }

    [Fact]
    public void UpdateExternalGroups_Should_UpdateGroupsAndSyncTime()
    {
        var identity = ExternalIdentity.Create(
            UserId.New(), "Keycloak", "kc-user-1", null, Now);

        var groupsJson = """["admin-group","dev-group"]""";
        identity.UpdateExternalGroups(groupsJson, Now.AddHours(1));

        identity.ExternalGroupsJson.Should().Be(groupsJson);
        identity.LastSyncAt.Should().Be(Now.AddHours(1));
    }
}

/// <summary>
/// Testes de domínio para SsoGroupMapping.
/// </summary>
public sealed class SsoGroupMappingTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_BeActiveByDefault()
    {
        var mapping = SsoGroupMapping.Create(
            TenantId.New(), "AzureAD", "group-oid-123",
            "Engineering Team", RoleId.New(), Now);

        mapping.IsActive.Should().BeTrue();
        mapping.Provider.Should().Be("AzureAD");
        mapping.ExternalGroupId.Should().Be("group-oid-123");
    }

    [Fact]
    public void Deactivate_Should_DisableMapping()
    {
        var mapping = SsoGroupMapping.Create(
            TenantId.New(), "Okta", "okta-grp-1",
            "Platform Admins", RoleId.New(), Now);

        mapping.Deactivate();

        mapping.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ChangeRole_Should_UpdateRoleId()
    {
        var mapping = SsoGroupMapping.Create(
            TenantId.New(), "AzureAD", "group-1",
            "Developers", RoleId.New(), Now);

        var newRoleId = RoleId.New();
        mapping.ChangeRole(newRoleId);

        mapping.RoleId.Should().Be(newRoleId);
    }
}

