using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.DecideJitAccess;

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
        ICurrentUser currentUser,
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

            if (decidedBy == jitRequest.RequestedBy)
                return IdentityErrors.JitSelfApprovalNotAllowed();

            if (request.Approve)
            {
                jitRequest.Approve(decidedBy, dateTimeProvider.UtcNow);
            }
            else
            {
                jitRequest.Reject(decidedBy, request.RejectionReason!, dateTimeProvider.UtcNow);
            }

            return Unit.Value;
        }
    }
}
