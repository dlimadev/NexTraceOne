using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa uma narrativa de incidente gerada por IA.
/// Consolida sintomas, timeline, causa provável, mitigação, mudanças correlacionadas
/// e serviços afetados num texto estruturado e contextualizado.
/// Ligada a um IncidentRecord; suporta regeneração (refresh) com rastreio de versões.
/// </summary>
public sealed class IncidentNarrative : AuditableEntity<IncidentNarrativeId>
{
    private IncidentNarrative() { }

    /// <summary>Identificador do incidente associado.</summary>
    public Guid IncidentId { get; private set; }

    /// <summary>Texto completo da narrativa gerada.</summary>
    public string NarrativeText { get; private set; } = string.Empty;

    /// <summary>Secção de sintomas — o que aconteceu.</summary>
    public string? SymptomsSection { get; private set; }

    /// <summary>Secção de timeline — quando aconteceu.</summary>
    public string? TimelineSection { get; private set; }

    /// <summary>Secção de causa provável.</summary>
    public string? ProbableCauseSection { get; private set; }

    /// <summary>Secção de mitigação — o que foi feito para mitigar.</summary>
    public string? MitigationSection { get; private set; }

    /// <summary>Secção de mudanças correlacionadas.</summary>
    public string? RelatedChangesSection { get; private set; }

    /// <summary>Secção de serviços afetados.</summary>
    public string? AffectedServicesSection { get; private set; }

    /// <summary>Modelo de IA utilizado para gerar a narrativa.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Número de tokens consumidos na geração.</summary>
    public int TokensUsed { get; private set; }

    /// <summary>Estado actual da narrativa (Draft, Published, Stale).</summary>
    public NarrativeStatus Status { get; private set; }

    /// <summary>Identificador do tenant ao qual a narrativa pertence.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data/hora UTC da geração da narrativa.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Data/hora UTC da última regeneração (refresh).</summary>
    public DateTimeOffset? LastRefreshedAt { get; private set; }

    /// <summary>Número de vezes que a narrativa foi regenerada.</summary>
    public int RefreshCount { get; private set; }

    /// <summary>
    /// Factory method para criação de uma IncidentNarrative com validações de guarda.
    /// </summary>
    public static IncidentNarrative Create(
        IncidentNarrativeId id,
        Guid incidentId,
        string narrativeText,
        string? symptomsSection,
        string? timelineSection,
        string? probableCauseSection,
        string? mitigationSection,
        string? relatedChangesSection,
        string? affectedServicesSection,
        string modelUsed,
        int tokensUsed,
        NarrativeStatus status,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Default(incidentId);
        Guard.Against.NullOrWhiteSpace(narrativeText);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        return new IncidentNarrative
        {
            Id = id,
            IncidentId = incidentId,
            NarrativeText = narrativeText,
            SymptomsSection = symptomsSection,
            TimelineSection = timelineSection,
            ProbableCauseSection = probableCauseSection,
            MitigationSection = mitigationSection,
            RelatedChangesSection = relatedChangesSection,
            AffectedServicesSection = affectedServicesSection,
            ModelUsed = modelUsed,
            TokensUsed = tokensUsed,
            Status = status,
            TenantId = tenantId,
            GeneratedAt = generatedAt,
            RefreshCount = 0,
        };
    }

    /// <summary>
    /// Marca a narrativa como desatualizada (Stale) quando os dados do incidente mudam.
    /// </summary>
    public void MarkAsStale()
    {
        Status = NarrativeStatus.Stale;
    }

    /// <summary>
    /// Regenera a narrativa com novos dados, incrementa o contador de refresh
    /// e atualiza o timestamp de última regeneração.
    /// </summary>
    public void Refresh(
        string narrativeText,
        string? symptomsSection,
        string? timelineSection,
        string? probableCauseSection,
        string? mitigationSection,
        string? relatedChangesSection,
        string? affectedServicesSection,
        string modelUsed,
        int tokensUsed,
        DateTimeOffset refreshedAt)
    {
        Guard.Against.NullOrWhiteSpace(narrativeText);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        NarrativeText = narrativeText;
        SymptomsSection = symptomsSection;
        TimelineSection = timelineSection;
        ProbableCauseSection = probableCauseSection;
        MitigationSection = mitigationSection;
        RelatedChangesSection = relatedChangesSection;
        AffectedServicesSection = affectedServicesSection;
        ModelUsed = modelUsed;
        TokensUsed = tokensUsed;
        Status = NarrativeStatus.Draft;
        LastRefreshedAt = refreshedAt;
        RefreshCount++;
    }
}

/// <summary>Identificador fortemente tipado de IncidentNarrative.</summary>
public sealed record IncidentNarrativeId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static IncidentNarrativeId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static IncidentNarrativeId From(Guid id) => new(id);
}
