using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa release notes geradas por IA para uma release.
/// Consolida sumário técnico, sumário executivo, novos endpoints, breaking changes,
/// serviços afetados, métricas de confiança e links de evidência.
/// Ligada a uma Release; suporta regeneração com rastreio de versões.
/// </summary>
public sealed class ReleaseNotes : AuditableEntity<ReleaseNotesId>
{
    private ReleaseNotes() { }

    /// <summary>Identificador da release associada.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Sumário técnico completo gerado por IA.</summary>
    public string TechnicalSummary { get; private set; } = string.Empty;

    /// <summary>Sumário executivo de alto nível (opcional, persona Executive).</summary>
    public string? ExecutiveSummary { get; private set; }

    /// <summary>Secção de novos endpoints/contratos.</summary>
    public string? NewEndpointsSection { get; private set; }

    /// <summary>Secção de breaking changes.</summary>
    public string? BreakingChangesSection { get; private set; }

    /// <summary>Secção de serviços afetados.</summary>
    public string? AffectedServicesSection { get; private set; }

    /// <summary>Secção de métricas de confiança.</summary>
    public string? ConfidenceMetricsSection { get; private set; }

    /// <summary>Secção de links para evidências.</summary>
    public string? EvidenceLinksSection { get; private set; }

    /// <summary>Modelo de IA utilizado para gerar as release notes.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Número de tokens consumidos na geração.</summary>
    public int TokensUsed { get; private set; }

    /// <summary>Estado atual das release notes (Draft, Published, Archived).</summary>
    public ReleaseNotesStatus Status { get; private set; }

    /// <summary>Identificador do tenant ao qual as release notes pertencem.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data/hora UTC da geração das release notes.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Data/hora UTC da última regeneração.</summary>
    public DateTimeOffset? LastRegeneratedAt { get; private set; }

    /// <summary>Número de vezes que as release notes foram regeneradas.</summary>
    public int RegenerationCount { get; private set; }

    /// <summary>
    /// Factory method para criação de ReleaseNotes com validações de guarda.
    /// </summary>
    public static ReleaseNotes Create(
        ReleaseNotesId id,
        ReleaseId releaseId,
        string technicalSummary,
        string? executiveSummary,
        string? newEndpointsSection,
        string? breakingChangesSection,
        string? affectedServicesSection,
        string? confidenceMetricsSection,
        string? evidenceLinksSection,
        string modelUsed,
        int tokensUsed,
        ReleaseNotesStatus status,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.NullOrWhiteSpace(technicalSummary);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        return new ReleaseNotes
        {
            Id = id,
            ReleaseId = releaseId,
            TechnicalSummary = technicalSummary,
            ExecutiveSummary = executiveSummary,
            NewEndpointsSection = newEndpointsSection,
            BreakingChangesSection = breakingChangesSection,
            AffectedServicesSection = affectedServicesSection,
            ConfidenceMetricsSection = confidenceMetricsSection,
            EvidenceLinksSection = evidenceLinksSection,
            ModelUsed = modelUsed,
            TokensUsed = tokensUsed,
            Status = status,
            TenantId = tenantId,
            GeneratedAt = generatedAt,
            RegenerationCount = 0,
        };
    }

    /// <summary>
    /// Publica as release notes, tornando-as visíveis para as personas.
    /// </summary>
    public void Publish()
    {
        Status = ReleaseNotesStatus.Published;
    }

    /// <summary>
    /// Arquiva as release notes (versão anterior substituída).
    /// </summary>
    public void Archive()
    {
        Status = ReleaseNotesStatus.Archived;
    }

    /// <summary>
    /// Regenera as release notes com novos dados, incrementa o contador
    /// de regeneração e atualiza o timestamp de última regeneração.
    /// </summary>
    public void Regenerate(
        string technicalSummary,
        string? executiveSummary,
        string? newEndpointsSection,
        string? breakingChangesSection,
        string? affectedServicesSection,
        string? confidenceMetricsSection,
        string? evidenceLinksSection,
        string modelUsed,
        int tokensUsed,
        DateTimeOffset regeneratedAt)
    {
        Guard.Against.NullOrWhiteSpace(technicalSummary);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Negative(tokensUsed);

        TechnicalSummary = technicalSummary;
        ExecutiveSummary = executiveSummary;
        NewEndpointsSection = newEndpointsSection;
        BreakingChangesSection = breakingChangesSection;
        AffectedServicesSection = affectedServicesSection;
        ConfidenceMetricsSection = confidenceMetricsSection;
        EvidenceLinksSection = evidenceLinksSection;
        ModelUsed = modelUsed;
        TokensUsed = tokensUsed;
        Status = ReleaseNotesStatus.Draft;
        LastRegeneratedAt = regeneratedAt;
        RegenerationCount++;
    }
}

/// <summary>Identificador fortemente tipado de ReleaseNotes.</summary>
public sealed record ReleaseNotesId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static ReleaseNotesId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ReleaseNotesId From(Guid id) => new(id);
}
