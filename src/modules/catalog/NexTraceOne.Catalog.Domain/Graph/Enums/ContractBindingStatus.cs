namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Estado de um vínculo entre interface e versão de contrato.
/// </summary>
public enum ContractBindingStatus
{
    /// <summary>Vínculo activo — versão do contrato está em vigor para esta interface.</summary>
    Active = 0,

    /// <summary>Vínculo marcado como obsoleto — versão substituída mas ainda referenciada.</summary>
    Deprecated = 1,

    /// <summary>Vínculo em período de sunset — prazo de remoção definido.</summary>
    Sunset = 2
}
