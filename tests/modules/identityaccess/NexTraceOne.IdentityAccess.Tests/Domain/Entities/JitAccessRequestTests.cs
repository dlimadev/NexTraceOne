using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate JitAccessRequest.
/// Cobre criação, aprovação, rejeição, expiração, revogação e proteção contra auto-aprovação.
/// </summary>
public sealed class JitAccessRequestTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_StartAsPending_When_Requested()
    {
        var request = JitAccessRequest.Create(
            UserId.New(), TenantId.New(),
            "promotion:promote",
            "Release 'API Pagamentos v2.3' → Production",
            "Urgent deployment of hotfix for payment processing.",
            Now);

        request.Status.Should().Be(JitAccessStatus.Pending);
        request.ApprovalDeadline.Should().Be(Now.Add(JitAccessRequest.DefaultApprovalTimeout));
    }

    [Fact]
    public void Approve_Should_GrantTemporaryAccess_When_ApprovedByDifferentUser()
    {
        var requester = UserId.New();
        var approver = UserId.New();

        var request = JitAccessRequest.Create(
            requester, TenantId.New(),
            "workflow:approve",
            "Workflow 'Release v3.0 Approval'",
            "Need to approve pending release for client deadline.",
            Now);

        request.Approve(approver, Now.AddMinutes(10));

        request.Status.Should().Be(JitAccessStatus.Approved);
        request.DecidedBy.Should().Be(approver);
        request.GrantedFrom.Should().Be(Now.AddMinutes(10));
        request.GrantedUntil.Should().Be(Now.AddMinutes(10).Add(JitAccessRequest.DefaultAccessDuration));
        request.IsAccessActiveAt(Now.AddMinutes(15)).Should().BeTrue();
    }

    [Fact]
    public void Approve_Should_BeIgnored_When_SelfApproval()
    {
        var requester = UserId.New();

        var request = JitAccessRequest.Create(
            requester, TenantId.New(),
            "promotion:promote", "Scope", "Justification.", Now);

        request.Approve(requester, Now.AddMinutes(5));

        request.Status.Should().Be(JitAccessStatus.Pending);
        request.DecidedBy.Should().BeNull();
    }

    [Fact]
    public void Reject_Should_ChangeStatusToRejected_When_ReasonProvided()
    {
        var request = JitAccessRequest.Create(
            UserId.New(), TenantId.New(),
            "promotion:promote", "Scope", "Justification.", Now);

        var rejector = UserId.New();
        request.Reject(rejector, "Access not justified for this scope.", Now.AddMinutes(5));

        request.Status.Should().Be(JitAccessStatus.Rejected);
        request.RejectionReason.Should().Be("Access not justified for this scope.");
    }

    [Fact]
    public void Expire_Should_ExpirePendingRequest_When_DeadlinePasses()
    {
        var request = JitAccessRequest.Create(
            UserId.New(), TenantId.New(),
            "promotion:promote", "Scope", "Justification.", Now);

        request.Expire(Now.Add(JitAccessRequest.DefaultApprovalTimeout).AddMinutes(1));

        request.Status.Should().Be(JitAccessStatus.Expired);
    }

    [Fact]
    public void Revoke_Should_EndAccessEarly_When_AlreadyApproved()
    {
        var requester = UserId.New();
        var approver = UserId.New();
        var revoker = UserId.New();

        var request = JitAccessRequest.Create(
            requester, TenantId.New(),
            "workflow:approve", "Scope", "Justification.", Now);

        request.Approve(approver, Now.AddMinutes(5));
        request.Revoke(revoker, Now.AddHours(1));

        request.Status.Should().Be(JitAccessStatus.Revoked);
        request.RevokedBy.Should().Be(revoker);
        request.IsAccessActiveAt(Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void IsAccessActiveAt_Should_ReturnFalse_When_GrantHasExpired()
    {
        var requester = UserId.New();
        var approver = UserId.New();

        var request = JitAccessRequest.Create(
            requester, TenantId.New(),
            "promotion:promote", "Scope", "Justification.", Now);

        request.Approve(approver, Now.AddMinutes(5));

        var afterGrant = Now.AddMinutes(5).Add(JitAccessRequest.DefaultAccessDuration).AddMinutes(1);
        request.IsAccessActiveAt(afterGrant).Should().BeFalse();
    }
}

