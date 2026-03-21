using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que representa a avaliação de prontidão (readiness) de um serviço
/// para promoção de um ambiente não produtivo para o próximo estágio.
///
/// O ReadinessAssessment é o resultado final da análise da IA, combinando:
/// - RiskFindings identificados
/// - RegressionSignals detectados
/// - Score de prontidão calculado (0-100)
/// - Recomendação final (Promote, Promote with Caution, Block)
/// - Rastreabilidade completa da análise
///
/// Este é o artefato que apoia a decisão humana final.
/// A IA recomenda — o humano decide.
/// </summary>
public sealed class ReadinessAssessment : ValueObject
{
    /// <summary>Identificador único desta avaliação para rastreabilidade.</summary>
    public Guid AssessmentId { get; }

    /// <summary>Tenant para o qual a avaliação foi realizada.</summary>
    public TenantId TenantId { get; }

    /// <summary>Ambiente de origem da análise.</summary>
    public EnvironmentId SourceEnvironmentId { get; }

    /// <summary>Ambiente de destino da promoção.</summary>
    public EnvironmentId TargetEnvironmentId { get; }

    /// <summary>Serviço avaliado.</summary>
    public string ServiceName { get; }

    /// <summary>Versão sendo avaliada para promoção.</summary>
    public string Version { get; }

    /// <summary>
    /// Score de prontidão (0-100).
    /// 100 = totalmente pronto, sem achados críticos.
    /// 0 = bloqueado, riscos críticos identificados.
    /// </summary>
    public int ReadinessScore { get; }

    /// <summary>Recomendação da IA para a promoção.</summary>
    public PromotionRecommendation Recommendation { get; }

    /// <summary>Achados de risco identificados durante a análise.</summary>
    public IReadOnlyList<RiskFinding> RiskFindings { get; }

    /// <summary>Sinais de regressão detectados durante a comparação.</summary>
    public IReadOnlyList<RegressionSignal> RegressionSignals { get; }

    /// <summary>
    /// Sumário executivo da avaliação gerado pela IA.
    /// Linguagem direta, orientada a ação, sem jargão técnico excessivo.
    /// </summary>
    public string ExecutiveSummary { get; }

    /// <summary>Data/hora UTC em que a avaliação foi gerada.</summary>
    public DateTimeOffset AssessedAt { get; }

    private ReadinessAssessment(
        Guid assessmentId,
        TenantId tenantId,
        EnvironmentId sourceEnvironmentId,
        EnvironmentId targetEnvironmentId,
        string serviceName,
        string version,
        int readinessScore,
        PromotionRecommendation recommendation,
        IReadOnlyList<RiskFinding> riskFindings,
        IReadOnlyList<RegressionSignal> regressionSignals,
        string executiveSummary,
        DateTimeOffset assessedAt)
    {
        AssessmentId = assessmentId;
        TenantId = tenantId;
        SourceEnvironmentId = sourceEnvironmentId;
        TargetEnvironmentId = targetEnvironmentId;
        ServiceName = serviceName;
        Version = version;
        ReadinessScore = readinessScore;
        Recommendation = recommendation;
        RiskFindings = riskFindings;
        RegressionSignals = regressionSignals;
        ExecutiveSummary = executiveSummary;
        AssessedAt = assessedAt;
    }

    /// <summary>
    /// Cria uma ReadinessAssessment a partir dos achados e sinais da análise.
    /// O score e a recomendação são calculados automaticamente.
    /// </summary>
    public static ReadinessAssessment Create(
        TenantId tenantId,
        EnvironmentId sourceEnvironmentId,
        EnvironmentId targetEnvironmentId,
        string serviceName,
        string version,
        IEnumerable<RiskFinding> riskFindings,
        IEnumerable<RegressionSignal> regressionSignals,
        string executiveSummary,
        DateTimeOffset assessedAt)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(sourceEnvironmentId);
        ArgumentNullException.ThrowIfNull(targetEnvironmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(executiveSummary);

        var findings = riskFindings.ToList().AsReadOnly();
        var signals = regressionSignals.ToList().AsReadOnly();

        var score = CalculateScore(findings, signals);
        var recommendation = DetermineRecommendation(score, findings);

        return new ReadinessAssessment(
            Guid.NewGuid(),
            tenantId,
            sourceEnvironmentId,
            targetEnvironmentId,
            serviceName,
            version,
            score,
            recommendation,
            findings,
            signals,
            executiveSummary,
            assessedAt);
    }

    /// <summary>
    /// Calcula o score de prontidão baseado nos achados e sinais.
    /// Penalidades: Critical=-40, High=-15, Warning=-5, Severe regression=-10, Significant=-5.
    /// </summary>
    private static int CalculateScore(
        IReadOnlyList<RiskFinding> findings,
        IReadOnlyList<RegressionSignal> signals)
    {
        var score = 100;

        foreach (var f in findings)
        {
            score -= f.Severity switch
            {
                RiskSeverity.Critical => ScoringPolicy.CriticalFindingPenalty,
                RiskSeverity.High => ScoringPolicy.HighFindingPenalty,
                RiskSeverity.Warning => ScoringPolicy.WarningFindingPenalty,
                _ => 0
            };
        }

        foreach (var s in signals.Where(x => x.IsDegradation))
        {
            score -= s.Intensity switch
            {
                RegressionIntensity.Severe => ScoringPolicy.SevereRegressionPenalty,
                RegressionIntensity.Significant => ScoringPolicy.SignificantRegressionPenalty,
                RegressionIntensity.Moderate => ScoringPolicy.ModerateRegressionPenalty,
                _ => 0
            };
        }

        return Math.Max(0, score);
    }

    /// <summary>Determina a recomendação com base no score e presença de achados críticos.</summary>
    private static PromotionRecommendation DetermineRecommendation(
        int score,
        IReadOnlyList<RiskFinding> findings)
    {
        if (findings.Any(f => f.Severity == RiskSeverity.Critical))
            return PromotionRecommendation.Block;

        return score switch
        {
            >= ScoringPolicy.PromoteThreshold => PromotionRecommendation.Promote,
            >= ScoringPolicy.PromoteWithCautionThreshold => PromotionRecommendation.PromoteWithCaution,
            _ => PromotionRecommendation.Block
        };
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AssessmentId;
    }
}

/// <summary>
/// Constantes de pontuação para o cálculo do ReadinessScore.
/// Extraídas para facilitar manutenção e tornar os pesos explícitos.
/// </summary>
public static class ScoringPolicy
{
    public const int CriticalFindingPenalty = 40;
    public const int HighFindingPenalty = 15;
    public const int WarningFindingPenalty = 5;
    public const int SevereRegressionPenalty = 10;
    public const int SignificantRegressionPenalty = 5;
    public const int ModerateRegressionPenalty = 2;
    public const int PromoteThreshold = 80;
    public const int PromoteWithCautionThreshold = 50;
}

/// <summary>Recomendação da IA para promoção entre ambientes.</summary>
public enum PromotionRecommendation
{
    /// <summary>Promover. Score alto, sem achados críticos.</summary>
    Promote = 1,

    /// <summary>Promover com cautela. Score moderado ou achados de aviso.</summary>
    PromoteWithCaution = 2,

    /// <summary>Bloquear promoção. Score baixo ou achados críticos presentes.</summary>
    Block = 3
}
