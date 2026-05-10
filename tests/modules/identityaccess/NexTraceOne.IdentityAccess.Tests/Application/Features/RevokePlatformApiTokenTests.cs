using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.RevokePlatformApiToken;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler RevokePlatformApiToken.
/// Cobre: token não encontrado, token já revogado/expirado, revogação bem-sucedida,
/// validação de entrada.
/// </summary>
public sealed class RevokePlatformApiTokenTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string UserId = Guid.NewGuid().ToString();

    private static (
        IPlatformApiTokenRepository repository,
        IIdentityAccessUnitOfWork unitOfWork,
        RevokePlatformApiToken.Handler handler) CreateHandler()
    {
        var repository = Substitute.For<IPlatformApiTokenRepository>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();
        var clock = new TestDateTimeProvider(FixedNow);

        var handler = new RevokePlatformApiToken.Handler(repository, unitOfWork, clock);

        return (repository, unitOfWork, handler);
    }

    private static PlatformApiToken CreateActiveToken(DateTimeOffset? expiresAt = null)
        => PlatformApiToken.Create(
            tenantId: TenantId,
            name: "test-token",
            tokenHash: "abc123hash",
            tokenPrefix: "abc123ab",
            scope: PlatformApiTokenScope.ReadWrite,
            createdBy: UserId,
            createdAt: FixedNow.AddDays(-1),
            expiresAt: expiresAt);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTokenDoesNotExist()
    {
        var (repository, _, handler) = CreateHandler();
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns((PlatformApiToken?)null);

        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(Guid.NewGuid(), "security audit"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Contain("not_found");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenTokenAlreadyRevoked()
    {
        var (repository, _, handler) = CreateHandler();
        var token = CreateActiveToken();
        token.Revoke("previous reason", FixedNow.AddHours(-1));
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "another reason"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Business);
        result.Error.Code.Should().Contain("already_inactive");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenTokenExpired()
    {
        var (repository, _, handler) = CreateHandler();
        // expires in the past
        var token = CreateActiveToken(expiresAt: FixedNow.AddDays(-1));
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "expired cleanup"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Business);
    }

    [Fact]
    public async Task Handle_Should_RevokeToken_WhenActive()
    {
        var (repository, _, handler) = CreateHandler();
        var token = CreateActiveToken();
        token.IsActive(FixedNow).Should().BeTrue("precondition");
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "security audit"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.IsActive(FixedNow).Should().BeFalse("token must be revoked");
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnRevokedAt_InResponse()
    {
        var (repository, _, handler) = CreateHandler();
        var token = CreateActiveToken();
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "cleanup"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RevokedAt.Should().Be(FixedNow);
        result.Value.TokenId.Should().Be(token.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_CallUpdateAndCommit_OnSuccess()
    {
        var (repository, unitOfWork, handler) = CreateHandler();
        var token = CreateActiveToken();
        repository.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);

        await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "cleanup"),
            CancellationToken.None);

        await repository.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyTokenId()
    {
        var validator = new RevokePlatformApiToken.Validator();

        var result = validator.Validate(new RevokePlatformApiToken.Command(Guid.Empty, "reason"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RevokePlatformApiToken.Command.TokenId));
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyReason()
    {
        var validator = new RevokePlatformApiToken.Validator();

        var result = validator.Validate(new RevokePlatformApiToken.Command(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RevokePlatformApiToken.Command.Reason));
    }

    [Fact]
    public async Task Validator_Should_RejectTooLongReason()
    {
        var validator = new RevokePlatformApiToken.Validator();
        var longReason = new string('x', 501);

        var result = validator.Validate(new RevokePlatformApiToken.Command(Guid.NewGuid(), longReason));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RevokePlatformApiToken.Command.Reason));
    }
}
