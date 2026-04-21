namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

/// <summary>
/// Tipo de sinal de risco que contribui para o score de risco do serviço.
/// Permite identificar as causas raiz do risco calculado.
/// </summary>
public enum RiskSignalType
{
    /// <summary>Vulnerabilidade de severidade crítica presente e não mitigada.</summary>
    VulnerabilityCritical = 0,

    /// <summary>Taxa de falha de mudanças acima do threshold aceitável.</summary>
    HighChangeFailureRate = 1,

    /// <summary>Blast radius de falha de serviço excede limiar configurado.</summary>
    LargeBlastRadius = 2,

    /// <summary>Violação activa de política de governança (Policy Studio).</summary>
    PolicyViolation = 3,

    /// <summary>Serviço sem owner definido ou com ownership em drift.</summary>
    NoOwner = 4,

    /// <summary>Contrato desactualizado ou sem versão activa publicada.</summary>
    StaleContract = 5,

    /// <summary>Release promovida para produção sem aprovação ou evidência válida.</summary>
    UnreviewedRelease = 6
}
