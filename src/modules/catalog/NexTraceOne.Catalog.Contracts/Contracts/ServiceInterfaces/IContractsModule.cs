using NexTraceOne.BuildingBlocks.Core.Enums;

namespace NexTraceOne.Contracts.Contracts.ServiceInterfaces;

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
}
