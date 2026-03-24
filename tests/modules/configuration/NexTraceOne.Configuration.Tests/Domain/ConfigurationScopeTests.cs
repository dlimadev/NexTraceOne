using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para o enum ConfigurationScope.
/// Valida hierarquia de precedência e integridade dos valores.
/// </summary>
public sealed class ConfigurationScopeTests
{
    [Fact]
    public void ScopeHierarchy_SystemIsLowestPrecedence()
    {
        ((int)ConfigurationScope.System).Should().Be(0);
    }

    [Fact]
    public void ScopeHierarchy_UserIsHighestPrecedence()
    {
        ((int)ConfigurationScope.User).Should().Be(5);

        var allScopes = Enum.GetValues<ConfigurationScope>();
        var maxScope = allScopes.Max(s => (int)s);
        ((int)ConfigurationScope.User).Should().Be(maxScope);
    }

    [Fact]
    public void ScopeHierarchy_TenantOverridesSystem()
    {
        ((int)ConfigurationScope.Tenant).Should().BeGreaterThan((int)ConfigurationScope.System);
    }

    [Fact]
    public void ScopeHierarchy_EnvironmentOverridesTenant()
    {
        ((int)ConfigurationScope.Environment).Should().BeGreaterThan((int)ConfigurationScope.Tenant);
    }

    [Fact]
    public void AllScopes_ShouldHaveCorrectValues()
    {
        ((int)ConfigurationScope.System).Should().Be(0);
        ((int)ConfigurationScope.Tenant).Should().Be(1);
        ((int)ConfigurationScope.Environment).Should().Be(2);
        ((int)ConfigurationScope.Role).Should().Be(3);
        ((int)ConfigurationScope.Team).Should().Be(4);
        ((int)ConfigurationScope.User).Should().Be(5);
    }

    [Fact]
    public void AllScopes_ShouldHaveSixMembers()
    {
        var allScopes = Enum.GetValues<ConfigurationScope>();
        allScopes.Should().HaveCount(6);
    }

    [Fact]
    public void ScopeHierarchy_ShouldBeStrictlyAscending()
    {
        var expectedOrder = new[]
        {
            ConfigurationScope.System,
            ConfigurationScope.Tenant,
            ConfigurationScope.Environment,
            ConfigurationScope.Role,
            ConfigurationScope.Team,
            ConfigurationScope.User
        };

        for (var i = 1; i < expectedOrder.Length; i++)
        {
            ((int)expectedOrder[i]).Should().BeGreaterThan((int)expectedOrder[i - 1],
                $"{expectedOrder[i]} should have higher precedence than {expectedOrder[i - 1]}");
        }
    }
}
