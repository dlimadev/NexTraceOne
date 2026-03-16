namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Nível de criticidade de um domínio de negócio.
/// Utilizado para priorização de incidentes, blast radius e decisões de governança.
/// </summary>
public enum DomainCriticality
{
    /// <summary>Domínio de baixa criticidade — impacto limitado em caso de falha.</summary>
    Low = 0,

    /// <summary>Domínio de criticidade média — impacto moderado em caso de falha.</summary>
    Medium = 1,

    /// <summary>Domínio de alta criticidade — impacto significativo em caso de falha.</summary>
    High = 2,

    /// <summary>Domínio crítico — falhas afetam diretamente receita ou operação core.</summary>
    Critical = 3
}
