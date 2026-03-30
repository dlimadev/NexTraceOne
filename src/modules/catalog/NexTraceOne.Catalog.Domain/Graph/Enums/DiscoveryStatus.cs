namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Estado de processamento de um serviço descoberto automaticamente.
/// Governa o fluxo de triagem: Pending → Matched | Ignored | Registered.
/// </summary>
public enum DiscoveryStatus
{
    /// <summary>Serviço descoberto aguardando triagem por um humano ou regra automática.</summary>
    Pending = 0,

    /// <summary>Serviço associado a um ServiceAsset existente no catálogo.</summary>
    Matched = 1,

    /// <summary>Serviço marcado como irrelevante para o catálogo.</summary>
    Ignored = 2,

    /// <summary>Serviço registado como novo ServiceAsset no catálogo a partir da descoberta.</summary>
    Registered = 3
}
