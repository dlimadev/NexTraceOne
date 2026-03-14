using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.EngineeringGraph.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo EngineeringGraph com códigos i18n.
/// </summary>
public static class EngineeringGraphErrors
{
    /// <summary>Ativo de API não encontrado.</summary>
    public static Error ApiAssetNotFound(Guid apiAssetId)
        => Error.NotFound("EngineeringGraph.ApiAsset.NotFound", "API asset '{0}' was not found.", apiAssetId);

    /// <summary>Ativo de serviço não encontrado.</summary>
    public static Error ServiceAssetNotFound(string name)
        => Error.NotFound("EngineeringGraph.ServiceAsset.NotFound", "Service asset '{0}' was not found.", name);

    /// <summary>Ativo de serviço já existe com o mesmo nome.</summary>
    public static Error ServiceAssetAlreadyExists(string name)
        => Error.Conflict("EngineeringGraph.ServiceAsset.AlreadyExists", "Service asset '{0}' already exists.", name);

    /// <summary>Ativo de API já existe com o mesmo nome e serviço proprietário.</summary>
    public static Error ApiAssetAlreadyExists(string name)
        => Error.Conflict("EngineeringGraph.ApiAsset.AlreadyExists", "API asset '{0}' already exists for this owner service.", name);

    /// <summary>Fonte de descoberta duplicada para o mesmo ativo.</summary>
    public static Error DuplicateDiscoverySource(string sourceType, string externalReference)
        => Error.Conflict(
            "EngineeringGraph.DiscoverySource.Duplicate",
            "Discovery source '{0}' with reference '{1}' is already registered for this API asset.",
            sourceType,
            externalReference);

    /// <summary>Ativo de API já descomissionado.</summary>
    public static Error ApiAssetDecommissioned(Guid apiAssetId)
        => Error.Conflict("EngineeringGraph.ApiAsset.Decommissioned", "API asset '{0}' is decommissioned.", apiAssetId);

    /// <summary>Relação de consumidor não encontrada.</summary>
    public static Error ConsumerRelationshipNotFound(Guid relationshipId)
        => Error.NotFound("EngineeringGraph.ConsumerRelationship.NotFound", "Consumer relationship '{0}' was not found.", relationshipId);

    /// <summary>Confiança da dependência descoberta abaixo do mínimo.</summary>
    public static Error LowConfidenceDependency(string consumerName, decimal actual, decimal minimum)
        => Error.Business(
            "EngineeringGraph.Dependency.LowConfidence",
            "Dependency to '{0}' has confidence '{1:P0}' below minimum '{2:P0}'.",
            consumerName,
            actual,
            minimum);

    // ── Temporal / Snapshot ───────────────────────────────────────────────

    /// <summary>Snapshot do grafo não encontrado pelo identificador.</summary>
    public static Error GraphSnapshotNotFound(Guid snapshotId)
        => Error.NotFound("EngineeringGraph.GraphSnapshot.NotFound", "Graph snapshot '{0}' was not found.", snapshotId);

    /// <summary>Snapshot de referência (baseline) não encontrado para comparação temporal.</summary>
    public static Error BaselineSnapshotNotFound()
        => Error.NotFound("EngineeringGraph.GraphSnapshot.BaselineNotFound", "No baseline snapshot was found for temporal comparison.");

    // ── Saved Views ──────────────────────────────────────────────────────

    /// <summary>Visão salva do grafo não encontrada.</summary>
    public static Error SavedViewNotFound(Guid viewId)
        => Error.NotFound("EngineeringGraph.SavedView.NotFound", "Saved graph view '{0}' was not found.", viewId);

    /// <summary>Usuário não tem permissão para acessar a visão salva.</summary>
    public static Error SavedViewAccessDenied(Guid viewId)
        => Error.Forbidden("EngineeringGraph.SavedView.AccessDenied", "Access denied to saved graph view '{0}'.", viewId);

    // ── Impact Propagation ───────────────────────────────────────────────

    /// <summary>Nó raiz para propagação de impacto não encontrado.</summary>
    public static Error ImpactRootNodeNotFound(Guid nodeId)
        => Error.NotFound("EngineeringGraph.Impact.RootNotFound", "Impact root node '{0}' was not found.", nodeId);

    // ── Node Health / Overlay ────────────────────────────────────────────

    /// <summary>Dados de saúde não disponíveis para o nó solicitado.</summary>
    public static Error NodeHealthNotAvailable(Guid nodeId)
        => Error.NotFound("EngineeringGraph.NodeHealth.NotAvailable", "Health data is not available for node '{0}'.", nodeId);
}
