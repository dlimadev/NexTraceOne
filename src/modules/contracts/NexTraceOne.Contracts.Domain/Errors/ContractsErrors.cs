using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Contracts.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Contracts com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Contracts.{Entidade}.{Descrição}
/// </summary>
public static class ContractsErrors
{
    /// <summary>Versão de contrato não encontrada.</summary>
    public static Error ContractVersionNotFound(string id)
        => Error.NotFound("Contracts.ContractVersion.NotFound", "Contract version '{0}' was not found.", id);

    /// <summary>Versão semântica já existe para este ativo de API.</summary>
    public static Error AlreadyExists(string semVer, string apiAssetId)
        => Error.Conflict("Contracts.ContractVersion.AlreadyExists", "Contract version '{0}' already exists for API asset '{1}'.", semVer, apiAssetId);

    /// <summary>A versão de contrato já está bloqueada.</summary>
    public static Error AlreadyLocked(string semVer)
        => Error.Conflict("Contracts.ContractVersion.AlreadyLocked", "Contract version '{0}' is already locked.", semVer);

    /// <summary>Versão semântica inválida — deve seguir o formato Major.Minor.Patch.</summary>
    public static Error InvalidSemVer(string version)
        => Error.Validation("Contracts.ContractVersion.InvalidSemVer", "'{0}' is not a valid semantic version. Expected format: Major.Minor.Patch.", version);

    /// <summary>Conteúdo da especificação OpenAPI está vazio.</summary>
    public static Error EmptySpecContent()
        => Error.Validation("Contracts.ContractVersion.EmptySpecContent", "The OpenAPI spec content cannot be empty.");

    /// <summary>Nenhuma versão anterior encontrada para este ativo de API.</summary>
    public static Error NoPreviousVersion(string apiAssetId)
        => Error.Business("Contracts.ContractVersion.NoPreviousVersion", "No previous contract version found for API asset '{0}'.", apiAssetId);

    /// <summary>Falha ao computar o diff entre versões de contrato.</summary>
    public static Error DiffComputationFailed(string reason)
        => Error.Business("Contracts.ContractDiff.ComputationFailed", "Failed to compute contract diff: {0}", reason);

    /// <summary>Nenhum diff encontrado para a versão de contrato informada.</summary>
    public static Error DiffNotFound(string contractVersionId)
        => Error.NotFound("Contracts.ContractDiff.NotFound", "No diff found for contract version '{0}'.", contractVersionId);
}
