using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Infrastructure.Context;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Context;

/// <summary>
/// Testes para EnvironmentProfileResolver — resolve perfil operacional de ambientes.
/// Cobre resolução de perfil e validação de production-like status com isolamento de tenant.
/// </summary>
public sealed class EnvironmentProfileResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EnvironmentId EnvironmentId = EnvironmentId.New();

    private static DomainEnvironment CreateEnvironment(
        TenantId? tenantId = null,
        bool isActive = true,
        EnvironmentProfile profile = EnvironmentProfile.Development,
        bool? isProductionLike = null)
    {
        var env = DomainEnvironment.Create(
            tenantId ?? TenantId,
            "Test Environment",
            "test-env",
            0,
            Now,
            profile,
            isProductionLike: isProductionLike);

        if (!isActive)
            env.Deactivate();

        return env;
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_ReturnNull_WhenEnvironmentNotFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.ResolveProfileAsync(TenantId, EnvironmentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_ReturnNull_WhenEnvironmentBelongsToDifferentTenant()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var differentTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: differentTenantId, profile: EnvironmentProfile.Production);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.ResolveProfileAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_ReturnNull_WhenEnvironmentIsInactive()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: false, profile: EnvironmentProfile.Development);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.ResolveProfileAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_ReturnCorrectProfile_WhenEnvironmentValid()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(
            tenantId: TenantId,
            isActive: true,
            profile: EnvironmentProfile.Production);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.ResolveProfileAsync(TenantId, environment.Id);

        // Assert
        result.Should().Be(EnvironmentProfile.Production);
    }

    [Theory]
    [InlineData(EnvironmentProfile.Development)]
    [InlineData(EnvironmentProfile.Validation)]
    [InlineData(EnvironmentProfile.Staging)]
    [InlineData(EnvironmentProfile.Production)]
    [InlineData(EnvironmentProfile.DisasterRecovery)]
    [InlineData(EnvironmentProfile.Sandbox)]
    [InlineData(EnvironmentProfile.Training)]
    [InlineData(EnvironmentProfile.UserAcceptanceTesting)]
    public async Task ResolveProfileAsync_Should_ReturnAllProfiles(EnvironmentProfile profile)
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true, profile: profile);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.ResolveProfileAsync(TenantId, environment.Id);

        // Assert
        result.Should().Be(profile);
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_EnforceMultipleTenantIsolation()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var environment = CreateEnvironment(tenantId: tenantA, profile: EnvironmentProfile.Production);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var resultForA = await resolver.ResolveProfileAsync(tenantA, environment.Id);
        var resultForB = await resolver.ResolveProfileAsync(tenantB, environment.Id);

        // Assert
        resultForA.Should().Be(EnvironmentProfile.Production);
        resultForB.Should().BeNull();
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_ReturnFalse_WhenEnvironmentNotFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, EnvironmentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_ReturnFalse_WhenEnvironmentBelongsToDifferentTenant()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var differentTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: differentTenantId, profile: EnvironmentProfile.Production, isProductionLike: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_ReturnFalse_WhenEnvironmentIsInactive()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: false, profile: EnvironmentProfile.Production, isProductionLike: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsProductionLikeAsync_Should_ReturnCorrectValue_WhenEnvironmentValid(bool isProductionLike)
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(
            tenantId: TenantId,
            isActive: true,
            profile: EnvironmentProfile.Production,
            isProductionLike: isProductionLike);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, environment.Id);

        // Assert
        result.Should().Be(isProductionLike);
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_AutoInferFromProfile_WhenIsProductionLikeNotExplicit()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        // Create with Production profile, which auto-sets IsProductionLike to true
        var environment = DomainEnvironment.Create(
            TenantId,
            "Prod",
            "prod",
            5,
            Now,
            EnvironmentProfile.Production);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_ReturnFalseForNonProductionProfiles()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = DomainEnvironment.Create(
            TenantId,
            "Dev",
            "dev",
            0,
            Now,
            EnvironmentProfile.Development);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new EnvironmentProfileResolver(repository);

        // Act
        var result = await resolver.IsProductionLikeAsync(TenantId, environment.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveProfileAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new EnvironmentProfileResolver(repository);
        var cts = new CancellationTokenSource();

        // Act
        await resolver.ResolveProfileAsync(TenantId, EnvironmentId, cts.Token);

        // Assert
        await repository.Received(1).GetByIdAsync(Arg.Any<EnvironmentId>(), cts.Token);
    }

    [Fact]
    public async Task IsProductionLikeAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new EnvironmentProfileResolver(repository);
        var cts = new CancellationTokenSource();

        // Act
        await resolver.IsProductionLikeAsync(TenantId, EnvironmentId, cts.Token);

        // Assert
        await repository.Received(1).GetByIdAsync(Arg.Any<EnvironmentId>(), cts.Token);
    }
}


