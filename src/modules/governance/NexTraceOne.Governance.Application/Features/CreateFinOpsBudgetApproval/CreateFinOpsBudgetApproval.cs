using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreateFinOpsBudgetApproval;

/// <summary>
/// Feature: CreateFinOpsBudgetApproval — cria um pedido de aprovação de override de orçamento FinOps.
/// Usado quando uma release excede o orçamento e o gate está em modo RequireApproval.
/// O aprovador designado pode depois aceitar ou rejeitar via ResolveFinOpsBudgetApproval.
/// Pilar: FinOps contextual — governança de promoções com controlo de custo.
/// </summary>
public static class CreateFinOpsBudgetApproval
{
    /// <summary>Command para criar um pedido de aprovação de orçamento.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal ActualCost,
        decimal BaselineCost,
        decimal CostDeltaPct,
        string Currency,
        string RequestedBy,
        string? Justification) : ICommand<Response>;

    /// <summary>Validação do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ActualCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BaselineCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Justification).MaximumLength(4000).When(x => x.Justification is not null);
        }
    }

    /// <summary>Handler que persiste o pedido de aprovação de orçamento.</summary>
    public sealed class Handler(
        IFinOpsBudgetApprovalRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var approval = FinOpsBudgetApproval.Create(
                releaseId: request.ReleaseId,
                serviceName: request.ServiceName,
                environment: request.Environment,
                actualCost: request.ActualCost,
                baselineCost: request.BaselineCost,
                costDeltaPct: request.CostDeltaPct,
                currency: request.Currency,
                requestedBy: request.RequestedBy,
                justification: request.Justification,
                now: clock.UtcNow);

            await repository.AddAsync(approval, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ApprovalId: approval.Id.Value,
                ReleaseId: approval.ReleaseId,
                ServiceName: approval.ServiceName,
                Environment: approval.Environment,
                Status: approval.Status.ToString(),
                RequestedAt: approval.RequestedAt));
        }
    }

    /// <summary>Resposta da criação do pedido de aprovação.</summary>
    public sealed record Response(
        Guid ApprovalId,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string Status,
        DateTimeOffset RequestedAt);
}
