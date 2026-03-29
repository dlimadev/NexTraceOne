namespace NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

/// <summary>
/// Tipo de artefacto DB2.
/// Classifica o tipo de objeto na base de dados DB2.
/// </summary>
public enum Db2ArtifactType
{
    /// <summary>Tabela DB2.</summary>
    Table = 0,

    /// <summary>Vista DB2.</summary>
    View = 1,

    /// <summary>Stored Procedure DB2.</summary>
    StoredProcedure = 2,

    /// <summary>Índice DB2.</summary>
    Index = 3,

    /// <summary>Tablespace DB2.</summary>
    Tablespace = 4,

    /// <summary>Package DB2 (plano de acesso compilado).</summary>
    Package = 5
}
