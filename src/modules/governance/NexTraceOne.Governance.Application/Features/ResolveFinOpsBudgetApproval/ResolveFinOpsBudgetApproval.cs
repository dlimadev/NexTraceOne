using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.ResolveFinOpsBudgetApproval;

/// <summary>
/// Feature: ResolveFinOpsBudgetApproval — aprova ou rejeita um pedido de override de orçamento FinOps.
/// Apenas utilizadores na lista de aprovadores (finops.release.budget_gate.approvers) devem chamar este endpoint.
/// Pilar: FinOps contextual — workflow de aprovação com auditoria.
/// </summary>
public static class ResolveFinOpsBudgetApproval
{
    /// <summary>Command para resolver (aprovar ou rejeitar) um pedido de aprovação.</summary>
    public sealed record Command(
        Guid ApprovalId,
        bool Approved,
        string ResolvedBy,
        string? Comment) : ICommand<Response>;

    /// <summary>Validação do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApprovalId).NotEmpty();
            RuleFor(x => x.ResolvedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Comment).MaximumLength(4000).When(x => x.Comment is not null);
        }
    }

    /// <summary>Handler que resolve o pedido de aprovação de orçamento.</summary>
    public sealed class Handler(
        IFinOpsBudgetApprovalRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var approval = await repository.GetByIdAsync(request.ApprovalId, cancellationToken);
            if (approval is null)
                return GovernanceChangeCostErrors.ReleaseNotFound(request.ApprovalId.ToString());

            if (approval.Status != FinOpsBudgetApprovalStatus.Pending)
                return Error.Validation("finops.approval.already_resolved",
                    "This budget approval request has already been resolved.");

            if (request.Approved)
                approval.Approve(request.ResolvedBy, request.Comment, clock.UtcNow);
            else
                approval.Reject(request.ResolvedBy, request.Comment, clock.UtcNow);

            repository.Update(approval);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ApprovalId: approval.Id.Value,
                ReleaseId: approval.ReleaseId,
                Status: approval.Status.ToString(),
                ResolvedBy: approval.ResolvedBy,
                ResolvedAt: approval.ResolvedAt));
        }
    }

    /// <summary>Resposta da resolução de um pedido de aprovação.</summary>
    public sealed record Response(
        Guid ApprovalId,
        Guid ReleaseId,
        string Status,
        string? ResolvedBy,
        DateTimeOffset? ResolvedAt);
}
