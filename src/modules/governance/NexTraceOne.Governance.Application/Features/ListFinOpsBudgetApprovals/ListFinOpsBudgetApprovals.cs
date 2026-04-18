using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListFinOpsBudgetApprovals;

/// <summary>
/// Feature: ListFinOpsBudgetApprovals — lista os pedidos de aprovação de override de orçamento FinOps.
/// Suporta filtros por status (Pending, Approved, Rejected) e serviço.
/// Pilar: FinOps contextual — auditoria e visibilidade dos workflows de aprovação.
/// </summary>
public static class ListFinOpsBudgetApprovals
{
    /// <summary>Query para listar pedidos de aprovação de orçamento.</summary>
    public sealed record Query(
        string? Status,
        string? ServiceName,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Handler que retorna os pedidos de aprovação filtrados.</summary>
    public sealed class Handler(
        IFinOpsBudgetApprovalRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            FinOpsBudgetApprovalStatus? statusFilter = request.Status switch
            {
                "Pending" => FinOpsBudgetApprovalStatus.Pending,
                "Approved" => FinOpsBudgetApprovalStatus.Approved,
                "Rejected" => FinOpsBudgetApprovalStatus.Rejected,
                _ => null,
            };

            var items = await repository.ListAsync(statusFilter, request.ServiceName, cancellationToken);

            var dtos = items
                .Take(request.PageSize)
                .Select(a => new ApprovalDto(
                    ApprovalId: a.Id.Value,
                    ReleaseId: a.ReleaseId,
                    ServiceName: a.ServiceName,
                    Environment: a.Environment,
                    ActualCost: a.ActualCost,
                    BaselineCost: a.BaselineCost,
                    CostDeltaPct: a.CostDeltaPct,
                    Currency: a.Currency,
                    Status: a.Status.ToString(),
                    RequestedBy: a.RequestedBy,
                    Justification: a.Justification,
                    ResolvedBy: a.ResolvedBy,
                    Comment: a.Comment,
                    RequestedAt: a.RequestedAt,
                    ResolvedAt: a.ResolvedAt))
                .ToList();

            return Result<Response>.Success(new Response(dtos));
        }
    }

    /// <summary>DTO de um pedido de aprovação de orçamento.</summary>
    public sealed record ApprovalDto(
        Guid ApprovalId,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal ActualCost,
        decimal BaselineCost,
        decimal CostDeltaPct,
        string Currency,
        string Status,
        string RequestedBy,
        string? Justification,
        string? ResolvedBy,
        string? Comment,
        DateTimeOffset RequestedAt,
        DateTimeOffset? ResolvedAt);

    /// <summary>Resposta com a lista de pedidos de aprovação.</summary>
    public sealed record Response(IReadOnlyList<ApprovalDto> Items);
}
