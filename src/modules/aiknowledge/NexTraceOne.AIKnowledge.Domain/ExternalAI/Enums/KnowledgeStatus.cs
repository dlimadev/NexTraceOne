namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;

/// <summary>
/// Estado de revisão de um conhecimento capturado a partir de interações com IA.
/// Fluxo: Pending → (Approved | Rejected).
/// </summary>
public enum KnowledgeStatus
{
    /// <summary>Conhecimento capturado, aguardando revisão humana.</summary>
    Pending = 0,

    /// <summary>Conhecimento aprovado — disponível para reutilização organizacional.</summary>
    Approved = 1,

    /// <summary>Conhecimento rejeitado — descartado por falta de relevância ou precisão.</summary>
    Rejected = 2
}
