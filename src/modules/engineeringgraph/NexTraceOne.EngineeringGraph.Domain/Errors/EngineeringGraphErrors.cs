using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.EngineeringGraph.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo EngineeringGraph com códigos i18n.
/// </summary>
public static class EngineeringGraphErrors
{
    /// <summary>Ativo de API não encontrado.</summary>
    public static Error ApiAssetNotFound(Guid apiAssetId)
        => Error.NotFound("EngineeringGraph.ApiAsset.NotFound", "API asset '{0}' was not found.", apiAssetId);

    /// <summary>Fonte de descoberta duplicada para o mesmo ativo.</summary>
    public static Error DuplicateDiscoverySource(string sourceType, string externalReference)
        => Error.Conflict(
            "EngineeringGraph.DiscoverySource.Duplicate",
            "Discovery source '{0}' with reference '{1}' is already registered for this API asset.",
            sourceType,
            externalReference);
}
