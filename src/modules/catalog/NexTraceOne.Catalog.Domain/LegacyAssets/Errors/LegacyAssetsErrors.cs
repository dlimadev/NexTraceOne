using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

/// <summary>
/// Catálogo centralizado de erros do sub-domínio Legacy Assets com códigos i18n.
/// </summary>
public static class LegacyAssetsErrors
{
    // ── MainframeSystem ──────────────────────────────────────────────────

    /// <summary>Sistema mainframe não encontrado.</summary>
    public static Error MainframeSystemNotFound(Guid systemId)
        => Error.NotFound("LegacyAssets.MainframeSystem.NotFound", "Mainframe system '{0}' was not found.", systemId);

    /// <summary>Sistema mainframe já existe com o mesmo nome.</summary>
    public static Error MainframeSystemAlreadyExists(string name)
        => Error.Conflict("LegacyAssets.MainframeSystem.AlreadyExists", "Mainframe system '{0}' already exists.", name);

    // ── CobolProgram ─────────────────────────────────────────────────────

    /// <summary>Programa COBOL não encontrado.</summary>
    public static Error CobolProgramNotFound(Guid programId)
        => Error.NotFound("LegacyAssets.CobolProgram.NotFound", "COBOL program '{0}' was not found.", programId);

    /// <summary>Programa COBOL já existe com o mesmo nome e sistema.</summary>
    public static Error CobolProgramAlreadyExists(string name, Guid systemId)
        => Error.Conflict("LegacyAssets.CobolProgram.AlreadyExists", "COBOL program '{0}' already exists for system '{1}'.", name, systemId);

    // ── Copybook ─────────────────────────────────────────────────────────

    /// <summary>Copybook não encontrado.</summary>
    public static Error CopybookNotFound(Guid copybookId)
        => Error.NotFound("LegacyAssets.Copybook.NotFound", "Copybook '{0}' was not found.", copybookId);

    /// <summary>Copybook já existe com o mesmo nome e sistema.</summary>
    public static Error CopybookAlreadyExists(string name, Guid systemId)
        => Error.Conflict("LegacyAssets.Copybook.AlreadyExists", "Copybook '{0}' already exists for system '{1}'.", name, systemId);

    // ── CicsTransaction ──────────────────────────────────────────────────

    /// <summary>Transação CICS não encontrada.</summary>
    public static Error CicsTransactionNotFound(Guid transactionId)
        => Error.NotFound("LegacyAssets.CicsTransaction.NotFound", "CICS transaction '{0}' was not found.", transactionId);

    /// <summary>Transação CICS já existe com o mesmo ID e sistema.</summary>
    public static Error CicsTransactionAlreadyExists(string transactionId, Guid systemId)
        => Error.Conflict("LegacyAssets.CicsTransaction.AlreadyExists", "CICS transaction '{0}' already exists for system '{1}'.", transactionId, systemId);

    // ── ImsTransaction ───────────────────────────────────────────────────

    /// <summary>Transação IMS não encontrada.</summary>
    public static Error ImsTransactionNotFound(Guid transactionId)
        => Error.NotFound("LegacyAssets.ImsTransaction.NotFound", "IMS transaction '{0}' was not found.", transactionId);

    /// <summary>Transação IMS já existe com o mesmo código e sistema.</summary>
    public static Error ImsTransactionAlreadyExists(string transactionCode, Guid systemId)
        => Error.Conflict("LegacyAssets.ImsTransaction.AlreadyExists", "IMS transaction '{0}' already exists for system '{1}'.", transactionCode, systemId);

    // ── Db2Artifact ──────────────────────────────────────────────────────

    /// <summary>Artefacto DB2 não encontrado.</summary>
    public static Error Db2ArtifactNotFound(Guid artifactId)
        => Error.NotFound("LegacyAssets.Db2Artifact.NotFound", "DB2 artifact '{0}' was not found.", artifactId);

    /// <summary>Artefacto DB2 já existe com o mesmo nome e sistema.</summary>
    public static Error Db2ArtifactAlreadyExists(string name, Guid systemId)
        => Error.Conflict("LegacyAssets.Db2Artifact.AlreadyExists", "DB2 artifact '{0}' already exists for system '{1}'.", name, systemId);

    // ── ZosConnectBinding ────────────────────────────────────────────────

    /// <summary>Binding z/OS Connect não encontrado.</summary>
    public static Error ZosConnectBindingNotFound(Guid bindingId)
        => Error.NotFound("LegacyAssets.ZosConnectBinding.NotFound", "z/OS Connect binding '{0}' was not found.", bindingId);

    /// <summary>Binding z/OS Connect já existe com o mesmo nome e sistema.</summary>
    public static Error ZosConnectBindingAlreadyExists(string name, Guid systemId)
        => Error.Conflict("LegacyAssets.ZosConnectBinding.AlreadyExists", "z/OS Connect binding '{0}' already exists for system '{1}'.", name, systemId);

    // ── Genérico ─────────────────────────────────────────────────────────

    /// <summary>Tipo de ativo legacy inválido.</summary>
    public static Error InvalidAssetType(string assetType)
        => Error.Validation("LegacyAssets.InvalidAssetType", "Asset type '{0}' is not a valid legacy asset type.", assetType);
}
