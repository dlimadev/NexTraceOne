using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.CreatePlatformApiToken;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler CreatePlatformApiToken.
/// Cobre: geração de token seguro (apenas hash persistido, raw exposto uma vez),
/// prefixo de 8 caracteres, expiração opcional, validação de entrada.
/// </summary>
public sealed class CreatePlatformApiTokenTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static (
        IPlatformApiTokenRepository repository,
        IIdentityAccessUnitOfWork unitOfWork,
        CreatePlatformApiToken.Handler handler) CreateHandler(
            Guid? userId = null, Guid? tenantId = null)
    {
        var repository = Substitute.For<IPlatformApiTokenRepository>();
        var unitOfWork = Substitute.For<IIdentityAccessUnitOfWork>();
        var clock = new TestDateTimeProvider(FixedNow);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns((userId ?? UserId).ToString());
        currentUser.IsAuthenticated.Returns(true);

        var currentTenant = new TestCurrentTenant(tenantId ?? TenantId);

        var handler = new CreatePlatformApiToken.Handler(
            repository, unitOfWork, currentUser, currentTenant, clock);

        return (repository, unitOfWork, handler);
    }

    [Fact]
    public async Task Handle_Should_ReturnRawToken_OnlyInResponse()
    {
        var (repository, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("ci-pipeline", PlatformApiTokenScope.ReadWrite),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RawToken.Should().NotBeNullOrEmpty(
            "raw token must be returned to caller exactly once");
    }

    [Fact]
    public async Task Handle_Should_PersistOnlyHash_NeverRawToken()
    {
        var (repository, _, handler) = CreateHandler();
        PlatformApiToken? persisted = null;
        await repository.AddAsync(Arg.Do<PlatformApiToken>(t => persisted = t), Arg.Any<CancellationToken>());

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("ci-pipeline", PlatformApiTokenScope.ReadWrite),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted.Should().NotBeNull();
        persisted!.TokenHash.Should().NotBe(result.Value.RawToken,
            "only the hash must be stored — never the raw token");
        persisted.TokenHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_Should_SetTokenPrefix_ToFirst8Chars_OfRawToken()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("agent-v2", PlatformApiTokenScope.Read),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenPrefix.Should().HaveLength(8);
        result.Value.RawToken.Should().StartWith(result.Value.TokenPrefix);
    }

    [Fact]
    public async Task Handle_Should_SetNoExpiry_WhenExpiresInDaysOmitted()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("permanent-agent", PlatformApiTokenScope.Read),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeNull("token without ExpiresInDays must not expire");
    }

    [Fact]
    public async Task Handle_Should_SetExpiry_WhenExpiresInDaysProvided()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("short-lived", PlatformApiTokenScope.ReadWrite, ExpiresInDays: 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().Be(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_Should_RecordCreatedAt_FromClock()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("timing-token", PlatformApiTokenScope.Read),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnUrlSafeRawToken()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("safe-token", PlatformApiTokenScope.Admin),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RawToken.Should().NotContain("+", "raw token must be URL-safe");
        result.Value.RawToken.Should().NotContain("/", "raw token must be URL-safe");
        result.Value.RawToken.Should().NotContain("=", "raw token must not contain padding");
    }

    [Fact]
    public async Task Handle_Should_GenerateUniqueTokens_AcrossMultipleCalls()
    {
        var (_, _, handler) = CreateHandler();
        var cmd = new CreatePlatformApiToken.Command("agent", PlatformApiTokenScope.Read);

        var r1 = await handler.Handle(cmd, CancellationToken.None);
        var r2 = await handler.Handle(cmd, CancellationToken.None);

        r1.Value.RawToken.Should().NotBe(r2.Value.RawToken,
            "each token must be cryptographically unique");
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyName()
    {
        var validator = new CreatePlatformApiToken.Validator();

        var result = validator.Validate(new CreatePlatformApiToken.Command("", PlatformApiTokenScope.Read));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePlatformApiToken.Command.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    public async Task Validator_Should_RejectInvalidExpiresInDays(int days)
    {
        var validator = new CreatePlatformApiToken.Validator();

        var result = validator.Validate(
            new CreatePlatformApiToken.Command("token", PlatformApiTokenScope.Read, ExpiresInDays: days));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreatePlatformApiToken.Command.ExpiresInDays));
    }

    [Fact]
    public async Task Validator_Should_AcceptNullExpiresInDays()
    {
        var validator = new CreatePlatformApiToken.Validator();

        var result = validator.Validate(
            new CreatePlatformApiToken.Command("token", PlatformApiTokenScope.Read, ExpiresInDays: null));

        result.IsValid.Should().BeTrue();
    }
}

