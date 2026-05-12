using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Infrastructure.Context;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Context;

/// <summary>
/// Testes para TenantEnvironmentContextResolver — resolve contexto operacional validando isolamento por tenant.
/// Cobre resolução de contexto individual, isolamento de tenant e listagem de ambientes ativos.
/// </summary>
public sealed class TenantEnvironmentContextResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EnvironmentId EnvironmentId = EnvironmentId.New();

    private static DomainEnvironment CreateEnvironment(
        TenantId? tenantId = null,
        bool isActive = true,
        EnvironmentProfile profile = EnvironmentProfile.Development)
    {
        var env = DomainEnvironment.Create(
            tenantId ?? TenantId,
            "Test Environment",
            "test-env",
            0,
            Now,
            profile);

        if (!isActive)
            env.Deactivate();

        return env;
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNull_WhenEnvironmentNotFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var result = await resolver.ResolveAsync(TenantId, EnvironmentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNull_WhenEnvironmentBelongsToDifferentTenant()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var differentTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: differentTenantId);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var result = await resolver.ResolveAsync(TenantId, EnvironmentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNull_WhenEnvironmentIsInactive()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: false);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var result = await resolver.ResolveAsync(TenantId, EnvironmentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnTenantEnvironmentContext_WhenAllValid()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(
            tenantId: TenantId,
            isActive: true,
            profile: EnvironmentProfile.Production);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var result = await resolver.ResolveAsync(TenantId, environment.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(TenantId);
        result.EnvironmentId.Should().Be(environment.Id);
        result.Profile.Should().Be(EnvironmentProfile.Production);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_Should_ValidateTenantIdMatch()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var correctTenantId = TenantId;
        var wrongTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: correctTenantId);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var result = await resolver.ResolveAsync(wrongTenantId, environment.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveContextsForTenantAsync_Should_ReturnEmptyList_WhenNoEnvironmentsFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.ListByTenantAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DomainEnvironment>().AsReadOnly());

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var results = await resolver.ListActiveContextsForTenantAsync(TenantId);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ListActiveContextsForTenantAsync_Should_ReturnOnlyActiveEnvironments()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();

        var activeEnv1 = CreateEnvironment(tenantId: TenantId, isActive: true, profile: EnvironmentProfile.Development);
        var inactiveEnv = CreateEnvironment(tenantId: TenantId, isActive: false, profile: EnvironmentProfile.Validation);
        var activeEnv2 = CreateEnvironment(tenantId: TenantId, isActive: true, profile: EnvironmentProfile.Production);

        var allEnvironments = new[] { activeEnv1, inactiveEnv, activeEnv2 }.AsReadOnly();

        repository.ListByTenantAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(allEnvironments);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var results = await resolver.ListActiveContextsForTenantAsync(TenantId);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(ctx => ctx.IsActive.Should().BeTrue());
        results.Select(ctx => ctx.EnvironmentId).Should().Contain(new[] { activeEnv1.Id, activeEnv2.Id });
    }

    [Fact]
    public async Task ListActiveContextsForTenantAsync_Should_ReturnContextsWithCorrectValues()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();

        var env1 = DomainEnvironment.Create(TenantId, "Dev", "dev", 0, Now, EnvironmentProfile.Development);
        var env2 = DomainEnvironment.Create(TenantId, "Prod", "prod", 10, Now, EnvironmentProfile.Production);

        var allEnvironments = new[] { env1, env2 }.AsReadOnly();

        repository.ListByTenantAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(allEnvironments);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var results = await resolver.ListActiveContextsForTenantAsync(TenantId);

        // Assert
        results.Should().HaveCount(2);
        results[0].TenantId.Should().Be(TenantId);
        results[0].EnvironmentId.Should().Be(env1.Id);
        results[0].Profile.Should().Be(EnvironmentProfile.Development);
        results[1].TenantId.Should().Be(TenantId);
        results[1].EnvironmentId.Should().Be(env2.Id);
        results[1].Profile.Should().Be(EnvironmentProfile.Production);
    }

    [Fact]
    public async Task ListActiveContextsForTenantAsync_Should_FilterOutInactiveEnvironments()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();

        var activeEnv = CreateEnvironment(tenantId: TenantId, isActive: true);
        var inactiveEnv = CreateEnvironment(tenantId: TenantId, isActive: false);

        var allEnvironments = new[] { activeEnv, inactiveEnv }.AsReadOnly();

        repository.ListByTenantAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(allEnvironments);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);

        // Act
        var results = await resolver.ListActiveContextsForTenantAsync(TenantId);

        // Assert
        results.Should().HaveCount(1);
        results[0].EnvironmentId.Should().Be(activeEnv.Id);
    }

    [Fact]
    public async Task ResolveAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);
        var cts = new CancellationTokenSource();

        // Act
        await resolver.ResolveAsync(TenantId, EnvironmentId, cts.Token);

        // Assert
        await repository.Received(1).GetByIdAsync(Arg.Any<EnvironmentId>(), cts.Token);
    }

    [Fact]
    public async Task ListActiveContextsForTenantAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.ListByTenantAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DomainEnvironment>().AsReadOnly());

        var resolver = new TenantEnvironmentContextResolver(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantEnvironmentContextResolver>.Instance);
        var cts = new CancellationTokenSource();

        // Act
        await resolver.ListActiveContextsForTenantAsync(TenantId, cts.Token);

        // Assert
        await repository.Received(1).ListByTenantAsync(TenantId, cts.Token);
    }
}


