namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Resultado final da execução de um workflow de automação.
/// Permite classificar o desfecho da operação automatizada para auditoria e análise.
/// </summary>
public enum AutomationOutcome
{
    /// <summary>Execução concluída com sucesso total.</summary>
    Successful = 0,

    /// <summary>Execução parcialmente bem-sucedida — alguns passos falharam.</summary>
    PartiallySuccessful = 1,

    /// <summary>Execução falhada — erro durante o processamento.</summary>
    Failed = 2,

    /// <summary>Resultado inconclusivo — não foi possível determinar o desfecho.</summary>
    Inconclusive = 3,

    /// <summary>Execução cancelada antes da conclusão.</summary>
    Cancelled = 4,

    /// <summary>Execução revertida — rollback aplicado com sucesso.</summary>
    RolledBack = 5
}
