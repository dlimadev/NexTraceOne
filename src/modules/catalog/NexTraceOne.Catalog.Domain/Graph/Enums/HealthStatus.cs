namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Status de saúde de um nó no grafo de engenharia.
/// Usado pelo overlay Health para codificação visual (cor, ícone)
/// e pelo cálculo de propagação de risco.
/// </summary>
public enum HealthStatus
{
    /// <summary>Nó saudável — operação normal, sem incidentes ou alertas.</summary>
    Healthy = 0,

    /// <summary>Nó degradado — alertas ativos ou performance abaixo do esperado.</summary>
    Degraded = 1,

    /// <summary>Nó com falha — incidentes ativos ou deploys falhando.</summary>
    Unhealthy = 2,

    /// <summary>Status desconhecido — dados insuficientes para determinar saúde.</summary>
    Unknown = 3
}
