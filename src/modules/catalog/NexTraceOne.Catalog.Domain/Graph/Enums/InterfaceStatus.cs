namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Estado do ciclo de vida de uma interface de serviço.
/// Controla a visibilidade e elegibilidade para vínculo de contrato.
/// </summary>
public enum InterfaceStatus
{
    /// <summary>Interface activa e em uso.</summary>
    Active = 0,

    /// <summary>Interface marcada para descontinuação — ainda disponível.</summary>
    Deprecated = 1,

    /// <summary>Interface em período de sunset — prazo de remoção definido.</summary>
    Sunset = 2,

    /// <summary>Interface retirada — já não está disponível.</summary>
    Retired = 3
}
