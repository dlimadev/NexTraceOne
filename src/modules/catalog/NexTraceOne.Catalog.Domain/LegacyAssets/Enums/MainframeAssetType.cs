namespace NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

/// <summary>
/// Tipo de ativo mainframe no catálogo legacy.
/// Classifica a natureza do ativo para governança e catálogo.
/// </summary>
public enum MainframeAssetType
{
    /// <summary>Sistema mainframe (LPAR, sysplex, região).</summary>
    System = 0,

    /// <summary>Programa COBOL.</summary>
    Program = 1,

    /// <summary>Copybook — definição de layout de dados.</summary>
    Copybook = 2,

    /// <summary>Transação (CICS ou IMS).</summary>
    Transaction = 3,

    /// <summary>Batch Job (JCL, scheduling).</summary>
    Job = 4,

    /// <summary>Artefacto de base de dados (DB2).</summary>
    Artifact = 5,

    /// <summary>Binding z/OS Connect (exposição REST).</summary>
    Binding = 6
}
