namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>Estado de um pedido de aprovação de override de orçamento FinOps.</summary>
public enum FinOpsBudgetApprovalStatus
{
    /// <summary>Aguarda decisão de um aprovador designado.</summary>
    Pending = 0,
    /// <summary>Override aprovado — promoção pode prosseguir.</summary>
    Approved = 1,
    /// <summary>Override rejeitado — promoção bloqueada.</summary>
    Rejected = 2,
}
