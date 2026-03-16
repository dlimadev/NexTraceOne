namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Nível de maturidade de governança de uma equipa ou domínio.
/// Baseado no modelo CMMI adaptado ao contexto de governança operacional.
/// </summary>
public enum GovernanceMaturity
{
    /// <summary>Nível inicial — processos ad hoc, sem governança formal.</summary>
    Initial = 0,

    /// <summary>Em desenvolvimento — primeiras práticas de governança estabelecidas.</summary>
    Developing = 1,

    /// <summary>Definido — processos documentados e consistentes.</summary>
    Defined = 2,

    /// <summary>Gerido — governança medida e controlada com métricas.</summary>
    Managed = 3,

    /// <summary>Otimizado — melhoria contínua e governança proativa.</summary>
    Optimizing = 4
}
