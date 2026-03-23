using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa o vínculo entre uma integração externa e um ambiente específico de um tenant.
///
/// Cada tenant pode ter integrações (ex.: DataDog, PagerDuty, Kafka, SonarQube)
/// configuradas de forma diferente por ambiente. Um conector de observabilidade
/// pode apontar para diferentes endpoints em DEV vs PROD.
///
/// Fase 2: migração AddEnvironmentIntegrationBindings — ainda sem mapeamento EF na Fase 1.
/// </summary>
public sealed class EnvironmentIntegrationBinding : Entity<EnvironmentIntegrationBindingId>
{
    private EnvironmentIntegrationBinding() { }

    /// <summary>Tenant proprietário do vínculo.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Ambiente ao qual a integração está vinculada.</summary>
    public EnvironmentId EnvironmentId { get; private set; } = null!;

    /// <summary>
    /// Tipo de integração (ex.: "observability", "alerting", "ci_cd", "event_broker").
    /// </summary>
    public string IntegrationType { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador do conector de integração (referência ao módulo de Governance/Integrations).
    /// </summary>
    public Guid ConnectorId { get; private set; }

    /// <summary>
    /// Configuração específica do ambiente para este conector (ex.: endpoint URL, credenciais).
    /// Armazenado como JSON — estrutura depende do IntegrationType.
    /// Encriptado em repouso via AES-256-GCM (EncryptedStringConverter).
    /// </summary>
    [EncryptedField]
    public string BindingConfigJson { get; private set; } = "{}";

    /// <summary>Indica se o vínculo está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de um vínculo de integração por ambiente.
    /// </summary>
    public static EnvironmentIntegrationBinding Create(
        TenantId tenantId,
        EnvironmentId environmentId,
        string integrationType,
        Guid connectorId,
        DateTimeOffset now,
        string bindingConfigJson = "{}")
    {
        Guard.Against.Null(tenantId);
        Guard.Against.Null(environmentId);
        Guard.Against.NullOrWhiteSpace(integrationType, message: "Integration type is required.");
        Guard.Against.Default(connectorId, message: "Connector ID is required.");

        return new EnvironmentIntegrationBinding
        {
            Id = EnvironmentIntegrationBindingId.New(),
            TenantId = tenantId,
            EnvironmentId = environmentId,
            IntegrationType = integrationType,
            ConnectorId = connectorId,
            BindingConfigJson = bindingConfigJson,
            IsActive = true,
            CreatedAt = now
        };
    }

    /// <summary>Desativa o vínculo de integração.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Atualiza a configuração do vínculo.</summary>
    public void UpdateConfig(string bindingConfigJson)
    {
        Guard.Against.NullOrWhiteSpace(bindingConfigJson, message: "Binding config JSON is required.");
        BindingConfigJson = bindingConfigJson;
    }

    /// <summary>Tipos de integração reconhecidos na plataforma.</summary>
    public static class KnownIntegrationTypes
    {
        public const string Observability = "observability";
        public const string Alerting = "alerting";
        public const string CiCd = "ci_cd";
        public const string EventBroker = "event_broker";
        public const string IncidentManagement = "incident_management";
        public const string CodeQuality = "code_quality";
        public const string FeatureFlags = "feature_flags";
    }
}

/// <summary>Identificador fortemente tipado de EnvironmentIntegrationBinding.</summary>
public sealed record EnvironmentIntegrationBindingId(Guid Value) : TypedIdBase(Value)
{
    public static EnvironmentIntegrationBindingId New() => new(Guid.NewGuid());
    public static EnvironmentIntegrationBindingId From(Guid id) => new(id);
}
