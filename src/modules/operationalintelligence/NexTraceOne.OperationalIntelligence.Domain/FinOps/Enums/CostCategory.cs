namespace NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

/// <summary>
/// Categoria de custo operacional para alocação por serviço.
/// Utilizado em <see cref="NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities.ServiceCostAllocationRecord"/>.
/// Wave I.2 — FinOps Contextual por Serviço.
/// </summary>
public enum CostCategory
{
    /// <summary>Custo de computação (CPU, memória, instâncias).</summary>
    Compute = 0,

    /// <summary>Custo de armazenamento (disco, blobs, databases).</summary>
    Storage = 1,

    /// <summary>Custo de rede (egress, ingresss, load balancers).</summary>
    Network = 2,

    /// <summary>Custo de licenciamento de software ou plataforma.</summary>
    Licensing = 3,

    /// <summary>Custo de monitorização e observabilidade.</summary>
    Observability = 4,

    /// <summary>Outros custos operacionais não classificados.</summary>
    Other = 99,
}
