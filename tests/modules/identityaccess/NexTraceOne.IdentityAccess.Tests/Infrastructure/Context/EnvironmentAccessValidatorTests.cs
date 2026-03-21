using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Infrastructure.Context;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Context;

/// <summary>
/// Testes para EnvironmentAccessValidator — valida acesso a ambientes por usuário.
/// Cobre validação de existência, isolamento de tenant, ativação e expiração de acesso.
/// </summary>
public sealed class EnvironmentAccessValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId UserId = UserId.New();
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EnvironmentId EnvironmentId = EnvironmentId.New();

    private static DomainEnvironment CreateEnvironment(
        TenantId? tenantId = null,
        bool isActive = true)
    {
        var env = DomainEnvironment.Create(
            tenantId ?? TenantId,
            "Test Environment",
            "test-env",
            0,
            Now,
            EnvironmentProfile.Development);

        if (!isActive)
            env.Deactivate();

        return env;
    }

    private static EnvironmentAccess CreateAccess(
        UserId? userId = null,
        TenantId? tenantId = null,
        EnvironmentId? environmentId = null,
        DateTimeOffset? expiresAt = null)
    {
        var grantedBy = UserId.New();
        // Only set a default expiration if one wasn't explicitly provided as null
        // We use a different approach: if creating without explicit value, use null (which means no expiration)
        // If an explicit null was passed (expiresAt: null), we still need a default for the Create method validation
        // So we'll create with a future date as the default, but callers can override
        var actualExpiresAt = expiresAt ?? Now.AddHours(24); // Default to 24 hours if not specified
        return EnvironmentAccess.Create(
            userId ?? UserId,
            tenantId ?? TenantId,
            environmentId ?? EnvironmentId,
            EnvironmentAccessLevel.Read,
            grantedBy,
            Now,
            actualExpiresAt);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnNotFoundError_WhenEnvironmentNotFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, EnvironmentId, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.NotFound");
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnForbiddenError_WhenEnvironmentWrongTenant()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var differentTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: differentTenantId, isActive: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.WrongTenant");
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnForbiddenError_WhenEnvironmentInactive()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: false);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.Inactive");
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnForbiddenError_WhenUserHasNoAccess()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentAccess?)null);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.AccessDenied");
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnForbiddenError_WhenAccessExpired()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var createdAt = Now;
        var expiresAt = createdAt.AddHours(1);
        var grantedBy = UserId.New();
        var access = EnvironmentAccess.Create(
            UserId,
            TenantId,
            environment.Id,
            EnvironmentAccessLevel.Read,
            grantedBy,
            createdAt,
            expiresAt);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);
        var checkTime = Now.AddHours(2); // Check after expiration

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, checkTime);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.AccessExpired");
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnOk_WhenAllValid()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var access = CreateAccess(
            userId: UserId,
            tenantId: TenantId,
            environmentId: environment.Id,
            expiresAt: Now.AddHours(1)); // Future expiration

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task ValidateAsync_Should_AllowAccessWithoutExpiration()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var grantedBy = UserId.New();
        var access = EnvironmentAccess.Create(
            UserId,
            TenantId,
            environment.Id,
            EnvironmentAccessLevel.Read,
            grantedBy,
            Now,
            null); // No expiration date

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);
        var futureDate = Now.AddDays(365);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, environment.Id, futureDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_CheckAccessAtSpecificTime()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var expirationTime = Now.AddHours(1);
        var access = CreateAccess(
            userId: UserId,
            tenantId: TenantId,
            environmentId: environment.Id,
            expiresAt: expirationTime);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);

        // Act - Before expiration
        var resultBefore = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now.AddMinutes(30));

        // Assert
        resultBefore.IsSuccess.Should().BeTrue();

        // Act - After expiration
        var resultAfter = await validator.ValidateAsync(UserId, TenantId, environment.Id, Now.AddHours(2));

        // Assert
        resultAfter.IsFailure.Should().BeTrue();
        resultAfter.Error.Code.Should().Be("Environment.AccessExpired");
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnFalse_WhenEnvironmentNotFound()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, EnvironmentId, Now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnFalse_WhenEnvironmentWrongTenant()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var differentTenantId = TenantId.New();
        var environment = CreateEnvironment(tenantId: differentTenantId, isActive: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnFalse_WhenEnvironmentInactive()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: false);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnFalse_WhenUserHasNoAccess()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentAccess?)null);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnFalse_WhenAccessExpired()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var createdAt = Now;
        var expiresAt = createdAt.AddHours(1);
        var grantedBy = UserId.New();
        var access = EnvironmentAccess.Create(
            UserId,
            TenantId,
            environment.Id,
            EnvironmentAccessLevel.Read,
            grantedBy,
            createdAt,
            expiresAt);

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);
        var checkTime = Now.AddHours(2); // Check after expiration

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, checkTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnTrue_WhenAllValid()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var access = CreateAccess(
            userId: UserId,
            tenantId: TenantId,
            environmentId: environment.Id,
            expiresAt: Now.AddHours(1));

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, Now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessAsync_Should_ReturnTrue_WhenAccessHasNoExpiration()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        var environment = CreateEnvironment(tenantId: TenantId, isActive: true);
        var grantedBy = UserId.New();
        var access = EnvironmentAccess.Create(
            UserId,
            TenantId,
            environment.Id,
            EnvironmentAccessLevel.Read,
            grantedBy,
            Now,
            null); // No expiration

        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(environment);
        repository.GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(access);

        var validator = new EnvironmentAccessValidator(repository);
        var futureDate = Now.AddDays(365);

        // Act
        var result = await validator.HasAccessAsync(UserId, TenantId, environment.Id, futureDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_CheckAllConditionsInOrder()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var validator = new EnvironmentAccessValidator(repository);

        // Act
        var result = await validator.ValidateAsync(UserId, TenantId, EnvironmentId, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Environment.NotFound");
        // Should not call GetAccessAsync if environment not found
        await repository.DidNotReceive().GetAccessAsync(Arg.Any<UserId>(), Arg.Any<TenantId>(), Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var validator = new EnvironmentAccessValidator(repository);
        var cts = new CancellationTokenSource();

        // Act
        await validator.ValidateAsync(UserId, TenantId, EnvironmentId, Now, cts.Token);

        // Assert
        await repository.Received(1).GetByIdAsync(Arg.Any<EnvironmentId>(), cts.Token);
    }

    [Fact]
    public async Task HasAccessAsync_Should_PassCancellationTokenToRepository()
    {
        // Arrange
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var validator = new EnvironmentAccessValidator(repository);
        var cts = new CancellationTokenSource();

        // Act
        await validator.HasAccessAsync(UserId, TenantId, EnvironmentId, Now, cts.Token);

        // Assert
        await repository.Received(1).GetByIdAsync(Arg.Any<EnvironmentId>(), cts.Token);
    }
}
