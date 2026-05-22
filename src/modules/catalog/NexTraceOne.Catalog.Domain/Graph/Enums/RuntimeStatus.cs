namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Estado de runtime de um ativo deployado num ambiente específico.
/// Alimentado pelo <c>OtelCatalogBridgeJob</c> e pelo handler de deployment events.
/// </summary>
public enum RuntimeStatus
{
    /// <summary>Estado desconhecido — nenhum sinal recebido ainda.</summary>
    Unknown = 0,

    /// <summary>Deployment em curso — imagem a ser iniciada.</summary>
    Deploying = 1,

    /// <summary>Serviço a correr normalmente.</summary>
    Running = 2,

    /// <summary>Serviço degradado — a responder mas com erros elevados ou latência alta.</summary>
    Degraded = 3,

    /// <summary>Serviço indisponível — sem resposta ou a falhar consistentemente.</summary>
    Unhealthy = 4,

    /// <summary>Serviço parado intencionalmente.</summary>
    Stopped = 5
}
