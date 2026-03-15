namespace NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

/// <summary>
/// Tipo de ativo ao qual uma referência está vinculada.
/// Permite associar referências tanto a serviços quanto a contratos.
/// </summary>
public enum LinkedAssetType
{
    /// <summary>Referência vinculada a um serviço do catálogo.</summary>
    Service = 0,

    /// <summary>Referência vinculada a um contrato.</summary>
    Contract = 1
}
