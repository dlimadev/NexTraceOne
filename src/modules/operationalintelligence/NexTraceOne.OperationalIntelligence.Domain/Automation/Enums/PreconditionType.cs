namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Tipos de pré-condição verificáveis antes da execução de uma automação.
/// Permite definir critérios obrigatórios para garantir segurança operacional.
/// </summary>
public enum PreconditionType
{
    /// <summary>Verificação de saúde do serviço alvo.</summary>
    ServiceHealthCheck = 0,

    /// <summary>Limiar mínimo de severidade do incidente associado.</summary>
    IncidentSeverityThreshold = 1,

    /// <summary>Restrição de ambiente onde a automação pode ser executada.</summary>
    EnvironmentRestriction = 2,

    /// <summary>Presença de aprovação obrigatória antes da execução.</summary>
    ApprovalPresence = 3,

    /// <summary>Confirmação explícita do owner do serviço.</summary>
    OwnerConfirmation = 4,

    /// <summary>Restrição de raio de impacto máximo permitido.</summary>
    BlastRadiusConstraint = 5,

    /// <summary>Consciência de risco contratual associado à automação.</summary>
    ContractRiskAwareness = 6,

    /// <summary>Verificação do estado das dependências do serviço.</summary>
    DependencyStateCheck = 7,

    /// <summary>Período de arrefecimento obrigatório entre execuções.</summary>
    CooldownPeriod = 8
}
