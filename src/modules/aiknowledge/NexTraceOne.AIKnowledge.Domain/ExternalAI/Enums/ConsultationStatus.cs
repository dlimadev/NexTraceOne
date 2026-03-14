namespace NexTraceOne.ExternalAi.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de uma consulta a um provedor externo de IA.
/// Fluxo: Pending → InProgress → (Completed | Failed).
/// </summary>
public enum ConsultationStatus
{
    /// <summary>Consulta criada, aguardando envio ao provedor.</summary>
    Pending = 0,

    /// <summary>Consulta enviada ao provedor, aguardando resposta.</summary>
    InProgress = 1,

    /// <summary>Consulta concluída com sucesso — resposta recebida.</summary>
    Completed = 2,

    /// <summary>Consulta falhou — erro no provedor ou timeout.</summary>
    Failed = 3
}
