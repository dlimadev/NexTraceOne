namespace NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Catalog Graph.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface ICatalogGraphModule
{
    /// <summary>Verifica se um ativo de API existe pelo seu identificador.</summary>
    Task<bool> ApiAssetExistsAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>Verifica se um ativo de serviço existe pelo nome único.</summary>
    Task<bool> ServiceAssetExistsAsync(string serviceName, CancellationToken cancellationToken);
}
