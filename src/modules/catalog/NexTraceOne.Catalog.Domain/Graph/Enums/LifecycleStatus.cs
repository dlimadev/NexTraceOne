namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Estado do ciclo de vida do serviço.
/// Governa o que pode ou não ser feito com o serviço no catálogo.
/// </summary>
public enum LifecycleStatus
{
    /// <summary>Serviço em planeamento — ainda não está em desenvolvimento ativo.</summary>
    Planning = 0,

    /// <summary>Serviço aguardando aprovação — criado mas pendente de validação.</summary>
    PendingApproval = 7,

    /// <summary>Serviço em desenvolvimento ativo.</summary>
    Development = 1,

    /// <summary>Serviço em staging/pré-produção — validação final.</summary>
    Staging = 2,

    /// <summary>Serviço ativo em produção.</summary>
    Active = 3,

    /// <summary>Serviço em processo de descontinuação.</summary>
    Deprecating = 4,

    /// <summary>Serviço descontinuado — não deve receber novas dependências.</summary>
    Deprecated = 5,

    /// <summary>Serviço retirado/desligado — não mais operacional.</summary>
    Retired = 6
}
