using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para PolicyAsCodeDefinition.
/// </summary>
public sealed record PolicyAsCodeDefinitionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma política de governança definida como código (YAML ou JSON),
/// versionada no sistema. Suporta gradual enforcement (Advisory → Warning → Blocking)
/// e pode ser associada a GovernancePacks para distribuição.
/// Permite simulação de impacto antes de enforcement real.
/// </summary>
public sealed class PolicyAsCodeDefinition : AuditableEntity<PolicyAsCodeDefinitionId>
{
    private PolicyAsCodeDefinition() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome técnico único da política (ex: "require-openapi-contract").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição legível da política.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e âmbito da política.</summary>
    public string? Description { get; private set; }

    /// <summary>Versão semântica da definição (ex: "1.0.0").</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Formato da definição: YAML ou JSON.</summary>
    public PolicyDefinitionFormat Format { get; private set; }

    /// <summary>Conteúdo completo da política em YAML ou JSON.</summary>
    public string DefinitionContent { get; private set; } = string.Empty;

    /// <summary>Modo de enforcement atual desta política.</summary>
    public PolicyEnforcementMode EnforcementMode { get; private set; }

    /// <summary>Estado do ciclo de vida desta definição.</summary>
    public PolicyDefinitionStatus Status { get; private set; }

    /// <summary>Número de serviços que esta política se aplica (calculado em SimulatePolicyApplication).</summary>
    public int? SimulatedAffectedServices { get; private set; }

    /// <summary>Número de serviços não-conformes (calculado em SimulatePolicyApplication).</summary>
    public int? SimulatedNonCompliantServices { get; private set; }

    /// <summary>Data/hora UTC da última simulação de impacto.</summary>
    public DateTimeOffset? LastSimulatedAt { get; private set; }

    /// <summary>Identificador do utilizador que registou a política.</summary>
    public string RegisteredBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria uma nova definição de política como código.
    /// </summary>
    public static PolicyAsCodeDefinition Create(
        Guid tenantId,
        string name,
        string displayName,
        string? description,
        string version,
        PolicyDefinitionFormat format,
        string definitionContent,
        PolicyEnforcementMode enforcementMode,
        string registeredBy)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));
        if (description is not null) Guard.Against.StringTooLong(description, 1000, nameof(description));
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.StringTooLong(version, 30, nameof(version));
        Guard.Against.NullOrWhiteSpace(definitionContent);
        Guard.Against.NullOrWhiteSpace(registeredBy);

        return new PolicyAsCodeDefinition
        {
            Id = new PolicyAsCodeDefinitionId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Version = version.Trim(),
            Format = format,
            DefinitionContent = definitionContent.Trim(),
            EnforcementMode = enforcementMode,
            Status = PolicyDefinitionStatus.Draft,
            RegisteredBy = registeredBy.Trim()
        };
    }

    /// <summary>
    /// Activa a política, tornando-a elegível para rollout.
    /// </summary>
    public void Activate()
    {
        if (Status == PolicyDefinitionStatus.Deprecated)
            throw new InvalidOperationException("Cannot activate a deprecated policy.");
        Status = PolicyDefinitionStatus.Active;
    }

    /// <summary>
    /// Deprecia a política, impedindo novos rollouts.
    /// </summary>
    public void Deprecate()
    {
        Status = PolicyDefinitionStatus.Deprecated;
    }

    /// <summary>
    /// Faz a transição do modo de enforcement, respeitando a progressão Advisory → SoftEnforce → HardEnforce.
    /// </summary>
    /// <param name="targetMode">Modo alvo de enforcement.</param>
    public void TransitionEnforcement(PolicyEnforcementMode targetMode)
    {
        if (targetMode <= EnforcementMode)
            throw new InvalidOperationException(
                $"Cannot transition enforcement from '{EnforcementMode}' to '{targetMode}'. Transitions must be forward-only (Advisory → SoftEnforce → HardEnforce).");

        EnforcementMode = targetMode;
    }

    /// <summary>
    /// Regista os resultados da última simulação de impacto da política.
    /// </summary>
    public void RecordSimulationResult(int affectedServices, int nonCompliantServices, DateTimeOffset simulatedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(affectedServices);
        if (nonCompliantServices < 0 || nonCompliantServices > affectedServices)
            throw new ArgumentOutOfRangeException(nameof(nonCompliantServices));

        SimulatedAffectedServices = affectedServices;
        SimulatedNonCompliantServices = nonCompliantServices;
        LastSimulatedAt = simulatedAt;
    }
}
