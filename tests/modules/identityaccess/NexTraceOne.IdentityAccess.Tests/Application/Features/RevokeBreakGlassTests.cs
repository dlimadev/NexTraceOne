using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.RevokeBreakGlass;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler RevokeBreakGlass.
/// Cobre: utilizador não autenticado, request não encontrado, acesso não activo,
/// revogação bem-sucedida, registo de evento de segurança BreakGlassRevoked.
/// </summary>
public sealed class RevokeBreakGlassTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CurrentUserId = Guid.NewGuid();

    private static (
        IBreakGlassRepository breakGlassRepo,
        ISecurityEventRepository evtRepo,
        ISecurityEventTracker evtTracker,
        ICurrentUser currentUser,
        RevokeBreakGlass.Handler handler) CreateHandler(
            string? authenticatedUserId = null)
    {
        var breakGlassRepo = Substitute.For<IBreakGlassRepository>();
        var evtRepo = Substitute.For<ISecurityEventRepository>();
        var evtTracker = Substitute.For<ISecurityEventTracker>();
        var currentTenant = new TestCurrentTenant(TenantId);
        var clock = new TestDateTimeProvider(FixedNow);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(authenticatedUserId ?? CurrentUserId.ToString());
        currentUser.IsAuthenticated.Returns(authenticatedUserId != null || true);

        var handler = new RevokeBreakGlass.Handler(
            breakGlassRepo, evtRepo, evtTracker, currentUser, currentTenant, clock);

        return (breakGlassRepo, evtRepo, evtTracker, currentUser, handler);
    }

    private static BreakGlassRequest CreateActiveBreakGlassRequest(UserId? requestedBy = null)
    {
        var userId = requestedBy ?? Domain.Entities.UserId.From(Guid.NewGuid());
        return BreakGlassRequest.Create(
            requestedBy: userId,
            tenantId: Domain.Entities.TenantId.From(TenantId),
            justification: "Production incident",
            ipAddress: "10.0.0.1",
            userAgent: "TestAgent",
            now: FixedNow.AddMinutes(-10));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenCurrentUserNotAuthenticated()
    {
        var (_, _, _, currentUser, handler) = CreateHandler(authenticatedUserId: "");
        currentUser.Id.Returns(string.Empty);

        var (bgRepo, _, _, _, h) = CreateHandler();
        // Override with empty ID
        var emptyUser = Substitute.For<ICurrentUser>();
        emptyUser.Id.Returns(string.Empty);
        var evtRepo2 = Substitute.For<ISecurityEventRepository>();
        var evtTracker2 = Substitute.For<ISecurityEventTracker>();
        var unauthHandler = new RevokeBreakGlass.Handler(
            bgRepo, evtRepo2, evtTracker2, emptyUser,
            new TestCurrentTenant(TenantId),
            new TestDateTimeProvider(FixedNow));

        var result = await unauthHandler.Handle(
            new RevokeBreakGlass.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBreakGlassRequestDoesNotExist()
    {
        var (breakGlassRepo, _, _, _, handler) = CreateHandler();
        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns((BreakGlassRequest?)null);

        var result = await handler.Handle(
            new RevokeBreakGlass.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("BreakGlass");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenBreakGlassIsNotActive()
    {
        var (breakGlassRepo, _, _, _, handler) = CreateHandler();

        // Create expired request (window of 1 minute, now is 10 min later)
        var request = BreakGlassRequest.Create(
            requestedBy: Domain.Entities.UserId.From(Guid.NewGuid()),
            tenantId: Domain.Entities.TenantId.From(TenantId),
            justification: "incident",
            ipAddress: "127.0.0.1",
            userAgent: "agent",
            now: FixedNow.AddMinutes(-10),
            accessWindow: TimeSpan.FromMinutes(1)); // already expired

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);

        var result = await handler.Handle(
            new RevokeBreakGlass.Command(request.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Contain("BreakGlass");
    }

    [Fact]
    public async Task Handle_Should_RevokeBreakGlass_WhenActive()
    {
        var (breakGlassRepo, _, _, _, handler) = CreateHandler();
        var request = CreateActiveBreakGlassRequest();
        request.IsActiveAt(FixedNow).Should().BeTrue("precondition: request must be active");

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);

        var result = await handler.Handle(
            new RevokeBreakGlass.Command(request.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.IsActiveAt(FixedNow).Should().BeFalse("request must be revoked");
        request.RevokedAt.Should().NotBeNull();
        request.Status.Should().Be(BreakGlassStatus.Revoked);
    }

    [Fact]
    public async Task Handle_Should_RecordSecurityEvent_WithBreakGlassRevokedType()
    {
        var (breakGlassRepo, evtRepo, evtTracker, _, handler) = CreateHandler();
        var request = CreateActiveBreakGlassRequest();

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);

        await handler.Handle(
            new RevokeBreakGlass.Command(request.Id.Value),
            CancellationToken.None);

        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.BreakGlassRevoked));
        evtTracker.Received(1).Track(Arg.Any<SecurityEvent>());
    }

    [Fact]
    public async Task Handle_Should_SetRevokedBy_ToCurrentUser()
    {
        var (breakGlassRepo, _, _, _, handler) = CreateHandler();
        var request = CreateActiveBreakGlassRequest();

        breakGlassRepo.GetByIdAsync(Arg.Any<BreakGlassRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);

        await handler.Handle(
            new RevokeBreakGlass.Command(request.Id.Value),
            CancellationToken.None);

        request.RevokedBy.Should().NotBeNull();
        request.RevokedBy!.Value.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task Validator_Should_RejectEmptyRequestId()
    {
        var validator = new RevokeBreakGlass.Validator();

        var result = validator.Validate(new RevokeBreakGlass.Command(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RevokeBreakGlass.Command.RequestId));
    }
}
