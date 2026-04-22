namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Abstração de leitura de relações de conhecimento entre entidades do catálogo.
///
/// Fornece dados agregados de serviços com as suas relações estruturais — dependências,
/// contratos publicados/consumidos, runbooks associados e tipos de incidente correlacionados.
/// Desacopla o handler de grafo de conhecimento das implementações concretas de repositório.
///
/// Wave AB.1 — GetKnowledgeRelationGraph.
/// </summary>
public interface IKnowledgeRelationReader
{
    /// <summary>
    /// Lista todas as entradas de relação de serviços para um tenant.
    /// Cada entrada agrega as relações estruturais de um serviço com outras entidades do catálogo.
    /// </summary>
    Task<IReadOnlyList<ServiceRelationEntry>> ListServiceRelationsAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entrada de relação de conhecimento de um serviço.
/// Agrega dependências, contratos, runbooks e incidentes associados ao serviço.
/// Wave AB.1.
/// </summary>
public sealed record ServiceRelationEntry(
    /// <summary>Nome do serviço.</summary>
    string ServiceName,
    /// <summary>Nome da equipa proprietária, ou null se não atribuído.</summary>
    string? TeamName,
    /// <summary>Lista de serviços dos quais este serviço depende directamente.</summary>
    IReadOnlyList<string> DependsOnServices,
    /// <summary>Lista de contratos publicados por este serviço.</summary>
    IReadOnlyList<string> PublishedContracts,
    /// <summary>Lista de contratos consumidos por este serviço.</summary>
    IReadOnlyList<string> ConsumedContracts,
    /// <summary>Lista de nomes de runbooks associados a este serviço.</summary>
    IReadOnlyList<string> AssociatedRunbooks,
    /// <summary>Lista de tipos de incidente correlacionados com este serviço.</summary>
    IReadOnlyList<string> AssociatedIncidentTypes,
    /// <summary>Data do último release associado, ou null se não disponível.</summary>
    DateTimeOffset? LastReleaseAt,
    /// <summary>Data do último incidente registado, ou null se não disponível.</summary>
    DateTimeOffset? LastIncidentAt);
