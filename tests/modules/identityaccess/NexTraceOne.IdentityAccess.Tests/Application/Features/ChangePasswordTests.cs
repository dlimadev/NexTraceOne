using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using ChangePasswordFeature = NexTraceOne.IdentityAccess.Application.Features.ChangePassword.ChangePassword;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature ChangePassword.
/// Cobre cenários de alteração de senha: sucesso, senha atual incorreta,
/// usuário federado, geração de SecurityEvent e auditoria.
/// </summary>
public sealed class ChangePasswordTests
{
    private readonly DateTimeOffset _now = new(2025, 03, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ChangePassword_When_CurrentPasswordIsCorrect()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("OldP@ss123"));

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(user.Id.Value.ToString());

        var tenantId = Guid.NewGuid();
        var currentTenant = new TestCurrentTenant(tenantId);
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("OldP@ss123", user.PasswordHash!.Value).Returns(true);
        passwordHasher.Hash("NewP@ss456").Returns("hashed-new-password");

        var sut = new ChangePasswordFeature.Handler(
            currentUser, currentTenant, userRepository, passwordHasher,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(
            new ChangePasswordFeature.Command("OldP@ss123", "NewP@ss456"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Verifica que SecurityEvent de sucesso foi gerado
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.PasswordChanged));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.PasswordChanged));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_CurrentPasswordIsIncorrect()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("OldP@ss123"));

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(user.Id.Value.ToString());

        var tenantId = Guid.NewGuid();
        var currentTenant = new TestCurrentTenant(tenantId);
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("WrongP@ss", user.PasswordHash!.Value).Returns(false);

        var sut = new ChangePasswordFeature.Handler(
            currentUser, currentTenant, userRepository, passwordHasher,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(
            new ChangePasswordFeature.Command("WrongP@ss", "NewP@ss456"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.CurrentPasswordInvalid");
        // Verifica que SecurityEvent de falha foi gerado
        securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.PasswordChangeFailed));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserIsFederated()
    {
        var user = User.CreateFederated(
            Email.Create("fed@example.com"),
            FullName.Create("Fed", "User"),
            "azure-ad",
            "ext-12345");

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(user.Id.Value.ToString());

        var tenantId = Guid.NewGuid();
        var currentTenant = new TestCurrentTenant(tenantId);
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new ChangePasswordFeature.Handler(
            currentUser, currentTenant, userRepository, passwordHasher,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(
            new ChangePasswordFeature.Command("any-pass", "NewP@ss456"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserNotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);

        var currentTenant = new TestCurrentTenant(Guid.NewGuid());
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var securityEventRepository = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();
        var dateTimeProvider = new TestDateTimeProvider(_now);

        var sut = new ChangePasswordFeature.Handler(
            currentUser, currentTenant, userRepository, passwordHasher,
            securityEventRepository, securityEventTracker, dateTimeProvider);

        var result = await sut.Handle(
            new ChangePasswordFeature.Command("old", "new12345"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.NotAuthenticated");
    }
}

