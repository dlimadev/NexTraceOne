namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Período de contabilização do budget/quota de tokens de IA.
/// Define a janela de tempo para acumulação e reset dos contadores.
/// </summary>
public enum BudgetPeriod
{
    /// <summary>Reset diário do budget.</summary>
    Daily,

    /// <summary>Reset semanal do budget.</summary>
    Weekly,

    /// <summary>Reset mensal do budget.</summary>
    Monthly
}
