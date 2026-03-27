using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using DecideJitFeature = NexTraceOne.IdentityAccess.Application.Features.DecideJitAccess.DecideJitAccess;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature DecideJitAccess.
/// Cobre self-approval prevention, aprovação legítima e rejeição.
/// </summary>
public sealed class DecideJitAccessTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static JitAccessRequest CreatePendingRequest(UserId requestedBy, TenantId tenantId)
        => JitAccessRequest.Create(
            requestedBy,
            tenantId,
            "promotion:promote",
            "Release 'API Pagamentos v2.3' → Production",
            "Hotfix deployment required urgently.",
            Now);

    [Fact]
    public async Task Handle_Should_ReturnForbidden_When_SelfApprovalAttempted()
    {
        var requesterId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var jitRequest = CreatePendingRequest(UserId.From(requesterId), TenantId.From(tenantId));

        var jitRepo = Substitute.For<IJitAccessRepository>();
        var securityEventRepo = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        jitRepo.GetByIdAsync(Arg.Any<JitAccessRequestId>(), Arg.Any<CancellationToken>())
            .Returns(jitRequest);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(requesterId.ToString());

        var sut = new DecideJitFeature.Handler(
            jitRepo,
            securityEventRepo,
            securityEventTracker,
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new DecideJitFeature.Command(jitRequest.Id.Value, Approve: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.JitAccess.SelfApprovalNotAllowed");
        securityEventRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitSelfApprovalDenied));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitSelfApprovalDenied));
    }

    [Fact]
    public async Task Handle_Should_ApproveRequest_When_DifferentUserDecides()
    {
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var jitRequest = CreatePendingRequest(UserId.From(requesterId), TenantId.From(tenantId));

        var jitRepo = Substitute.For<IJitAccessRepository>();
        var securityEventRepo = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        jitRepo.GetByIdAsync(Arg.Any<JitAccessRequestId>(), Arg.Any<CancellationToken>())
            .Returns(jitRequest);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(approverId.ToString());

        var sut = new DecideJitFeature.Handler(
            jitRepo,
            securityEventRepo,
            securityEventTracker,
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new DecideJitFeature.Command(jitRequest.Id.Value, Approve: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jitRequest.Status.Should().Be(JitAccessStatus.Approved);
        securityEventRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitAccessApproved));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitAccessApproved));
    }

    [Fact]
    public async Task Handle_Should_RejectRequest_When_DecidedWithReason()
    {
        var requesterId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var jitRequest = CreatePendingRequest(UserId.From(requesterId), TenantId.From(tenantId));

        var jitRepo = Substitute.For<IJitAccessRepository>();
        var securityEventRepo = Substitute.For<ISecurityEventRepository>();
        var securityEventTracker = Substitute.For<ISecurityEventTracker>();

        jitRepo.GetByIdAsync(Arg.Any<JitAccessRequestId>(), Arg.Any<CancellationToken>())
            .Returns(jitRequest);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(reviewerId.ToString());

        var sut = new DecideJitFeature.Handler(
            jitRepo,
            securityEventRepo,
            securityEventTracker,
            currentUser,
            new TestCurrentTenant(tenantId),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new DecideJitFeature.Command(jitRequest.Id.Value, Approve: false, RejectionReason: "Access scope too broad."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jitRequest.Status.Should().Be(JitAccessStatus.Rejected);
        securityEventRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitAccessRejected));
        securityEventTracker.Received(1).Track(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.JitAccessRejected));
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_RequestNotFound()
    {
        var reviewerId = Guid.NewGuid();
        var jitRepo = Substitute.For<IJitAccessRepository>();

        jitRepo.GetByIdAsync(Arg.Any<JitAccessRequestId>(), Arg.Any<CancellationToken>())
            .Returns((JitAccessRequest?)null);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(reviewerId.ToString());

        var sut = new DecideJitFeature.Handler(
            jitRepo,
            Substitute.For<ISecurityEventRepository>(),
            Substitute.For<ISecurityEventTracker>(),
            currentUser,
            new TestCurrentTenant(Guid.NewGuid()),
            new TestDateTimeProvider(Now));

        var result = await sut.Handle(
            new DecideJitFeature.Command(Guid.NewGuid(), Approve: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.JitAccess.NotFound");
    }
}
