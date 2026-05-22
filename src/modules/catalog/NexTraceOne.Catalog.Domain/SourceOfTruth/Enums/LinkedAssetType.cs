namespace NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

/// <summary>
/// Tipo de ativo ao qual uma referência está vinculada.
/// Permite associar referências a serviços, contratos e ativos legacy (mainframe).
/// </summary>
public enum LinkedAssetType
{
    /// <summary>Referência vinculada a um serviço do catálogo.</summary>
    Service = 0,

    /// <summary>Referência vinculada a um contrato.</summary>
    Contract = 1,

    // ── Ativos Legacy (mainframe) ──────────────────────────────────────────

    /// <summary>Referência vinculada a um sistema mainframe (LPAR, sysplex, região).</summary>
    MainframeSystem = 2,

    /// <summary>Referência vinculada a um programa COBOL.</summary>
    CobolProgram = 3,

    /// <summary>Referência vinculada a uma transação CICS.</summary>
    CicsTransaction = 4,

    /// <summary>Referência vinculada a uma transação IMS.</summary>
    ImsTransaction = 5,

    /// <summary>Referência vinculada a um artefacto DB2 (tabela, view, stored procedure).</summary>
    Db2Artifact = 6,

    /// <summary>Referência vinculada a um binding z/OS Connect (exposição REST de programa mainframe).</summary>
    ZosConnectBinding = 7,

    /// <summary>Referência vinculada a um copybook COBOL (layout de dados).</summary>
    Copybook = 8
}
