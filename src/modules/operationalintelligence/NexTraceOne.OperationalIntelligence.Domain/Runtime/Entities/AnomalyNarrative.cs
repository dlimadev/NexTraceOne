using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Entidade que representa uma narrativa de anomalia gerada por IA.
/// Consolida sintomas, comparação com baseline, causa provável, mudanças correlacionadas,
/// ações recomendadas e justificação de severidade num texto estruturado e contextualizado.
/// Ligada a um DriftFinding; suporta regeneração (refresh) com rastreio de versões.
/// </summary>
public sealed class AnomalyNarrative : AuditableEntity<AnomalyNarrativeId>
{
    private AnomalyNarrative() { }

    /// <summary>Identificador do drift finding associado.</summary>
    public DriftFindingId DriftFindingId { get; private set; } = null!;

    /// <summary>Texto completo da narrativa gerada.</summary>
    public string NarrativeText { get; private set; } = string.Empty;

    /// <summary>Secção de sintomas — o que a anomalia aparenta.</summary>
    public string? SymptomsSection { get; private set; }

    /// <summary>Secção de comparação com baseline — baseline vs actual.</summary>
    public string? BaselineComparisonSection { get; private set; }

    /// <summary>Secção de causa provável.</summary>
    public string? ProbableCauseSection { get; private set; }

    /// <summary>Secção de mudanças correlacionadas.</summary>
    public string? CorrelatedChangesSection { get; private set; }

    /// <summary>Secção de ações recomendadas.</summary>
    public string? RecommendedActionsSection { get; private set; }

    /// <summary>Secção de justificação da severidade.</summary>
    public string? SeverityJustificationSection { get; private set; }

    /// <summary>Modelo de IA utilizado para gerar a narrativa.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Número de tokens consumidos na geração.</summary>
    public int TokensUsed { get; private set; }

    /// <summary>Estado actual da narrativa (Draft, Published, Stale).</summary>
    public AnomalyNarrativeStatus Status { get; private set; }

    /// <summary>Identificador do tenant ao qual a narrativa pertence.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data/hora UTC da geração da narrativa.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Data/hora UTC da última regeneração (refresh).</summary>
    public DateTimeOffset? LastRefreshedAt { get; private set; }

    /// <summary>Número de vezes que a narrativa foi regenerada.</summary>
    public int RefreshCount { get; private set; }

    /// <summary>
    /// Factory method para criação de uma AnomalyNarrative com validações de guarda.
    /// </summary>
    public static AnomalyNarrative Create(
        AnomalyNarrativeId id,
        DriftFindingId driftFindingId,
        string narrativeText,
        string? symptomsSection,
        string? baselineComparisonSection,
        string? probableCauseSection,
        string? correlatedChangesSection,
        string? recommendedActionsSection,
        string? severityJustificationSection,
        string modelUsed,
        int tokensUsed,
        AnomalyNarrativeStatus status,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Null(driftFindingId);
        Guard.Against.NullOrWhiteSpace(narrativeText);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        return new AnomalyNarrative
        {
            Id = id,
            DriftFindingId = driftFindingId,
            NarrativeText = narrativeText,
            SymptomsSection = symptomsSection,
            BaselineComparisonSection = baselineComparisonSection,
            ProbableCauseSection = probableCauseSection,
            CorrelatedChangesSection = correlatedChangesSection,
            RecommendedActionsSection = recommendedActionsSection,
            SeverityJustificationSection = severityJustificationSection,
            ModelUsed = modelUsed,
            TokensUsed = tokensUsed,
            Status = status,
            TenantId = tenantId,
            GeneratedAt = generatedAt,
            RefreshCount = 0,
        };
    }

    /// <summary>
    /// Marca a narrativa como desatualizada (Stale) quando os dados da anomalia mudam.
    /// </summary>
    public void MarkAsStale()
    {
        Status = AnomalyNarrativeStatus.Stale;
    }

    /// <summary>
    /// Regenera a narrativa com novos dados, incrementa o contador de refresh
    /// e atualiza o timestamp de última regeneração.
    /// </summary>
    public void Refresh(
        string narrativeText,
        string? symptomsSection,
        string? baselineComparisonSection,
        string? probableCauseSection,
        string? correlatedChangesSection,
        string? recommendedActionsSection,
        string? severityJustificationSection,
        string modelUsed,
        int tokensUsed,
        DateTimeOffset refreshedAt)
    {
        Guard.Against.NullOrWhiteSpace(narrativeText);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        NarrativeText = narrativeText;
        SymptomsSection = symptomsSection;
        BaselineComparisonSection = baselineComparisonSection;
        ProbableCauseSection = probableCauseSection;
        CorrelatedChangesSection = correlatedChangesSection;
        RecommendedActionsSection = recommendedActionsSection;
        SeverityJustificationSection = severityJustificationSection;
        ModelUsed = modelUsed;
        TokensUsed = tokensUsed;
        Status = AnomalyNarrativeStatus.Draft;
        LastRefreshedAt = refreshedAt;
        RefreshCount++;
    }
}

/// <summary>Identificador fortemente tipado de AnomalyNarrative.</summary>
public sealed record AnomalyNarrativeId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static AnomalyNarrativeId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AnomalyNarrativeId From(Guid id) => new(id);
}
