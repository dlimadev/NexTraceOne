using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Classificador de intenção de prompt baseado em heurísticas de palavras-chave.
/// Classifica localmente sem dependência de modelos externos.
/// Cada intenção tem um conjunto de palavras-chave ponderadas — a intenção com
/// maior contagem de correspondências é seleccionada. GeneralQuery é o fallback.
/// </summary>
public sealed class PromptIntentClassifierService : IPromptIntentClassifier
{
    private static readonly Dictionary<PromptIntent, string[]> Keywords = new()
    {
        [PromptIntent.CodeGeneration] =
            ["generate code", "write function", "implement", "class", "method"],
        [PromptIntent.DocumentSummarization] =
            ["summarize", "summary", "explain", "describe", "what is"],
        [PromptIntent.IncidentAnalysis] =
            ["incident", "outage", "error", "failure", "alert", "anomaly"],
        [PromptIntent.ContractDraft] =
            ["contract", "api spec", "openapi", "endpoint", "schema"],
        [PromptIntent.ComplianceCheck] =
            ["compliance", "gdpr", "soc2", "pci", "hipaa", "regulation", "audit"],
    };

    /// <inheritdoc />
    public (PromptIntent Intent, double Confidence) Classify(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return (PromptIntent.GeneralQuery, 0.0);

        var lower = prompt.ToLowerInvariant();

        var scores = Keywords
            .Select(kvp => (
                Intent: kvp.Key,
                Score: kvp.Value.Count(kw => lower.Contains(kw, StringComparison.Ordinal))
            ))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scores.Count == 0)
            return (PromptIntent.GeneralQuery, 1.0);

        var best = scores[0];
        var totalKeywords = Keywords[best.Intent].Length;
        var confidence = Math.Min(1.0, (double)best.Score / Math.Max(1, totalKeywords));

        return (best.Intent, Math.Round(confidence, 2));
    }
}
