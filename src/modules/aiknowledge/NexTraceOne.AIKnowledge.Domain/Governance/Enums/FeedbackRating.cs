namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Classificação qualitativa do feedback do utilizador sobre interações de IA.
/// </summary>
public enum FeedbackRating
{
    /// <summary>Feedback positivo — a resposta foi útil.</summary>
    Positive = 1,

    /// <summary>Feedback negativo — a resposta não foi útil.</summary>
    Negative = 2,

    /// <summary>Feedback neutro — sem opinião forte.</summary>
    Neutral = 3
}
