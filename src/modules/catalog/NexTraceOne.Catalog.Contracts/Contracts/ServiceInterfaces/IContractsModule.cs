using NexTraceOne.BuildingBlocks.Core.Enums;

namespace NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Contracts.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IContractsModule
{
    /// <summary>Retorna o nível de mudança da versão mais recente do contrato de um ativo.</summary>
    Task<ChangeLevel?> GetLatestChangeLevelAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Verifica se existe alguma versão de contrato para um ativo.</summary>
    Task<bool> HasContractVersionAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Retorna o score consolidado do scorecard técnico do contrato mais recente de um ativo.</summary>
    Task<decimal?> GetLatestOverallScoreAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Verifica se a mudança mais recente de um ativo requer aprovação de workflow.</summary>
    Task<bool> RequiresWorkflowApprovalAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>
    /// Retorna resumos de breaking changes detectados num intervalo de tempo.
    /// Usado para anotações de dashboard — fonte "contracts".
    /// </summary>
    Task<IReadOnlyList<ContractBreakingChangeSummary>> GetRecentBreakingChangesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCount = 50,
        CancellationToken ct = default);
}

/// <summary>Resumo de um breaking change detectado por diff de contrato.</summary>
public sealed record ContractBreakingChangeSummary(
    Guid ApiAssetId,
    string ApiAssetName,
    string? OwnerServiceName,
    int BreakingChangeCount,
    DateTimeOffset DetectedAt);
