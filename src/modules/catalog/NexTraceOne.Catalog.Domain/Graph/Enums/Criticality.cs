namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Nível de criticidade do serviço para o negócio.
/// Usado para priorização de incidentes, mudanças e governança.
/// </summary>
public enum Criticality
{
    /// <summary>Serviço de baixa criticidade — impacto mínimo no negócio.</summary>
    Low = 0,

    /// <summary>Serviço de criticidade média — impacto moderado.</summary>
    Medium = 1,

    /// <summary>Serviço de alta criticidade — impacto significativo no negócio.</summary>
    High = 2,

    /// <summary>Serviço crítico — essencial para operações do negócio.</summary>
    Critical = 3
}
