namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Tipos de ações de mitigação disponíveis num workflow operacional.
/// Permite categorizar e orquestrar as ações corretivas aplicáveis a um incidente.
/// </summary>
public enum MitigationActionType
{
    /// <summary>Investigar a causa raiz do incidente.</summary>
    Investigate = 0,

    /// <summary>Validar a alteração associada ao incidente.</summary>
    ValidateChange = 1,

    /// <summary>Candidato a rollback — reverter alteração suspeita.</summary>
    RollbackCandidate = 2,

    /// <summary>Reinício controlado do serviço afetado.</summary>
    RestartControlled = 3,

    /// <summary>Reprocessar eventos ou operações falhadas.</summary>
    Reprocess = 4,

    /// <summary>Verificar estado e disponibilidade de dependências.</summary>
    VerifyDependency = 5,

    /// <summary>Escalar o incidente para uma equipa ou nível superior.</summary>
    Escalate = 6,

    /// <summary>Executar um runbook operacional predefinido.</summary>
    ExecuteRunbook = 7,

    /// <summary>Observar métricas e validar estabilidade pós-ação.</summary>
    ObserveAndValidate = 8,

    /// <summary>Rever o impacto nos contratos de serviço afetados.</summary>
    ContractImpactReview = 9
}
