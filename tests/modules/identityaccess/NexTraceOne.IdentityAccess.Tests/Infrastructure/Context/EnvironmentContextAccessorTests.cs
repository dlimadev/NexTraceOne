using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Infrastructure.Context;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Context;

/// <summary>
/// Testes para EnvironmentContextAccessor — accessor scoped do contexto de ambiente ativo.
/// Cobre estado padrão, configuração de contexto e atualização de valores.
/// </summary>
public sealed class EnvironmentContextAccessorTests
{
    [Fact]
    public void Constructor_Should_InitializeWithDefaultState()
    {
        // Arrange & Act
        var accessor = new EnvironmentContextAccessor();

        // Assert
        accessor.IsResolved.Should().BeFalse();
        accessor.EnvironmentId.Value.Should().Be(Guid.Empty);
        accessor.Profile.Should().Be(EnvironmentProfile.Development);
        accessor.IsProductionLike.Should().BeFalse();
    }

    [Fact]
    public void Set_Should_UpdateAllContextValues_WhenCalled()
    {
        // Arrange
        var accessor = new EnvironmentContextAccessor();
        var environmentId = EnvironmentId.New();
        var profile = EnvironmentProfile.Production;
        var isProductionLike = true;

        // Act
        accessor.Set(environmentId, profile, isProductionLike);

        // Assert
        accessor.IsResolved.Should().BeTrue();
        accessor.EnvironmentId.Should().Be(environmentId);
        accessor.Profile.Should().Be(profile);
        accessor.IsProductionLike.Should().Be(isProductionLike);
    }

    [Fact]
    public void Set_Should_UpdateContextValues_WhenCalledMultipleTimes()
    {
        // Arrange
        var accessor = new EnvironmentContextAccessor();
        var firstEnvironmentId = EnvironmentId.New();
        var secondEnvironmentId = EnvironmentId.New();

        // Act - First Set
        accessor.Set(firstEnvironmentId, EnvironmentProfile.Development, false);
        accessor.IsResolved.Should().BeTrue();
        accessor.EnvironmentId.Should().Be(firstEnvironmentId);
        accessor.Profile.Should().Be(EnvironmentProfile.Development);
        accessor.IsProductionLike.Should().BeFalse();

        // Act - Second Set
        accessor.Set(secondEnvironmentId, EnvironmentProfile.Production, true);

        // Assert
        accessor.IsResolved.Should().BeTrue();
        accessor.EnvironmentId.Should().Be(secondEnvironmentId);
        accessor.Profile.Should().Be(EnvironmentProfile.Production);
        accessor.IsProductionLike.Should().BeTrue();
    }

    [Theory]
    [InlineData(EnvironmentProfile.Development, false)]
    [InlineData(EnvironmentProfile.Validation, false)]
    [InlineData(EnvironmentProfile.Staging, false)]
    [InlineData(EnvironmentProfile.Production, true)]
    [InlineData(EnvironmentProfile.DisasterRecovery, true)]
    [InlineData(EnvironmentProfile.Sandbox, false)]
    [InlineData(EnvironmentProfile.Training, false)]
    [InlineData(EnvironmentProfile.UserAcceptanceTesting, false)]
    public void Set_Should_StoreProfileAndIsProductionLike_Correctly(
        EnvironmentProfile profile, bool isProductionLike)
    {
        // Arrange
        var accessor = new EnvironmentContextAccessor();
        var environmentId = EnvironmentId.New();

        // Act
        accessor.Set(environmentId, profile, isProductionLike);

        // Assert
        accessor.Profile.Should().Be(profile);
        accessor.IsProductionLike.Should().Be(isProductionLike);
    }

    [Fact]
    public void IsResolved_Should_BeTrue_AfterSet()
    {
        // Arrange
        var accessor = new EnvironmentContextAccessor();

        // Act
        accessor.Set(EnvironmentId.New(), EnvironmentProfile.Staging, false);

        // Assert
        accessor.IsResolved.Should().BeTrue();
    }
}

