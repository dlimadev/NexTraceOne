using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Política de compliance contratual configurável por âmbito (organização, equipa,
/// ambiente ou serviço). Define regras de verificação, ações automáticas, geração
/// de changelog, CDCT, deteção de drift e notificações. Permite governança
/// granular e adaptável ao contexto de cada tenant.
/// </summary>
public sealed class ContractCompliancePolicy : AuditableEntity<ContractCompliancePolicyId>
{
    private ContractCompliancePolicy() { }

    /// <summary>Identificador do tenant para isolamento multi-tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome descritivo da política.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada da política e do seu objetivo.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Âmbito de aplicação da política.</summary>
    public PolicyScope Scope { get; private set; }

    /// <summary>Identificador do âmbito (nulo para âmbito Organization).</summary>
    public string? ScopeId { get; private set; }

    /// <summary>Indica se a política está ativa e deve ser avaliada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Modo de verificação configurado.</summary>
    public VerificationMode VerificationMode { get; private set; }

    /// <summary>Abordagem de verificação configurada.</summary>
    public VerificationApproach VerificationApproach { get; private set; }

    /// <summary>Ação a executar quando uma breaking change é detetada.</summary>
    public ComplianceAction OnBreakingChange { get; private set; }

    /// <summary>Ação a executar quando uma alteração não disruptiva é detetada.</summary>
    public ComplianceAction OnNonBreakingChange { get; private set; }

    /// <summary>Ação a executar quando um novo endpoint é detetado.</summary>
    public ComplianceAction OnNewEndpoint { get; private set; }

    /// <summary>Ação a executar quando um endpoint removido é detetado.</summary>
    public ComplianceAction OnRemovedEndpoint { get; private set; }

    /// <summary>Ação a executar quando não existe contrato registado.</summary>
    public ComplianceAction OnMissingContract { get; private set; }

    /// <summary>Ação a executar quando o contrato não foi aprovado.</summary>
    public ComplianceAction OnContractNotApproved { get; private set; }

    /// <summary>Indica se o changelog deve ser gerado automaticamente.</summary>
    public bool AutoGenerateChangelog { get; private set; }

    /// <summary>Formato do changelog (mapeado para enum ChangelogFormat, armazenado como int).</summary>
    public int ChangelogFormat { get; private set; }

    /// <summary>Indica se o changelog requer aprovação formal antes de publicação.</summary>
    public bool RequireChangelogApproval { get; private set; }

    /// <summary>Indica se o CDCT (Consumer-Driven Contract Testing) está ativo.</summary>
    public bool EnforceCdct { get; private set; }

    /// <summary>Ação a executar quando o CDCT falha.</summary>
    public ComplianceAction CdctFailureAction { get; private set; }

    /// <summary>Indica se a deteção de drift em runtime está ativa.</summary>
    public bool EnableRuntimeDriftDetection { get; private set; }

    /// <summary>Intervalo em minutos entre execuções de deteção de drift.</summary>
    public int DriftDetectionIntervalMinutes { get; private set; }

    /// <summary>Limiar de drift para gerar alerta.</summary>
    public decimal DriftThresholdForAlert { get; private set; }

    /// <summary>Limiar de drift para criar incidente.</summary>
    public decimal DriftThresholdForIncident { get; private set; }

    /// <summary>Indica se deve notificar quando uma verificação falha.</summary>
    public bool NotifyOnVerificationFailure { get; private set; }

    /// <summary>Indica se deve notificar quando uma breaking change é detetada.</summary>
    public bool NotifyOnBreakingChange { get; private set; }

    /// <summary>Indica se deve notificar quando drift é detetado.</summary>
    public bool NotifyOnDriftDetected { get; private set; }

    /// <summary>Canais de notificação em formato JSON (JSONB array de nomes de canal).</summary>
    public string NotificationChannels { get; private set; } = "[]";

    /// <summary>
    /// Cria uma nova política de compliance contratual com estado ativo.
    /// Valida os campos obrigatórios e aplica valores por omissão seguros.
    /// </summary>
    public static ContractCompliancePolicy Create(
        string tenantId,
        string name,
        string description,
        PolicyScope scope,
        string? scopeId,
        VerificationMode verificationMode,
        VerificationApproach verificationApproach,
        ComplianceAction onBreakingChange,
        ComplianceAction onNonBreakingChange,
        ComplianceAction onNewEndpoint,
        ComplianceAction onRemovedEndpoint,
        ComplianceAction onMissingContract,
        ComplianceAction onContractNotApproved,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 200);
        Guard.Against.EnumOutOfRange(scope);
        Guard.Against.EnumOutOfRange(verificationMode);
        Guard.Against.EnumOutOfRange(verificationApproach);
        Guard.Against.EnumOutOfRange(onBreakingChange);
        Guard.Against.EnumOutOfRange(onNonBreakingChange);
        Guard.Against.EnumOutOfRange(onNewEndpoint);
        Guard.Against.EnumOutOfRange(onRemovedEndpoint);
        Guard.Against.EnumOutOfRange(onMissingContract);
        Guard.Against.EnumOutOfRange(onContractNotApproved);

        if (description is not null)
            Guard.Against.StringTooLong(description, 2000);

        if (scopeId is not null)
            Guard.Against.StringTooLong(scopeId, 200);

        var policy = new ContractCompliancePolicy
        {
            Id = new ContractCompliancePolicyId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            Description = description ?? string.Empty,
            Scope = scope,
            ScopeId = scopeId,
            IsActive = true,
            VerificationMode = verificationMode,
            VerificationApproach = verificationApproach,
            OnBreakingChange = onBreakingChange,
            OnNonBreakingChange = onNonBreakingChange,
            OnNewEndpoint = onNewEndpoint,
            OnRemovedEndpoint = onRemovedEndpoint,
            OnMissingContract = onMissingContract,
            OnContractNotApproved = onContractNotApproved,
            AutoGenerateChangelog = true,
            ChangelogFormat = 0,
            RequireChangelogApproval = false,
            EnforceCdct = false,
            CdctFailureAction = ComplianceAction.Warn,
            EnableRuntimeDriftDetection = false,
            DriftDetectionIntervalMinutes = 60,
            DriftThresholdForAlert = 0.1m,
            DriftThresholdForIncident = 0.3m,
            NotifyOnVerificationFailure = true,
            NotifyOnBreakingChange = true,
            NotifyOnDriftDetected = false,
            NotificationChannels = "[]",
        };
        policy.SetCreated(createdAt, string.Empty);
        policy.SetUpdated(createdAt, string.Empty);
        return policy;
    }

    /// <summary>
    /// Ativa a política de compliance. Políticas ativas são avaliadas automaticamente.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Desativa a política de compliance. Políticas inativas não são avaliadas.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Atualiza todos os campos configuráveis da política.
    /// </summary>
    public void Update(
        string name,
        string description,
        PolicyScope scope,
        string? scopeId,
        VerificationMode verificationMode,
        VerificationApproach verificationApproach,
        ComplianceAction onBreakingChange,
        ComplianceAction onNonBreakingChange,
        ComplianceAction onNewEndpoint,
        ComplianceAction onRemovedEndpoint,
        ComplianceAction onMissingContract,
        ComplianceAction onContractNotApproved,
        bool autoGenerateChangelog,
        int changelogFormat,
        bool requireChangelogApproval,
        bool enforceCdct,
        ComplianceAction cdctFailureAction,
        bool enableRuntimeDriftDetection,
        int driftDetectionIntervalMinutes,
        decimal driftThresholdForAlert,
        decimal driftThresholdForIncident,
        bool notifyOnVerificationFailure,
        bool notifyOnBreakingChange,
        bool notifyOnDriftDetected,
        string notificationChannels)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 200);
        Guard.Against.EnumOutOfRange(scope);
        Guard.Against.EnumOutOfRange(verificationMode);
        Guard.Against.EnumOutOfRange(verificationApproach);
        Guard.Against.EnumOutOfRange(onBreakingChange);
        Guard.Against.EnumOutOfRange(onNonBreakingChange);
        Guard.Against.EnumOutOfRange(onNewEndpoint);
        Guard.Against.EnumOutOfRange(onRemovedEndpoint);
        Guard.Against.EnumOutOfRange(onMissingContract);
        Guard.Against.EnumOutOfRange(onContractNotApproved);
        Guard.Against.EnumOutOfRange(cdctFailureAction);
        Guard.Against.NegativeOrZero(driftDetectionIntervalMinutes);

        if (description is not null)
            Guard.Against.StringTooLong(description, 2000);

        if (scopeId is not null)
            Guard.Against.StringTooLong(scopeId, 200);

        Name = name;
        Description = description ?? string.Empty;
        Scope = scope;
        ScopeId = scopeId;
        VerificationMode = verificationMode;
        VerificationApproach = verificationApproach;
        OnBreakingChange = onBreakingChange;
        OnNonBreakingChange = onNonBreakingChange;
        OnNewEndpoint = onNewEndpoint;
        OnRemovedEndpoint = onRemovedEndpoint;
        OnMissingContract = onMissingContract;
        OnContractNotApproved = onContractNotApproved;
        AutoGenerateChangelog = autoGenerateChangelog;
        ChangelogFormat = changelogFormat;
        RequireChangelogApproval = requireChangelogApproval;
        EnforceCdct = enforceCdct;
        CdctFailureAction = cdctFailureAction;
        EnableRuntimeDriftDetection = enableRuntimeDriftDetection;
        DriftDetectionIntervalMinutes = driftDetectionIntervalMinutes;
        DriftThresholdForAlert = driftThresholdForAlert;
        DriftThresholdForIncident = driftThresholdForIncident;
        NotifyOnVerificationFailure = notifyOnVerificationFailure;
        NotifyOnBreakingChange = notifyOnBreakingChange;
        NotifyOnDriftDetected = notifyOnDriftDetected;
        NotificationChannels = notificationChannels;
    }
}
