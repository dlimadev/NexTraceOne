namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Status de validação pós-mudança no NexTraceOne.
/// Indica se a mudança foi verificada e qual o resultado da verificação.
/// </summary>
public enum ValidationStatus
{
    /// <summary>Validação pendente — ainda não verificada.</summary>
    Pending = 0,

    /// <summary>Validação em progresso.</summary>
    InProgress = 1,

    /// <summary>Validação concluída com sucesso.</summary>
    Passed = 2,

    /// <summary>Validação concluída com falhas.</summary>
    Failed = 3,

    /// <summary>Validação ignorada (skip).</summary>
    Skipped = 4
}
