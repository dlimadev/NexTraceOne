namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado de um evento de deployment de uma versão de contrato.
/// Alimenta o Change Intelligence e a rastreabilidade de mudanças por ambiente.
/// </summary>
public enum ContractDeploymentStatus
{
    /// <summary>Deployment registado mas ainda não concluído.</summary>
    Pending = 0,

    /// <summary>Deployment concluído com sucesso no ambiente de destino.</summary>
    Success = 1,

    /// <summary>Deployment falhou — contrato não chegou ao estado esperado.</summary>
    Failed = 2,

    /// <summary>Deployment revertido para versão anterior após problemas detetados.</summary>
    Rollback = 3,
}
