using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

public sealed record FinOpsBudgetApprovalId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade de domínio: pedido de aprovação de override de orçamento FinOps.
/// Criado quando uma release excede o orçamento configurado e o gate está em modo RequireApproval.
/// Aprovado ou rejeitado por um utilizador designado como aprovador FinOps.
/// </summary>
public sealed class FinOpsBudgetApproval : Entity<FinOpsBudgetApprovalId>
{
    public Guid ReleaseId { get; private init; }
    public string ServiceName { get; private init; } = string.Empty;
    public string Environment { get; private init; } = string.Empty;
    public decimal ActualCost { get; private init; }
    public decimal BaselineCost { get; private init; }
    public decimal CostDeltaPct { get; private init; }
    public string Currency { get; private init; } = "USD";
    public string RequestedBy { get; private init; } = string.Empty;
    public string? Justification { get; private init; }
    public FinOpsBudgetApprovalStatus Status { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset RequestedAt { get; private init; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public uint RowVersion { get; set; }

    private FinOpsBudgetApproval() { }

    /// <summary>Cria um novo pedido de aprovação de override de orçamento.</summary>
    public static FinOpsBudgetApproval Create(
        Guid releaseId,
        string serviceName,
        string environment,
        decimal actualCost,
        decimal baselineCost,
        decimal costDeltaPct,
        string currency,
        string requestedBy,
        string? justification,
        DateTimeOffset now)
    {
        Guard.Against.Default(releaseId, nameof(releaseId));
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.StringTooLong(serviceName, 200, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(environment, nameof(environment));
        Guard.Against.StringTooLong(environment, 100, nameof(environment));
        Guard.Against.Negative(actualCost, nameof(actualCost));
        Guard.Against.Negative(baselineCost, nameof(baselineCost));
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));
        Guard.Against.OutOfRange(currency.Length, nameof(currency), 3, 3);
        Guard.Against.NullOrWhiteSpace(requestedBy, nameof(requestedBy));
        Guard.Against.StringTooLong(requestedBy, 500, nameof(requestedBy));

        if (justification is not null)
            Guard.Against.StringTooLong(justification, 4000, nameof(justification));

        return new FinOpsBudgetApproval
        {
            Id = new FinOpsBudgetApprovalId(Guid.NewGuid()),
            ReleaseId = releaseId,
            ServiceName = serviceName,
            Environment = environment,
            ActualCost = actualCost,
            BaselineCost = baselineCost,
            CostDeltaPct = costDeltaPct,
            Currency = currency.ToUpperInvariant(),
            RequestedBy = requestedBy,
            Justification = justification,
            Status = FinOpsBudgetApprovalStatus.Pending,
            RequestedAt = now,
        };
    }

    /// <summary>Aprova o override de orçamento.</summary>
    public void Approve(string approvedBy, string? comment, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(approvedBy, nameof(approvedBy));
        Status = FinOpsBudgetApprovalStatus.Approved;
        ResolvedBy = approvedBy;
        Comment = comment;
        ResolvedAt = now;
    }

    /// <summary>Rejeita o override de orçamento.</summary>
    public void Reject(string rejectedBy, string? comment, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(rejectedBy, nameof(rejectedBy));
        Status = FinOpsBudgetApprovalStatus.Rejected;
        ResolvedBy = rejectedBy;
        Comment = comment;
        ResolvedAt = now;
    }
}
