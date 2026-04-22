using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Classificador de intenção de prompt baseado em heurísticas de palavras-chave.
/// Não depende de modelos externos — classifica localmente com regras determinísticas.
/// </summary>
public interface IPromptIntentClassifier
{
    /// <summary>
    /// Classifica a intenção de um prompt e retorna a intenção com nível de confiança.
    /// </summary>
    (PromptIntent Intent, double Confidence) Classify(string prompt);
}
