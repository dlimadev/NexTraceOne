using FluentValidation;

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
    public sealed record Command(Guid PolicyId) : ICommand;

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
        IChangeIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var policy = await policyRepository.GetByIdAsync(
                ReleaseApprovalPolicyId.From(request.PolicyId), cancellationToken);

            if (policy is null)
                return Result.NotFound("Approval policy not found.");

            policy.Deactivate(currentUser.UserId ?? "system", clock.UtcNow);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Ok();
        }
    }
}
