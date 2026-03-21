using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade base que representa uma política associada a um ambiente específico de um tenant.
///
/// EnvironmentPolicy é a estrutura conceitual de Fase 1 para vincular regras operacionais
/// a ambientes parametrizáveis. Em fases futuras, será especializada em:
/// - Políticas de aprovação de mudança por ambiente
/// - Políticas de janela de congelamento
/// - Políticas de alertas e escalação
/// - Políticas de promoção entre ambientes
///
/// Fase 2: migração AddEnvironmentPolicies — esta entidade ainda não tem mapeamento EF.
/// </summary>
public sealed class EnvironmentPolicy : Entity<EnvironmentPolicyId>
{
    private EnvironmentPolicy() { }

    /// <summary>Tenant proprietário desta política.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Ambiente ao qual a política se aplica.</summary>
    public EnvironmentId EnvironmentId { get; private set; } = null!;

    /// <summary>
    /// Tipo da política.
    /// Permite distinguir entre políticas de promoção, janela, alertas, etc.
    /// </summary>
    public string PolicyType { get; private set; } = string.Empty;

    /// <summary>Nome legível da política.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Configuração JSON da política.
    /// Estrutura dependente do PolicyType — será tipada nas fases futuras.
    /// </summary>
    public string ConfigurationJson { get; private set; } = "{}";

    /// <summary>Indica se esta política está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de uma EnvironmentPolicy.
    /// </summary>
    public static EnvironmentPolicy Create(
        TenantId tenantId,
        EnvironmentId environmentId,
        string policyType,
        string name,
        DateTimeOffset now,
        string configurationJson = "{}")
    {
        Guard.Against.Null(tenantId);
        Guard.Against.Null(environmentId);
        Guard.Against.NullOrWhiteSpace(policyType, message: "Policy type is required.");
        Guard.Against.NullOrWhiteSpace(name, message: "Policy name is required.");

        return new EnvironmentPolicy
        {
            Id = EnvironmentPolicyId.New(),
            TenantId = tenantId,
            EnvironmentId = environmentId,
            PolicyType = policyType,
            Name = name,
            ConfigurationJson = configurationJson,
            IsActive = true,
            CreatedAt = now
        };
    }

    /// <summary>Desativa a política.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>Atualiza a configuração da política.</summary>
    public void UpdateConfiguration(string configurationJson, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(configurationJson, message: "Configuration JSON is required.");
        ConfigurationJson = configurationJson;
        UpdatedAt = now;
    }

    /// <summary>Tipos de política conhecidos — serão expandidos nas fases futuras.</summary>
    public static class KnownPolicyTypes
    {
        /// <summary>Política de aprovação para promoção entre ambientes.</summary>
        public const string PromotionApproval = "promotion_approval";

        /// <summary>Política de janela de congelamento de mudanças.</summary>
        public const string FreezeWindow = "freeze_window";

        /// <summary>Política de alertas e escalação operacional.</summary>
        public const string AlertEscalation = "alert_escalation";

        /// <summary>Política de qualidade mínima para deploy.</summary>
        public const string DeployQualityGate = "deploy_quality_gate";
    }
}

/// <summary>Identificador fortemente tipado de EnvironmentPolicy.</summary>
public sealed record EnvironmentPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentPolicyId From(Guid id) => new(id);
}
