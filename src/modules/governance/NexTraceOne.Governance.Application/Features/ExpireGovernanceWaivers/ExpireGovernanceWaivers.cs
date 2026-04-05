using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ExpireGovernanceWaivers;

/// <summary>
/// Feature: ExpireGovernanceWaivers — identifica e revoga waivers aprovados cujo prazo
/// de expiração já foi ultrapassado. Suporta audit trail completo da revogação automática.
/// Deve ser executado periodicamente (ex: job Quartz diário).
/// </summary>
public static class ExpireGovernanceWaivers
{
    public sealed record Command(
        /// <summary>Identificador do revisor automático (ex: "system:waiver-expiry-job").</summary>
        string ReviewedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReviewedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGovernanceWaiverRepository waiverRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var approvedWaivers = await waiverRepository.ListAsync(
                packId: null,
                status: WaiverStatus.Approved,
                cancellationToken);

            var expiredWaivers = approvedWaivers
                .Where(w => w.ExpiresAt.HasValue && w.ExpiresAt.Value <= now)
                .ToList();

            foreach (var waiver in expiredWaivers)
            {
                waiver.Revoke(request.ReviewedBy);
                await waiverRepository.UpdateAsync(waiver, cancellationToken);
            }

            if (expiredWaivers.Count > 0)
                await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                expiredWaivers.Count,
                expiredWaivers.Select(w => w.Id.Value).ToList(),
                now));
        }
    }

    public sealed record Response(
        int ExpiredCount,
        IReadOnlyList<Guid> ExpiredWaiverIds,
        DateTimeOffset ProcessedAt);
}
