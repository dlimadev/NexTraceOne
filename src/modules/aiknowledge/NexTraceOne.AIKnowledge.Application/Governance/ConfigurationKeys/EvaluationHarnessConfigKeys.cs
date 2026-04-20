namespace NexTraceOne.AIKnowledge.Application.Governance.ConfigurationKeys;

/// <summary>
/// Chaves de configuração do AI Evaluation Harness (ADR-009).
/// Todos os parâmetros são geridos via IConfigurationResolutionService + ConfigurationDefinitionSeeder.
/// </summary>
public static class EvaluationHarnessConfigKeys
{
    /// <summary>Latência máxima aceitável por caso de avaliação (ms).</summary>
    public const string DefaultLatencyBudgetMs = "ai.evaluation.defaults.latencyBudgetMs";

    /// <summary>Modelo LLM-as-Judge padrão para avaliação qualitativa.</summary>
    public const string LlmJudgeModel = "ai.evaluation.defaults.llmJudge.model";

    /// <summary>Número de dias de retenção de execuções de avaliação.</summary>
    public const string RunRetentionDays = "ai.evaluation.runs.retentionDays";

    /// <summary>Número máximo de execuções de avaliação em paralelo.</summary>
    public const string MaxConcurrency = "ai.evaluation.runs.maxConcurrency";

    /// <summary>SLA em horas para revisão humana de casos pendentes.</summary>
    public const string HumanReviewSlaHours = "ai.evaluation.humanReview.slaHours";
}
