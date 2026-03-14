namespace NexTraceOne.AiOrchestration.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de uma conversa multi-turno com IA.
/// Fluxo: Active → (Completed | Expired).
/// </summary>
public enum ConversationStatus
{
    /// <summary>Conversa em andamento — aceita novos turnos de interação.</summary>
    Active = 0,

    /// <summary>Conversa concluída — resumo gerado e finalizada pelo usuário.</summary>
    Completed = 1,

    /// <summary>Conversa expirada — encerrada por inatividade ou timeout.</summary>
    Expired = 2
}
