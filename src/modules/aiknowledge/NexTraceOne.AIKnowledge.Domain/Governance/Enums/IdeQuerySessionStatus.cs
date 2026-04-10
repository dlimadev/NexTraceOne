namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado do ciclo de vida de uma sessão de consulta IDE governada.
/// </summary>
public enum IdeQuerySessionStatus
{
    /// <summary>Consulta em processamento — aguarda resposta do modelo de IA.</summary>
    Processing = 1,

    /// <summary>Resposta gerada com sucesso.</summary>
    Responded = 2,

    /// <summary>Consulta bloqueada por política de governança.</summary>
    Blocked = 3,

    /// <summary>Consulta falhou por erro técnico.</summary>
    Failed = 4
}
