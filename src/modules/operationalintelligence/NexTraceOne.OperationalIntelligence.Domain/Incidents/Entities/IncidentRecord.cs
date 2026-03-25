using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Aggregate root que representa um incidente operacional registado na plataforma.
/// Armazena toda a informação consolidada de identidade, correlação, evidências,
/// mitigação e contratos impactados. Coleções complexas são persistidas como JSON.
/// </summary>
public sealed class IncidentRecord : AuditableEntity<IncidentRecordId>
{
    private IncidentRecord() { }

    /// <summary>Referência externa legível (ex: INC-2026-0042).</summary>
    public string ExternalRef { get; private set; } = string.Empty;

    /// <summary>Título resumido do incidente.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do incidente.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo de incidente (ServiceDegradation, DependencyFailure, etc.).</summary>
    public IncidentType Type { get; private set; }

    /// <summary>Severidade do incidente.</summary>
    public IncidentSeverity Severity { get; private set; }

    /// <summary>Status atual do ciclo de vida do incidente.</summary>
    public IncidentStatus Status { get; private set; }

    /// <summary>Identificador do serviço afetado.</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do serviço afetado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Equipa responsável pelo serviço.</summary>
    public string OwnerTeam { get; private set; } = string.Empty;

    /// <summary>Domínio de negócio impactado.</summary>
    public string? ImpactedDomain { get; private set; }

    /// <summary>Ambiente onde o incidente ocorreu (Production, Staging, etc.).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de detecção do incidente.</summary>
    public DateTimeOffset DetectedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização de estado.</summary>
    public DateTimeOffset LastUpdatedAt { get; private set; }

    /// <summary>Indica se existe correlação com mudanças recentes.</summary>
    public bool HasCorrelation { get; private set; }

    /// <summary>Nível de confiança da correlação.</summary>
    public CorrelationConfidence CorrelationConfidence { get; private set; }

    /// <summary>Status resumido da mitigação.</summary>
    public MitigationStatus MitigationStatus { get; private set; }

    // ── Correlação ──────────────────────────────────────────────────────

    /// <summary>Análise textual da correlação.</summary>
    public string? CorrelationAnalysis { get; private set; }

    // ── Evidência ───────────────────────────────────────────────────────

    /// <summary>Resumo dos sinais de telemetria.</summary>
    public string? EvidenceTelemetrySummary { get; private set; }

    /// <summary>Resumo do impacto de negócio.</summary>
    public string? EvidenceBusinessImpact { get; private set; }

    /// <summary>Análise de anomalias das evidências.</summary>
    public string? EvidenceAnalysis { get; private set; }

    /// <summary>Contexto temporal das evidências (notas/observação).</summary>
    public string? EvidenceTemporalContext { get; private set; }

    // ── Mitigação ───────────────────────────────────────────────────────

    /// <summary>Narrativa/orientação de rollback.</summary>
    public string? MitigationNarrative { get; private set; }

    /// <summary>Indica se existe caminho de escalação definido.</summary>
    public bool HasEscalationPath { get; private set; }

    /// <summary>Orientação de escalação.</summary>
    public string? EscalationPath { get; private set; }

    // ── JSON-serialized collections ─────────────────────────────────────

    /// <summary>Timeline do incidente (JSON).</summary>
    public string? TimelineJson { get; private set; }

    /// <summary>Serviços linkados ao incidente (JSON).</summary>
    public string? LinkedServicesJson { get; private set; }

    /// <summary>Mudanças correlacionadas (JSON).</summary>
    public string? CorrelatedChangesJson { get; private set; }

    /// <summary>Serviços correlacionados (JSON).</summary>
    public string? CorrelatedServicesJson { get; private set; }

    /// <summary>Dependências correlacionadas (JSON).</summary>
    public string? CorrelatedDependenciesJson { get; private set; }

    /// <summary>Contratos impactados (JSON).</summary>
    public string? ImpactedContractsJson { get; private set; }

    /// <summary>Observações de evidência (JSON).</summary>
    public string? EvidenceObservationsJson { get; private set; }

    /// <summary>Contratos relacionados no detalhe (JSON).</summary>
    public string? RelatedContractsJson { get; private set; }

    /// <summary>Links de runbook associados (JSON).</summary>
    public string? RunbookLinksJson { get; private set; }

    /// <summary>Ações de mitigação sugeridas (JSON).</summary>
    public string? MitigationActionsJson { get; private set; }

    /// <summary>Recomendações de mitigação (JSON).</summary>
    public string? MitigationRecommendationsJson { get; private set; }

    /// <summary>Runbooks recomendados para mitigação (JSON).</summary>
    public string? MitigationRecommendedRunbooksJson { get; private set; }

    // ── Concorrência otimista ───────────────────────────────────────────

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    // ── Fase 4: Contexto de Tenant/Ambiente ────────────────────────────────

    /// <summary>
    /// Identificador do tenant ao qual o incidente pertence.
    /// Nullable por retrocompatibilidade — incidentes criados antes da Fase 4
    /// não possuem este campo preenchido.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Identificador do ambiente (infra) onde o incidente ocorreu.
    /// Nullable por retrocompatibilidade — complementa o campo Environment (string).
    /// </summary>
    public Guid? EnvironmentId { get; private set; }

    /// <summary>
    /// Factory method para criação de um IncidentRecord com validações de guarda.
    /// </summary>
    public static IncidentRecord Create(
        IncidentRecordId id,
        string externalRef,
        string title,
        string description,
        IncidentType type,
        IncidentSeverity severity,
        IncidentStatus status,
        string serviceId,
        string serviceName,
        string ownerTeam,
        string? impactedDomain,
        string environment,
        DateTimeOffset detectedAt,
        DateTimeOffset lastUpdatedAt,
        bool hasCorrelation,
        CorrelationConfidence correlationConfidence,
        MitigationStatus mitigationStatus)
    {
        Guard.Against.NullOrWhiteSpace(externalRef);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(ownerTeam);
        Guard.Against.NullOrWhiteSpace(environment);

        return new IncidentRecord
        {
            Id = id,
            ExternalRef = externalRef,
            Title = title,
            Description = description,
            Type = type,
            Severity = severity,
            Status = status,
            ServiceId = serviceId,
            ServiceName = serviceName,
            OwnerTeam = ownerTeam,
            ImpactedDomain = impactedDomain,
            Environment = environment,
            DetectedAt = detectedAt,
            LastUpdatedAt = lastUpdatedAt,
            HasCorrelation = hasCorrelation,
            CorrelationConfidence = correlationConfidence,
            MitigationStatus = mitigationStatus,
        };
    }

    /// <summary>Define campos de correlação.</summary>
    public void SetCorrelation(string? analysis, string? correlatedChangesJson, string? correlatedServicesJson,
        string? correlatedDependenciesJson, string? impactedContractsJson)
    {
        CorrelationAnalysis = analysis;
        CorrelatedChangesJson = correlatedChangesJson;
        CorrelatedServicesJson = correlatedServicesJson;
        CorrelatedDependenciesJson = correlatedDependenciesJson;
        ImpactedContractsJson = impactedContractsJson;
    }

    /// <summary>
    /// Atualiza estado agregado de correlação (flags + confiança + last update).
    /// </summary>
    public void UpdateCorrelationAssessment(
        bool hasCorrelation,
        CorrelationConfidence confidence,
        DateTimeOffset updatedAtUtc)
    {
        HasCorrelation = hasCorrelation;
        CorrelationConfidence = confidence;
        LastUpdatedAt = updatedAtUtc;
    }

    /// <summary>Define campos de evidência.</summary>
    public void SetEvidence(string? telemetrySummary, string? businessImpact,
        string? evidenceObservationsJson, string? analysis, string? temporalContext)
    {
        EvidenceTelemetrySummary = telemetrySummary;
        EvidenceBusinessImpact = businessImpact;
        EvidenceObservationsJson = evidenceObservationsJson;
        EvidenceAnalysis = analysis;
        EvidenceTemporalContext = temporalContext;
    }

    /// <summary>Define campos de mitigação.</summary>
    public void SetMitigation(string? mitigationActionsJson, string? mitigationRecommendedRunbooksJson,
        string? narrative, bool hasEscalationPath, string? escalationPath)
    {
        MitigationActionsJson = mitigationActionsJson;
        MitigationRecommendedRunbooksJson = mitigationRecommendedRunbooksJson;
        MitigationNarrative = narrative;
        HasEscalationPath = hasEscalationPath;
        EscalationPath = escalationPath;
    }

    /// <summary>Define JSON das recomendações de mitigação.</summary>
    public void SetMitigationRecommendations(string? recommendationsJson)
    {
        MitigationRecommendationsJson = recommendationsJson;
    }

    /// <summary>Define campos do detalhe (timeline, linked services, related contracts, runbooks).</summary>
    public void SetDetail(string? timelineJson, string? linkedServicesJson,
        string? relatedContractsJson, string? runbookLinksJson)
    {
        TimelineJson = timelineJson;
        LinkedServicesJson = linkedServicesJson;
        RelatedContractsJson = relatedContractsJson;
        RunbookLinksJson = runbookLinksJson;
    }

    /// <summary>
    /// Define o contexto de tenant e ambiente do incidente.
    /// Operação idempotente — apenas atribui se o valor atual for null (??= semantics).
    /// Garante que o primeiro contexto definido não seja sobrescrito.
    /// </summary>
    public void SetTenantContext(Guid? tenantId, Guid? environmentId)
    {
        TenantId ??= tenantId;
        EnvironmentId ??= environmentId;
    }
}

/// <summary>Identificador fortemente tipado de IncidentRecord.</summary>
public sealed record IncidentRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static IncidentRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static IncidentRecordId From(Guid id) => new(id);
}
