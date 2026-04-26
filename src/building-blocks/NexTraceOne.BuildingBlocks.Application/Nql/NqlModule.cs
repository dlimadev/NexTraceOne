namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>
/// Módulos disponíveis na NexTraceOne Query Language (NQL).
/// Cada módulo expõe um ou mais <see cref="NqlEntity"/> consultáveis.
/// A execução de queries cross-módulo é mediada pelo <see cref="IQueryGovernanceService"/>
/// que garante isolamento de tenant, ambiente e persona.
/// </summary>
public enum NqlModule
{
    Catalog = 0,
    Changes = 1,
    Operations = 2,
    Knowledge = 3,
    FinOps = 4,
    Governance = 5
}

/// <summary>
/// Entidades consultáveis dentro de cada módulo NQL.
/// Formato no texto NQL: "<c>module.entity</c>" (ex: "catalog.services").
/// </summary>
public enum NqlEntity
{
    // Catalog
    CatalogServices = 0,
    CatalogContracts = 1,

    // Changes
    ChangesReleases = 10,
    ChangesChangeScores = 11,

    // Operations
    OperationsIncidents = 20,
    OperationsSlos = 21,

    // Knowledge
    KnowledgeDocs = 30,

    // FinOps
    FinOpsCosts = 40,

    // Governance
    GovernanceTeams = 50,
    GovernanceDomains = 51
}

/// <summary>Helper para mapear string "module.entity" para <see cref="NqlEntity"/>.</summary>
public static class NqlEntityMap
{
    private static readonly Dictionary<string, NqlEntity> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["catalog.services"]         = NqlEntity.CatalogServices,
        ["catalog.contracts"]        = NqlEntity.CatalogContracts,
        ["changes.releases"]         = NqlEntity.ChangesReleases,
        ["changes.changescores"]     = NqlEntity.ChangesChangeScores,
        ["operations.incidents"]     = NqlEntity.OperationsIncidents,
        ["operations.slos"]          = NqlEntity.OperationsSlos,
        ["knowledge.docs"]           = NqlEntity.KnowledgeDocs,
        ["finops.costs"]             = NqlEntity.FinOpsCosts,
        ["governance.teams"]         = NqlEntity.GovernanceTeams,
        ["governance.domains"]       = NqlEntity.GovernanceDomains
    };

    public static bool TryParse(string source, out NqlEntity entity) =>
        Map.TryGetValue(source, out entity);

    public static IReadOnlyCollection<string> ValidSources => Map.Keys;
}
