using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.DeleteApprovalPolicy;

/// <summary>
/// Feature: DeleteApprovalPolicy — desactiva (soft-delete) uma política de aprovação de release.
///
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class DeleteApprovalPolicy
{
    /// <summary>Comando para desactivar uma política.</summary>
    public sealed record Command(Guid PolicyId) : ICommand<Unit>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
        }
    }

    /// <summary>Handler que desactiva a política via soft-delete.</summary>
    public sealed class Handler(
        IReleaseApprovalPolicyRepository policyRepository,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IChangeIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var policy = await policyRepository.GetByIdAsync(
                ReleaseApprovalPolicyId.From(request.PolicyId), cancellationToken);

            if (policy is null)
                return Error.NotFound(
                    "change_intelligence.approval_policy.not_found",
                    "Approval policy not found.");

            policy.Deactivate(currentUser.Id, clock.UtcNow);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
