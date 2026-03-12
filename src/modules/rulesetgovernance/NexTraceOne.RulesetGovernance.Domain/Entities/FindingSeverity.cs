namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Severidade de um finding de linting, compatível com o padrão Spectral.
/// </summary>
public enum FindingSeverity
{
    /// <summary>Erro crítico que deve ser corrigido antes do deploy.</summary>
    Error = 0,

    /// <summary>Aviso que deve ser avaliado, mas não bloqueia deploy.</summary>
    Warning = 1,

    /// <summary>Informação que pode melhorar a qualidade do contrato.</summary>
    Info = 2,

    /// <summary>Sugestão de melhoria de baixa prioridade.</summary>
    Hint = 3
}
