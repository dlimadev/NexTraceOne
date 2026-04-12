namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Resultado da avaliação de um gate de compliance contratual.
/// Indica se a versão do contrato passou, gerou aviso ou foi bloqueada.
/// </summary>
public enum ComplianceEvaluationResult
{
    /// <summary>A versão do contrato passou na avaliação de compliance.</summary>
    Pass = 0,

    /// <summary>A versão do contrato gerou avisos mas não foi bloqueada.</summary>
    Warn = 1,

    /// <summary>A versão do contrato foi bloqueada por violações de compliance.</summary>
    Block = 2
}
