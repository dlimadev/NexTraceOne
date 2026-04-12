using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ServiceMaturityAssessment.
/// Garante que nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ServiceMaturityAssessmentId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Avaliação de maturidade de um serviço individual, baseada num modelo de 5 níveis.
/// Cada nível agrega critérios cumulativos que determinam a maturidade operacional do serviço.
/// A entidade suporta reavaliações ao longo do ciclo de vida do serviço.
/// </summary>
public sealed class ServiceMaturityAssessment : Entity<ServiceMaturityAssessmentId>
{
    /// <summary>Identificador do serviço avaliado (FK lógica).</summary>
    public Guid ServiceId { get; private init; }

    /// <summary>Nome desnormalizado do serviço para exibição sem join.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Nível de maturidade atual, derivado automaticamente dos critérios.</summary>
    public ServiceMaturityLevel CurrentLevel { get; private set; }

    // ── Critérios de Nível 1 (Basic) ──
    /// <summary>Ownership do serviço está definido.</summary>
    public bool OwnershipDefined { get; private set; }

    // ── Critérios de Nível 2 (Documented) ──
    /// <summary>Contratos do serviço estão publicados.</summary>
    public bool ContractsPublished { get; private set; }

    /// <summary>Documentação do serviço existe.</summary>
    public bool DocumentationExists { get; private set; }

    // ── Critérios de Nível 3 (Governed) ──
    /// <summary>Políticas de governança estão aplicadas ao serviço.</summary>
    public bool PoliciesApplied { get; private set; }

    /// <summary>Workflow de aprovação está activo para o serviço.</summary>
    public bool ApprovalWorkflowActive { get; private set; }

    // ── Critérios de Nível 4 (Observed) ──
    /// <summary>Telemetria (traces, métricas, logs) está activa.</summary>
    public bool TelemetryActive { get; private set; }

    /// <summary>Baselines de performance estão estabelecidos.</summary>
    public bool BaselinesEstablished { get; private set; }

    /// <summary>Alertas operacionais estão configurados.</summary>
    public bool AlertsConfigured { get; private set; }

    // ── Critérios de Nível 5 (Resilient) ──
    /// <summary>Runbooks operacionais estão disponíveis.</summary>
    public bool RunbooksAvailable { get; private set; }

    /// <summary>Procedimento de rollback foi testado.</summary>
    public bool RollbackTested { get; private set; }

    /// <summary>Validação de chaos engineering foi realizada.</summary>
    public bool ChaosValidated { get; private set; }

    /// <summary>Data/hora UTC da avaliação original.</summary>
    public DateTimeOffset AssessedAt { get; private init; }

    /// <summary>Quem realizou a avaliação ("auto" ou nome do utilizador).</summary>
    public string AssessedBy { get; private init; } = string.Empty;

    /// <summary>Identificador do tenant proprietário (nullable para multi-tenant).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Data/hora UTC da última reavaliação.</summary>
    public DateTimeOffset? LastReassessedAt { get; private set; }

    /// <summary>Número de vezes que a avaliação foi reavaliada.</summary>
    public int ReassessmentCount { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ServiceMaturityAssessment() { }

    /// <summary>
    /// Cria uma nova avaliação de maturidade para um serviço.
    /// O nível é derivado automaticamente a partir dos critérios fornecidos.
    /// </summary>
    /// <param name="serviceId">Identificador do serviço avaliado.</param>
    /// <param name="serviceName">Nome do serviço (máx. 200 caracteres).</param>
    /// <param name="ownershipDefined">Critério: ownership definido.</param>
    /// <param name="contractsPublished">Critério: contratos publicados.</param>
    /// <param name="documentationExists">Critério: documentação existente.</param>
    /// <param name="policiesApplied">Critério: políticas aplicadas.</param>
    /// <param name="approvalWorkflowActive">Critério: workflow de aprovação activo.</param>
    /// <param name="telemetryActive">Critério: telemetria activa.</param>
    /// <param name="baselinesEstablished">Critério: baselines estabelecidos.</param>
    /// <param name="alertsConfigured">Critério: alertas configurados.</param>
    /// <param name="runbooksAvailable">Critério: runbooks disponíveis.</param>
    /// <param name="rollbackTested">Critério: rollback testado.</param>
    /// <param name="chaosValidated">Critério: chaos engineering validado.</param>
    /// <param name="assessedBy">Quem realizou a avaliação.</param>
    /// <param name="tenantId">Identificador do tenant (opcional).</param>
    /// <param name="now">Data/hora UTC da avaliação.</param>
    /// <returns>Nova instância válida de ServiceMaturityAssessment.</returns>
    public static ServiceMaturityAssessment Assess(
        Guid serviceId,
        string serviceName,
        bool ownershipDefined,
        bool contractsPublished,
        bool documentationExists,
        bool policiesApplied,
        bool approvalWorkflowActive,
        bool telemetryActive,
        bool baselinesEstablished,
        bool alertsConfigured,
        bool runbooksAvailable,
        bool rollbackTested,
        bool chaosValidated,
        string assessedBy,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.Default(serviceId, nameof(serviceId));
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.StringTooLong(serviceName, 200, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(assessedBy, nameof(assessedBy));
        Guard.Against.StringTooLong(assessedBy, 200, nameof(assessedBy));

        var assessment = new ServiceMaturityAssessment
        {
            Id = new ServiceMaturityAssessmentId(Guid.NewGuid()),
            ServiceId = serviceId,
            ServiceName = serviceName.Trim(),
            OwnershipDefined = ownershipDefined,
            ContractsPublished = contractsPublished,
            DocumentationExists = documentationExists,
            PoliciesApplied = policiesApplied,
            ApprovalWorkflowActive = approvalWorkflowActive,
            TelemetryActive = telemetryActive,
            BaselinesEstablished = baselinesEstablished,
            AlertsConfigured = alertsConfigured,
            RunbooksAvailable = runbooksAvailable,
            RollbackTested = rollbackTested,
            ChaosValidated = chaosValidated,
            AssessedBy = assessedBy.Trim(),
            TenantId = tenantId?.Trim(),
            AssessedAt = now,
            LastReassessedAt = null,
            ReassessmentCount = 0
        };

        assessment.CurrentLevel = assessment.DeriveLevel();

        return assessment;
    }

    /// <summary>
    /// Reavalia a maturidade do serviço com novos critérios.
    /// Incrementa o contador de reavaliações e atualiza a data.
    /// </summary>
    public void Reassess(
        bool ownershipDefined,
        bool contractsPublished,
        bool documentationExists,
        bool policiesApplied,
        bool approvalWorkflowActive,
        bool telemetryActive,
        bool baselinesEstablished,
        bool alertsConfigured,
        bool runbooksAvailable,
        bool rollbackTested,
        bool chaosValidated,
        DateTimeOffset now)
    {
        OwnershipDefined = ownershipDefined;
        ContractsPublished = contractsPublished;
        DocumentationExists = documentationExists;
        PoliciesApplied = policiesApplied;
        ApprovalWorkflowActive = approvalWorkflowActive;
        TelemetryActive = telemetryActive;
        BaselinesEstablished = baselinesEstablished;
        AlertsConfigured = alertsConfigured;
        RunbooksAvailable = runbooksAvailable;
        RollbackTested = rollbackTested;
        ChaosValidated = chaosValidated;

        CurrentLevel = DeriveLevel();
        LastReassessedAt = now;
        ReassessmentCount++;
    }

    /// <summary>
    /// Deriva o nível de maturidade a partir dos critérios cumulativos.
    /// Level 5 (Resilient): todos os critérios.
    /// Level 4 (Observed): critérios até telemetria/baselines/alertas.
    /// Level 3 (Governed): critérios até políticas/approval.
    /// Level 2 (Documented): ownership + contratos + documentação.
    /// Level 1 (Basic): ownership definido ou fallback mínimo.
    /// </summary>
    private ServiceMaturityLevel DeriveLevel()
    {
        var hasLevel1 = OwnershipDefined;
        var hasLevel2 = hasLevel1 && ContractsPublished && DocumentationExists;
        var hasLevel3 = hasLevel2 && PoliciesApplied && ApprovalWorkflowActive;
        var hasLevel4 = hasLevel3 && TelemetryActive && BaselinesEstablished && AlertsConfigured;
        var hasLevel5 = hasLevel4 && RunbooksAvailable && RollbackTested && ChaosValidated;

        if (hasLevel5) return ServiceMaturityLevel.Resilient;
        if (hasLevel4) return ServiceMaturityLevel.Observed;
        if (hasLevel3) return ServiceMaturityLevel.Governed;
        if (hasLevel2) return ServiceMaturityLevel.Documented;

        return ServiceMaturityLevel.Basic;
    }
}
