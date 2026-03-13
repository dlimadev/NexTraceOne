using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;
using GrantEnvironmentAccessFeature = NexTraceOne.Identity.Application.Features.GrantEnvironmentAccess.GrantEnvironmentAccess;
using Environment = NexTraceOne.Identity.Domain.Entities.Environment;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature GrantEnvironmentAccess.
/// Cobre cenários de concessão de acesso a ambientes com validação de contexto,
/// verificação de ambiente ativo, nível de acesso válido e geração de SecurityEvent.
/// </summary>
public sealed class GrantEnvironmentAccessTests
{
    private readonly DateTimeOffset _now = new(2025, 03, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_GrantAccess_When_AllParametersAreValid()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var adminUserId = Guid.NewGuid();
        var targetUser = User.CreateLocal(
            Email.Create("dev@example.com"),
            FullName.Create("John", "Dev"),
            HashedPassword.FromPlainText("P@ssw0rd123"));
        var environment = Environment.Create(tenantId, "Development", "development", 0, _now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(adminUserId.ToString());
        currentUser.IsAuthenticated.Returns(true);

        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        userRepository.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var command = new GrantEnvironmentAccessFeature.Command(
            targetUser.Id.Value, environment.Id.Value, "write", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        environmentRepository.Received(1).AddAccess(Arg.Any<EnvironmentAccess>());
        securityEventRepository.Received(1).Add(Arg.Any<SecurityEvent>());
        securityEventTracker.Received(1).Track(Arg.Any<SecurityEvent>());
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_TenantContextIsMissing()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = new TestCurrentTenant(Guid.Empty);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var command = new GrantEnvironmentAccessFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), "read", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Tenant.ContextRequired");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_EnvironmentNotFound()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid().ToString());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        environmentRepository.GetByIdAsync(Arg.Any<EnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var environmentId = Guid.NewGuid();
        var command = new GrantEnvironmentAccessFeature.Command(
            Guid.NewGuid(), environmentId, "read", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Environment.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_EnvironmentIsNotActive()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var environment = Environment.Create(tenantId, "Staging", "staging", 1, _now);
        environment.Deactivate();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid().ToString());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var command = new GrantEnvironmentAccessFeature.Command(
            Guid.NewGuid(), environment.Id.Value, "read", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Environment.NotActive");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserNotFound()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var environment = Environment.Create(tenantId, "Development", "development", 0, _now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid().ToString());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var userId = Guid.NewGuid();
        var command = new GrantEnvironmentAccessFeature.Command(
            userId, environment.Id.Value, "write", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_AccessLevelIsInvalid()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var targetUser = User.CreateLocal(
            Email.Create("dev@example.com"),
            FullName.Create("John", "Dev"),
            HashedPassword.FromPlainText("P@ssw0rd123"));
        var environment = Environment.Create(tenantId, "Development", "development", 0, _now);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid().ToString());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var userRepository = Substitute.For<IUserRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        userRepository.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);

        var sut = new GrantEnvironmentAccessFeature.Handler(
            currentUser, currentTenant, userRepository, environmentRepository,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var command = new GrantEnvironmentAccessFeature.Command(
            targetUser.Id.Value, environment.Id.Value, "superadmin", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Environment.InvalidAccessLevel");
    }
}
