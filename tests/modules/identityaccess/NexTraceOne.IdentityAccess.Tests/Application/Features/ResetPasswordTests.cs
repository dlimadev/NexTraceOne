using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using ResetPasswordFeature = NexTraceOne.IdentityAccess.Application.Features.ResetPassword.ResetPassword;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

public sealed class ResetPasswordTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);

    private static User MakeActiveUser() =>
        User.CreateLocal(
            Email.Create("bob@example.com"),
            FullName.Create("Bob", "Smith"),
            HashedPassword.FromPlainText("OldP@ss1"));

    private static (string Raw, string Hash) MakeToken()
    {
        var raw = "TestResetToken456";
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }

    [Fact]
    public async Task Handle_Should_UpdatePassword_When_TokenIsValid()
    {
        var user = MakeActiveUser();
        var (rawToken, tokenHash) = MakeToken();
        var token = PasswordResetToken.Create(user.Id, tokenHash, Now.AddMinutes(-10));

        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Hash("NewSecureP@ss!").Returns("hashed_new_pass");

        var handler = new ResetPasswordFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ResetPasswordFeature.Command(rawToken, "NewSecureP@ss!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        token.IsUsed.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenNotFound()
    {
        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var handler = new ResetPasswordFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ResetPasswordFeature.Command("wrong_token", "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("password.reset.token_invalid");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenIsExpired()
    {
        var user = MakeActiveUser();
        var (rawToken, tokenHash) = MakeToken();
        var expiredToken = PasswordResetToken.Create(user.Id, tokenHash, Now.AddHours(-2));

        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expiredToken);

        var handler = new ResetPasswordFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ResetPasswordFeature.Command(rawToken, "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("password.reset.token_invalid");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TokenWasAlreadyUsed()
    {
        var user = MakeActiveUser();
        var (rawToken, tokenHash) = MakeToken();
        var usedToken = PasswordResetToken.Create(user.Id, tokenHash, Now.AddMinutes(-30));
        usedToken.MarkUsed(Now.AddMinutes(-20));

        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();

        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usedToken);

        var handler = new ResetPasswordFeature.Handler(
            tokenRepo, userRepo, passwordHasher, unitOfWork, new TestDateTimeProvider(Now));

        var result = await handler.Handle(
            new ResetPasswordFeature.Command(rawToken, "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("password.reset.token_invalid");
    }
}
