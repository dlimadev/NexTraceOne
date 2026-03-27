using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.DecideJitAccess;

/// <summary>
/// Feature: DecideJitAccess — aprova ou rejeita uma solicitação JIT.
///
/// Regras:
/// - Auto-aprovação não é permitida.
/// - Solicitação deve estar em estado Pending.
/// - Rejeição exige motivo obrigatório.
/// </summary>
public static class DecideJitAccess
{
    /// <summary>Comando para decisão sobre solicitação JIT.</summary>
    public sealed record Command(
        Guid RequestId,
        bool Approve,
        string? RejectionReason = null) : ICommand;

    /// <summary>Valida a entrada da decisão JIT.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            When(x => !x.Approve, () =>
            {
                RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(1000);
            });
        }
    }

    /// <summary>Handler que processa a decisão sobre solicitação JIT.</summary>
    public sealed class Handler(
        IJitAccessRepository jitAccessRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var jitRequest = await jitAccessRepository.GetByIdAsync(
                JitAccessRequestId.From(request.RequestId), cancellationToken);

            if (jitRequest is null)
                return IdentityErrors.JitAccessNotFound(request.RequestId);

            if (jitRequest.Status != JitAccessStatus.Pending)
                return IdentityErrors.JitAccessNotPending(request.RequestId);

            var decidedBy = UserId.From(Guid.Parse(currentUser.Id));
            var now = dateTimeProvider.UtcNow;
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : jitRequest.TenantId;

            if (decidedBy == jitRequest.RequestedBy)
            {
                var deniedEvent = SecurityEvent.Create(
                    tenantId,
                    decidedBy,
                    sessionId: null,
                    SecurityEventType.JitSelfApprovalDenied,
                    $"Self-approval denied for JIT access request '{jitRequest.Id.Value}'.",
                    riskScore: 65,
                    ipAddress: null,
                    userAgent: null,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        jitAccessRequestId = jitRequest.Id.Value,
                        requestedBy = jitRequest.RequestedBy.Value,
                        attemptedBy = decidedBy.Value
                    }),
                    now);

                securityEventRepository.Add(deniedEvent);
                securityEventTracker.Track(deniedEvent);
                return IdentityErrors.JitSelfApprovalNotAllowed();
            }

            if (request.Approve)
            {
                jitRequest.Approve(decidedBy, now);

                var approvedEvent = SecurityEvent.Create(
                    tenantId,
                    jitRequest.RequestedBy,
                    sessionId: null,
                    SecurityEventType.JitAccessApproved,
                    $"JIT access request '{jitRequest.Id.Value}' approved by '{decidedBy.Value}'.",
                    riskScore: 45,
                    ipAddress: null,
                    userAgent: null,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        jitAccessRequestId = jitRequest.Id.Value,
                        requestedBy = jitRequest.RequestedBy.Value,
                        approvedBy = decidedBy.Value,
                        permissionCode = jitRequest.PermissionCode,
                        scope = jitRequest.Scope,
                        grantedFrom = jitRequest.GrantedFrom,
                        grantedUntil = jitRequest.GrantedUntil
                    }),
                    now);

                securityEventRepository.Add(approvedEvent);
                securityEventTracker.Track(approvedEvent);
            }
            else
            {
                jitRequest.Reject(decidedBy, request.RejectionReason!, now);

                var rejectedEvent = SecurityEvent.Create(
                    tenantId,
                    jitRequest.RequestedBy,
                    sessionId: null,
                    SecurityEventType.JitAccessRejected,
                    $"JIT access request '{jitRequest.Id.Value}' rejected by '{decidedBy.Value}'.",
                    riskScore: 20,
                    ipAddress: null,
                    userAgent: null,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        jitAccessRequestId = jitRequest.Id.Value,
                        requestedBy = jitRequest.RequestedBy.Value,
                        rejectedBy = decidedBy.Value,
                        reason = request.RejectionReason
                    }),
                    now);

                securityEventRepository.Add(rejectedEvent);
                securityEventTracker.Track(rejectedEvent);
            }

            return Unit.Value;
        }
    }
}
