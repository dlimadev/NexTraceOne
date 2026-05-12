using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.ForgotPassword;
using NexTraceOne.IdentityAccess.Application.Features.ListBreakGlassRequests;
using NexTraceOne.IdentityAccess.Application.Features.ResetPassword;
using NexTraceOne.IdentityAccess.Application.Features.RevokeBreakGlass;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para fluxos de autenticação e acesso emergencial:
/// ForgotPassword, ResetPassword, ListBreakGlassRequests, RevokeBreakGlass.
/// </summary>
public sealed class IdentityAccessFlowFeaturesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock() => new TestDateTimeProvider(FixedNow);

    // ── ForgotPassword ──────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_UserExists_AlwaysReturnsAccepted()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var user = TestUserFactory.CreateRegularUser();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = new ForgotPassword.Handler(
            userRepo,
            Substitute.For<IPasswordResetTokenRepository>(),
            Substitute.For<IIdentityNotifier>(),
            Substitute.For<IIdentityAccessUnitOfWork>(),
            CreateClock());
        var result = await handler.Handle(
            new ForgotPassword.Command("user@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_UserDoesNotExist_AlwaysReturnsAccepted()
    {
        // Anti-enumeration: even if user doesn't exist, return success
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = new ForgotPassword.Handler(
            userRepo,
            Substitute.For<IPasswordResetTokenRepository>(),
            Substitute.For<IIdentityNotifier>(),
            Substitute.For<IIdentityAccessUnitOfWork>(),
            CreateClock());
        var result = await handler.Handle(
            new ForgotPassword.Command("unknown@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Accepted.Should().BeTrue("anti-enumeration: always accept");
    }

    // ── ResetPassword ───────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_InfrastructureNotReady_ReturnsValidationError()
    {
        var tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
        tokenRepo.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var handler = new ResetPassword.Handler(
            tokenRepo,
            Substitute.For<IUserRepository>(),
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IIdentityAccessUnitOfWork>(),
            CreateClock());
        var result = await handler.Handle(
            new ResetPassword.Command("some-reset-token", "NewPassword123!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("password.reset");
    }

    // ── ListBreakGlassRequests ──────────────────────────────────────────────

    [Fact]
    public async Task ListBreakGlassRequests_NoActiveRequests_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        breakGlassRepo.ListActiveByTenantAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest>().AsReadOnly());
        breakGlassRepo.ListPendingPostMortemAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest>().AsReadOnly());

        var handler = new ListBreakGlassRequests.Handler(breakGlassRepo, currentTenant);
        var result = await handler.Handle(
            new ListBreakGlassRequests.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBreakGlassRequests_WithActiveRequests_ReturnsActiveItems()
    {
        var tenantId = Guid.NewGuid();
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.From(tenantId),
            "Emergency access for production incident P1-12345 affecting all users.",
            "192.168.1.1", "TestAgent/1.0", FixedNow);

        breakGlassRepo.ListActiveByTenantAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest> { request }.AsReadOnly());
        breakGlassRepo.ListPendingPostMortemAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest>().AsReadOnly());

        var handler = new ListBreakGlassRequests.Handler(breakGlassRepo, currentTenant);
        var result = await handler.Handle(
            new ListBreakGlassRequests.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].RequestedBy.Should().Be(request.RequestedBy.Value);
        result.Value[0].Justification.Should().Be(request.Justification);
    }

    [Fact]
    public async Task ListBreakGlassRequests_IncludeInactive_ReturnsBothActiveAndPendingPostMortem()
    {
        var tenantId = Guid.NewGuid();
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var userId = UserId.New();
        var tId = TenantId.From(tenantId);
        var activeRequest = BreakGlassRequest.Create(
            userId, tId,
            "Active emergency for incident P1-001 production systems down.",
            "10.0.0.1", "Agent/1.0", FixedNow);

        var postMortemRequest = BreakGlassRequest.Create(
            userId, tId,
            "Previous emergency for audit trail P0-999 critical data access needed.",
            "10.0.0.2", "Agent/1.0", FixedNow.AddDays(-1));

        breakGlassRepo.ListActiveByTenantAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest> { activeRequest }.AsReadOnly());
        breakGlassRepo.ListPendingPostMortemAsync(
            Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BreakGlassRequest> { postMortemRequest }.AsReadOnly());

        var handler = new ListBreakGlassRequests.Handler(breakGlassRepo, currentTenant);
        var result = await handler.Handle(
            new ListBreakGlassRequests.Query(IncludeInactive: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ── RevokeBreakGlass ────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeBreakGlass_NotFound_ReturnsFailure()
    {
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var secEventRepo = Substitute.For<ISecurityEventRepository>();
        var secEventTracker = Substitute.For<ISecurityEventTracker>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();

        currentUser.Id.Returns(Guid.NewGuid().ToString());
        currentTenant.Id.Returns(Guid.NewGuid());

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns((BreakGlassRequest?)null);

        var handler = new RevokeBreakGlass.Handler(
            breakGlassRepo, secEventRepo, secEventTracker, currentUser, currentTenant, CreateClock());
        var result = await handler.Handle(
            new RevokeBreakGlass.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("BreakGlass");
    }

    [Fact]
    public async Task RevokeBreakGlass_NotAuthenticated_ReturnsFailure()
    {
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var secEventRepo = Substitute.For<ISecurityEventRepository>();
        var secEventTracker = Substitute.For<ISecurityEventTracker>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();

        currentUser.Id.Returns((string?)null);
        currentTenant.Id.Returns(Guid.NewGuid());

        var handler = new RevokeBreakGlass.Handler(
            breakGlassRepo, secEventRepo, secEventTracker, currentUser, currentTenant, CreateClock());
        var result = await handler.Handle(
            new RevokeBreakGlass.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    [Fact]
    public async Task RevokeBreakGlass_ExpiredRequest_ReturnsNotActiveFailure()
    {
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var secEventRepo = Substitute.For<ISecurityEventRepository>();
        var secEventTracker = Substitute.For<ISecurityEventTracker>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();

        currentUser.Id.Returns(Guid.NewGuid().ToString());
        currentTenant.Id.Returns(Guid.NewGuid());

        // Create a request in the past (already expired)
        var breakGlass = BreakGlassRequest.Create(
            UserId.New(), TenantId.From(Guid.NewGuid()),
            "Old emergency for a past incident that happened three days ago.",
            "10.0.0.1", "Agent/1.0",
            FixedNow.AddHours(-5),
            accessWindow: TimeSpan.FromHours(2));

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns(breakGlass);

        var handler = new RevokeBreakGlass.Handler(
            breakGlassRepo, secEventRepo, secEventTracker, currentUser, currentTenant, CreateClock());
        var result = await handler.Handle(
            new RevokeBreakGlass.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

