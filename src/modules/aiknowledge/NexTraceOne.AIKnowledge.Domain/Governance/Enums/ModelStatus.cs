namespace NexTraceOne.AiGovernance.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de um modelo de IA no Model Registry.
/// Controla a disponibilidade do modelo para uso nas políticas de acesso.
/// </summary>
public enum ModelStatus
{
    /// <summary>Modelo ativo e disponível para utilização.</summary>
    Active,

    /// <summary>Modelo inativo — temporariamente indisponível.</summary>
    Inactive,

    /// <summary>Modelo depreciado — será removido futuramente.</summary>
    Deprecated,

    /// <summary>Modelo bloqueado por política — uso proibido.</summary>
    Blocked
}
