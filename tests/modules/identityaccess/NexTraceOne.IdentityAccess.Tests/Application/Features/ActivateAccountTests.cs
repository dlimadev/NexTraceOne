using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using ActivateAccountFeature = NexTraceOne.IdentityAccess.Application.Features.ActivateAccount.ActivateAccount;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

public sealed class ActivateAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);

    private static User MakeUser(bool active = false)
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("OldP@ss1"));
        if (!active) user.Deactivate();
        return user;
    }

    private static (string Raw, string Hash) MakeToken()
    {
        var raw = "TestActivationToken123";
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }

    [Fact]
    public async Task Handle_Should_ActivateUser_And_SetPassword_When_TokenIsValid()
    {
        var user = MakeUser(active: false);
        var (rawToken, tokenHash) = MakeToken();
        var token = AccountActivationToken.Create(user.Id, tokenHash, Now.AddHours(-1));

        var tokenRepo = Substitute.For<IAccountActivationTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Hash("NewP@ss1").Returns("hashed_new_password");

        var handler = new ActivateAccountFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ActivateAccountFeature.Command(rawToken, "NewP@ss1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Activated.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        token.IsUsed.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenNotFound()
    {
        var tokenRepo = Substitute.For<IAccountActivationTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AccountActivationToken?)null);

        var handler = new ActivateAccountFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ActivateAccountFeature.Command("invalid_token", "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("account.activation.token_invalid");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenIsExpired()
    {
        var user = MakeUser();
        var (rawToken, tokenHash) = MakeToken();
        var expiredToken = AccountActivationToken.Create(user.Id, tokenHash, Now.AddDays(-3));

        var tokenRepo = Substitute.For<IAccountActivationTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expiredToken);

        var handler = new ActivateAccountFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ActivateAccountFeature.Command(rawToken, "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("account.activation.token_invalid");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenIsAlreadyUsed()
    {
        var user = MakeUser();
        var (rawToken, tokenHash) = MakeToken();
        var usedToken = AccountActivationToken.Create(user.Id, tokenHash, Now.AddHours(-2));
        usedToken.MarkUsed(Now.AddHours(-1));

        var tokenRepo = Substitute.For<IAccountActivationTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usedToken);

        var handler = new ActivateAccountFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ActivateAccountFeature.Command(rawToken, "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("account.activation.token_invalid");
    }
}
