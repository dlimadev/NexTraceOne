using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Política de aprovação configurável por ambiente e serviço.
///
/// Define as regras que governam o fluxo de aprovação de uma release antes da
/// promoção para um ambiente específico. Vive no banco (não em appsettings.json)
/// para permitir alteração sem redeploy.
/// </summary>
public sealed class ReleaseApprovalPolicy : Entity<ReleaseApprovalPolicyId>
{
    private ReleaseApprovalPolicy() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome descritivo da política.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Identificador do ambiente ao qual se aplica (null = todos os ambientes).</summary>
    public string? EnvironmentId { get; private set; }

    /// <summary>Identificador do serviço ao qual se aplica (null = todos os serviços).</summary>
    public Guid? ServiceId { get; private set; }

    /// <summary>Tag de serviço alternativa ao ServiceId (suporta grupos de serviços).</summary>
    public string? ServiceTag { get; private set; }

    /// <summary>Indica se este ambiente/serviço exige aprovação.</summary>
    public bool RequiresApproval { get; private set; } = true;

    /// <summary>Tipo de aprovação: Manual | ExternalWebhook | ExternalServiceNow | AutoApprove.</summary>
    public string ApprovalType { get; private set; } = "Manual";

    /// <summary>URL do webhook externo (obrigatório quando ApprovalType = ExternalWebhook).</summary>
    public string? ExternalWebhookUrl { get; private set; }

    /// <summary>Número mínimo de aprovadores internos (usado quando ApprovalType = Manual).</summary>
    public int MinApprovers { get; private set; } = 1;

    /// <summary>Grupos autorizados a aprovar (JSON array de group IDs).</summary>
    public string ApproverGroupsJson { get; private set; } = "[]";

    /// <summary>Roles que podem fazer bypass desta política (JSON array).</summary>
    public string BypassRolesJson { get; private set; } = "[]";

    /// <summary>Horas até o token de callback expirar.</summary>
    public int ExpirationHours { get; private set; } = 48;

    /// <summary>Se true, a promoção só avança após Evidence Pack estar completo.</summary>
    public bool RequireEvidencePack { get; private set; }

    /// <summary>Se true, o checklist da release deve estar 100% completo antes da aprovação.</summary>
    public bool RequireChecklistCompletion { get; private set; }

    /// <summary>
    /// Risk score mínimo acima do qual a aprovação manual torna-se obrigatória,
    /// independentemente do ApprovalType configurado.
    /// </summary>
    public int? MinRiskScoreForManualApproval { get; private set; }

    /// <summary>Janelas de tempo bloqueadas para promoção (JSON array de {start, end} UTC).</summary>
    public string BlockedTimeWindowsJson { get; private set; } = "[]";

    /// <summary>Indica se a política está activa.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Ordem de precedência (menor = maior prioridade) quando múltiplas políticas se aplicam.</summary>
    public int Priority { get; private set; } = 100;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Utilizador que criou a política.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da última actualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Utilizador que fez a última actualização.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>Cria uma nova ReleaseApprovalPolicy.</summary>
    public static ReleaseApprovalPolicy Create(
        Guid tenantId,
        string name,
        string approvalType,
        string createdBy,
        DateTimeOffset createdAt,
        string? environmentId = null,
        Guid? serviceId = null,
        string? serviceTag = null,
        string? externalWebhookUrl = null,
        int minApprovers = 1,
        int expirationHours = 48,
        bool requireEvidencePack = false,
        bool requireChecklistCompletion = false,
        int? minRiskScoreForManualApproval = null,
        int priority = 100)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(approvalType);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new ReleaseApprovalPolicy
        {
            Id = ReleaseApprovalPolicyId.New(),
            TenantId = tenantId,
            Name = name,
            ApprovalType = approvalType,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            EnvironmentId = environmentId,
            ServiceId = serviceId,
            ServiceTag = serviceTag,
            ExternalWebhookUrl = externalWebhookUrl,
            MinApprovers = minApprovers,
            ExpirationHours = expirationHours,
            RequireEvidencePack = requireEvidencePack,
            RequireChecklistCompletion = requireChecklistCompletion,
            MinRiskScoreForManualApproval = minRiskScoreForManualApproval,
            Priority = priority,
            IsActive = true,
        };
    }

    /// <summary>Actualiza os parâmetros da política.</summary>
    public void Update(
        string name,
        string approvalType,
        string updatedBy,
        DateTimeOffset updatedAt,
        string? environmentId = null,
        Guid? serviceId = null,
        string? serviceTag = null,
        string? externalWebhookUrl = null,
        int minApprovers = 1,
        int expirationHours = 48,
        bool requireEvidencePack = false,
        bool requireChecklistCompletion = false,
        int? minRiskScoreForManualApproval = null,
        int priority = 100)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(approvalType);

        Name = name;
        ApprovalType = approvalType;
        EnvironmentId = environmentId;
        ServiceId = serviceId;
        ServiceTag = serviceTag;
        ExternalWebhookUrl = externalWebhookUrl;
        MinApprovers = minApprovers;
        ExpirationHours = expirationHours;
        RequireEvidencePack = requireEvidencePack;
        RequireChecklistCompletion = requireChecklistCompletion;
        MinRiskScoreForManualApproval = minRiskScoreForManualApproval;
        Priority = priority;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    /// <summary>Desactiva a política.</summary>
    public void Deactivate(string deactivatedBy, DateTimeOffset deactivatedAt)
    {
        IsActive = false;
        UpdatedBy = deactivatedBy;
        UpdatedAt = deactivatedAt;
    }
}

/// <summary>Identificador fortemente tipado de ReleaseApprovalPolicy.</summary>
public sealed record ReleaseApprovalPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ReleaseApprovalPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ReleaseApprovalPolicyId From(Guid id) => new(id);
}
