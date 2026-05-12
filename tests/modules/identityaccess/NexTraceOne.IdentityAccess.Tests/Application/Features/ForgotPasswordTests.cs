using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.ForgotPassword;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler ForgotPassword.
/// Cobre: prevenção de enumeração de email, geração de token seguro,
/// invalidação de token anterior, envio de email de reset.
/// </summary>
public sealed class ForgotPasswordTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);

    private static (
        IUserRepository userRepo,
        IPasswordResetTokenRepository tokenRepo,
        IIdentityNotifier notifier,
        IIdentityAccessUnitOfWork unitOfWork,
        ForgotPassword.Handler handler) CreateHandler()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        var notifier = Substitute.For<IIdentityNotifier>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();
        var clock = new TestDateTimeProvider(FixedNow);

        var handler = new ForgotPassword.Handler(
            userRepo, tokenRepo, notifier, unitOfWork, clock);

        return (userRepo, tokenRepo, notifier, unitOfWork, handler);
    }

    private static User CreateActiveUser()
        => User.CreateLocal(
            Email.Create("user@test.com"),
            FullName.Create("Test", "User"),
            HashedPassword.FromPlainText("Password123!"));

    [Fact]
    public async Task Handle_Should_ReturnAccepted_WhenUserNotFound()
    {
        var (userRepo, _, notifier, _, handler) = CreateHandler();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await handler.Handle(
            new ForgotPassword.Command("unknown@test.com"),
            CancellationToken.None);

        // Must return accepted to prevent email enumeration
        result.IsSuccess.Should().BeTrue();
        result.Value.Accepted.Should().BeTrue();
        await notifier.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnAccepted_WhenUserExists()
    {
        var (userRepo, _, _, _, handler) = CreateHandler();
        var user = CreateActiveUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await handler.Handle(
            new ForgotPassword.Command("user@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Accepted.Should().BeTrue("response must always be accepted to prevent enumeration");
    }

    [Fact]
    public async Task Handle_Should_InvalidatePreviousToken_BeforeCreatingNew()
    {
        var (userRepo, tokenRepo, _, _, handler) = CreateHandler();
        var user = CreateActiveUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await handler.Handle(new ForgotPassword.Command("user@test.com"), CancellationToken.None);

        await tokenRepo.Received(1).DeleteByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_PersistNewToken_WhenUserExists()
    {
        var (userRepo, tokenRepo, _, _, handler) = CreateHandler();
        var user = CreateActiveUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await handler.Handle(new ForgotPassword.Command("user@test.com"), CancellationToken.None);

        tokenRepo.Received(1).Add(Arg.Is<PasswordResetToken>(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task Handle_Should_SendResetEmail_WithRawToken()
    {
        var (userRepo, _, notifier, _, handler) = CreateHandler();
        var user = CreateActiveUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await handler.Handle(new ForgotPassword.Command("user@test.com"), CancellationToken.None);

        await notifier.Received(1).SendPasswordResetAsync(
            user.Email.Value,
            user.FullName.FirstName,
            Arg.Is<string>(t => !string.IsNullOrEmpty(t)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_GenerateUrlSafeToken_WithoutPaddingChars()
    {
        var (userRepo, tokenRepo, _, _, handler) = CreateHandler();
        var user = CreateActiveUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        string? capturedRaw = null;
        tokenRepo.Add(Arg.Do<PasswordResetToken>(_ => { }));

        // Capture the raw token sent to notifier
        var notifier = Substitute.For<IIdentityNotifier>();
        notifier.When(n => n.SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedRaw = ci.ArgAt<string>(2));

        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();
        var clock = new TestDateTimeProvider(FixedNow);
        var freshHandler = new ForgotPassword.Handler(
            userRepo, tokenRepo, notifier, unitOfWork, clock);

        await freshHandler.Handle(new ForgotPassword.Command("user@test.com"), CancellationToken.None);

        capturedRaw.Should().NotContain("+", "token must be URL-safe");
        capturedRaw.Should().NotContain("/", "token must be URL-safe");
        capturedRaw.Should().NotContain("=", "token must not contain padding");
    }

    [Fact]
    public async Task Handle_Should_NotCallDeleteOrAdd_WhenUserNotFound()
    {
        var (userRepo, tokenRepo, _, _, handler) = CreateHandler();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await handler.Handle(new ForgotPassword.Command("ghost@test.com"), CancellationToken.None);

        await tokenRepo.DidNotReceive().DeleteByUserIdAsync(
            Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        tokenRepo.DidNotReceive().Add(Arg.Any<PasswordResetToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign")]
    public async Task Validator_Should_RejectInvalidEmail(string email)
    {
        var validator = new ForgotPassword.Validator();

        var result = validator.Validate(new ForgotPassword.Command(email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ForgotPassword.Command.Email));
    }

    [Fact]
    public async Task Validator_Should_AcceptValidEmail()
    {
        var validator = new ForgotPassword.Validator();

        var result = validator.Validate(new ForgotPassword.Command("valid@example.com"));

        result.IsValid.Should().BeTrue();
    }
}


