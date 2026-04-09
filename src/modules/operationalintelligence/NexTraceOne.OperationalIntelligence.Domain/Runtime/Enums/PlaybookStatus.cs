namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado do ciclo de vida de um playbook operacional.
/// Transições válidas: Draft → Active → Deprecated.
/// </summary>
public enum PlaybookStatus
{
    /// <summary>Playbook em rascunho — editável, não pode ser executado.</summary>
    Draft = 0,

    /// <summary>Playbook ativo — aprovado e pronto para execução.</summary>
    Active = 1,

    /// <summary>Playbook descontinuado — não pode ser executado.</summary>
    Deprecated = 2
}
