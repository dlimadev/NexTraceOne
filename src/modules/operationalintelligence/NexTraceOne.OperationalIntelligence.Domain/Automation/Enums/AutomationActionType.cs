namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Tipos de ação disponíveis no catálogo de automação operacional.
/// Permite categorizar e orquestrar as ações automatizadas aplicáveis a serviços e operações.
/// </summary>
public enum AutomationActionType
{
    /// <summary>Reinício controlado do serviço afetado.</summary>
    RestartControlled = 0,

    /// <summary>Reprocessamento controlado de eventos ou operações falhadas.</summary>
    ReprocessControlled = 1,

    /// <summary>Executar um passo de runbook operacional predefinido.</summary>
    ExecuteRunbookStep = 2,

    /// <summary>Revisão de prontidão para rollback da alteração.</summary>
    RollbackReadinessReview = 3,

    /// <summary>Observar métricas e validar estabilidade pós-ação.</summary>
    ObserveAndValidate = 4,

    /// <summary>Escalar com contexto operacional para equipa ou nível superior.</summary>
    EscalateWithContext = 5,

    /// <summary>Verificar estado e disponibilidade de dependências do serviço.</summary>
    VerifyDependencyState = 6,

    /// <summary>Validar estado pós-alteração do serviço em produção.</summary>
    ValidatePostChangeState = 7
}
