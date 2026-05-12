using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.CreatePlatformApiToken;
using NexTraceOne.IdentityAccess.Application.Features.GetAgentQueryAuditLog;
using NexTraceOne.IdentityAccess.Application.Features.ListPlatformApiTokens;
using NexTraceOne.IdentityAccess.Application.Features.RecordAgentQuery;
using NexTraceOne.IdentityAccess.Application.Features.RevokePlatformApiToken;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave D.4 — Agent-to-Agent Protocol.
/// Cobre criação, revogação, listagem de tokens e auditoria de queries de agentes.
/// </summary>
public sealed class AgentApiD4Tests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantGuid = Guid.NewGuid();
    private static readonly string UserId = "user-123";

    private static IDateTimeProvider CreateClock() =>
        Substitute.For<IDateTimeProvider>() is { } c
            ? (c.UtcNow.Returns(FixedNow), c).Item2
            : null!;

    private static ICurrentTenant CreateTenant() =>
        Substitute.For<ICurrentTenant>() is { } t
            ? (t.Id.Returns(TenantGuid), t).Item2
            : null!;

    private static ICurrentUser CreateUser() =>
        Substitute.For<ICurrentUser>() is { } u
            ? (u.Id.Returns(UserId), u).Item2
            : null!;

    private static PlatformApiToken MakeToken(
        string name = "ci-pipeline",
        PlatformApiTokenScope scope = PlatformApiTokenScope.Read,
        DateTimeOffset? expiresAt = null,
        bool revoke = false)
    {
        var token = PlatformApiToken.Create(TenantGuid, name, "hashvalue", "prefix12", scope, UserId, FixedNow, expiresAt);
        if (revoke) token.Revoke("test reason", FixedNow);
        return token;
    }

    // ── CreatePlatformApiToken ────────────────────────────────────────────

    [Fact]
    public async Task CreatePlatformApiToken_Creates_Token_With_Correct_Properties()
    {
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();
        var handler = new CreatePlatformApiToken.Handler(repo, uow, CreateUser(), CreateTenant(), CreateClock());

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("ci-agent", PlatformApiTokenScope.Read, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("ci-agent");
        result.Value.Scope.Should().Be(PlatformApiTokenScope.Read);
        result.Value.ExpiresAt.Should().BeNull();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<PlatformApiToken>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlatformApiToken_RawToken_Is_Not_Stored_In_Hash()
    {
        var capturedToken = (PlatformApiToken?)null;
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.AddAsync(Arg.Do<PlatformApiToken>(t => capturedToken = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new CreatePlatformApiToken.Handler(repo, uow, CreateUser(), CreateTenant(), CreateClock());
        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("my-token", PlatformApiTokenScope.ReadWrite, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedToken.Should().NotBeNull();
        capturedToken!.TokenHash.Should().NotBe(result.Value.RawToken);
        result.Value.RawToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreatePlatformApiToken_With_Expiry_Sets_ExpiresAt()
    {
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();
        var handler = new CreatePlatformApiToken.Handler(repo, uow, CreateUser(), CreateTenant(), CreateClock());

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("expiring-token", PlatformApiTokenScope.Read, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().Be(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task CreatePlatformApiToken_Without_Expiry_Leaves_ExpiresAt_Null()
    {
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();
        var handler = new CreatePlatformApiToken.Handler(repo, uow, CreateUser(), CreateTenant(), CreateClock());

        var result = await handler.Handle(
            new CreatePlatformApiToken.Command("permanent-token", PlatformApiTokenScope.Admin, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeNull();
    }

    // ── RevokePlatformApiToken ────────────────────────────────────────────

    [Fact]
    public async Task RevokePlatformApiToken_Revokes_Active_Token()
    {
        var token = MakeToken();
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>()).Returns(token);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new RevokePlatformApiToken.Handler(repo, uow, CreateClock());
        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "security policy"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenId.Should().Be(token.Id.Value);
        result.Value.RevokedAt.Should().Be(FixedNow);
        await repo.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokePlatformApiToken_Returns_Error_For_Unknown_Token()
    {
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>()).Returns((PlatformApiToken?)null);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new RevokePlatformApiToken.Handler(repo, uow, CreateClock());
        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(Guid.NewGuid(), "reason"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("platform_api_token.not_found");
    }

    [Fact]
    public async Task RevokePlatformApiToken_Returns_Error_For_Already_Revoked_Token()
    {
        var token = MakeToken(revoke: true);
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>()).Returns(token);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new RevokePlatformApiToken.Handler(repo, uow, CreateClock());
        var result = await handler.Handle(
            new RevokePlatformApiToken.Command(token.Id.Value, "again"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("platform_api_token.already_inactive");
    }

    // ── ListPlatformApiTokens ─────────────────────────────────────────────

    [Fact]
    public async Task ListPlatformApiTokens_Returns_Tenant_Tokens()
    {
        var tokens = new[] { MakeToken("ci-pipeline"), MakeToken("monitoring-agent") };
        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.ListByTenantAsync(TenantGuid, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<PlatformApiToken>)tokens);

        var handler = new ListPlatformApiTokens.Handler(repo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new ListPlatformApiTokens.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tokens.Should().HaveCount(2);
        result.Value.Tokens.Should().Contain(t => t.Name == "ci-pipeline");
        result.Value.Tokens.Should().Contain(t => t.Name == "monitoring-agent");
    }

    [Fact]
    public async Task ListPlatformApiTokens_Marks_Expired_Token_As_Inactive()
    {
        var expiredAt = FixedNow.AddDays(-1);
        var token = MakeToken("expired-token", expiresAt: expiredAt);

        var repo = Substitute.For<IPlatformApiTokenRepository>();
        repo.ListByTenantAsync(TenantGuid, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<PlatformApiToken>)[token]);

        var handler = new ListPlatformApiTokens.Handler(repo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new ListPlatformApiTokens.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tokens.Should().ContainSingle(t => t.IsActive == false);
    }

    // ── RecordAgentQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task RecordAgentQuery_Creates_Audit_Record()
    {
        var queryRepo = Substitute.For<IAgentQueryRepository>();
        var tokenRepo = Substitute.For<IPlatformApiTokenRepository>();
        tokenRepo.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns((PlatformApiToken?)null);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new RecordAgentQuery.Handler(queryRepo, tokenRepo, uow, CreateTenant(), CreateClock());
        var result = await handler.Handle(
            new RecordAgentQuery.Command(Guid.NewGuid(), "GetServiceCatalog", 200, 42),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutedAt.Should().Be(FixedNow);
        await queryRepo.Received(1).AddAsync(Arg.Any<AgentQueryRecord>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAgentQuery_Updates_Token_LastUsedAt()
    {
        var token = MakeToken();
        var queryRepo = Substitute.For<IAgentQueryRepository>();
        var tokenRepo = Substitute.For<IPlatformApiTokenRepository>();
        tokenRepo.GetByIdAsync(Arg.Any<PlatformApiTokenId>(), Arg.Any<CancellationToken>())
            .Returns(token);
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();

        var handler = new RecordAgentQuery.Handler(queryRepo, tokenRepo, uow, CreateTenant(), CreateClock());
        await handler.Handle(
            new RecordAgentQuery.Command(token.Id.Value, "GetContracts", 200, 10),
            CancellationToken.None);

        await tokenRepo.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
        token.LastUsedAt.Should().Be(FixedNow);
    }

    // ── GetAgentQueryAuditLog ─────────────────────────────────────────────

    [Fact]
    public async Task GetAgentQueryAuditLog_Returns_Empty_When_No_Records()
    {
        var repo = Substitute.For<IAgentQueryRepository>();
        repo.ListByTenantAsync(TenantGuid, Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AgentQueryRecord>)[]);

        var handler = new GetAgentQueryAuditLog.Handler(repo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetAgentQueryAuditLog.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAgentQueryAuditLog_Filters_By_Token_Id()
    {
        var tokenId = Guid.NewGuid();
        var record = AgentQueryRecord.Create(TenantGuid, tokenId, "GetCatalog", 200, 25, FixedNow);
        var repo = Substitute.For<IAgentQueryRepository>();
        repo.ListByTokenAsync(tokenId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AgentQueryRecord>)[record]);

        var handler = new GetAgentQueryAuditLog.Handler(repo, CreateTenant(), CreateClock());
        var result = await handler.Handle(
            new GetAgentQueryAuditLog.Query(TokenId: tokenId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.TokenId == tokenId);
        await repo.Received(1).ListByTokenAsync(tokenId, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Domain Entity tests ───────────────────────────────────────────────

    [Fact]
    public void PlatformApiToken_IsActive_Returns_True_Before_Expiry()
    {
        var token = MakeToken(expiresAt: FixedNow.AddDays(10));
        token.IsActive(FixedNow).Should().BeTrue();
    }

    [Fact]
    public void PlatformApiToken_IsActive_Returns_False_After_Revocation()
    {
        var token = MakeToken(revoke: true);
        token.IsActive(FixedNow).Should().BeFalse();
    }

    [Fact]
    public void AgentQueryRecord_Create_Sets_All_Properties()
    {
        var tenantId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var record = AgentQueryRecord.Create(tenantId, tokenId, "GetServices", 200, 55, FixedNow, "{}", null);

        record.TenantId.Should().Be(tenantId);
        record.TokenId.Should().Be(tokenId);
        record.QueryType.Should().Be("GetServices");
        record.ResponseCode.Should().Be(200);
        record.DurationMs.Should().Be(55);
        record.ExecutedAt.Should().Be(FixedNow);
        record.QueryParametersJson.Should().Be("{}");
        record.ErrorMessage.Should().BeNull();
    }
}

