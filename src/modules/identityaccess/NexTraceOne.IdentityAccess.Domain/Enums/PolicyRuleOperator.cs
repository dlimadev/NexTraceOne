namespace NexTraceOne.IdentityAccess.Domain.Enums;

/// <summary>Operador de comparação usado nas regras do Policy Studio.</summary>
public enum PolicyRuleOperator
{
    /// <summary>Igualdade exacta.</summary>
    Equals = 0,

    /// <summary>Diferença.</summary>
    NotEquals = 1,

    /// <summary>Maior que (numérico).</summary>
    GreaterThan = 2,

    /// <summary>Menor que (numérico).</summary>
    LessThan = 3,

    /// <summary>Contém substring.</summary>
    Contains = 4,

    /// <summary>Não contém substring.</summary>
    NotContains = 5,

    /// <summary>Correspondência de padrão simples.</summary>
    Matches = 6
}
