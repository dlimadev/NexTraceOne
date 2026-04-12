using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa um gate de compliance configurável para contratos.
/// Define regras, âmbito (organização/equipa/ambiente) e comportamento de bloqueio.
/// Permite governança automatizada de contratos antes de promoção ou publicação.
/// </summary>
public sealed class ContractComplianceGate : Entity<ContractComplianceGateId>
{
    private ContractComplianceGate() { }

    /// <summary>Nome descritivo do gate de compliance.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do gate e das suas regras (opcional).</summary>
    public string? Description { get; private set; }

    /// <summary>Configuração das regras de compliance em formato JSON (JSONB).</summary>
    public string? Rules { get; private set; }

    /// <summary>Âmbito de aplicação do gate: organização, equipa ou ambiente.</summary>
    public ComplianceGateScope Scope { get; private set; }

    /// <summary>Identificador do âmbito (ex: teamId, environmentId, orgId).</summary>
    public string ScopeId { get; private set; } = string.Empty;

    /// <summary>Indica se violações devem bloquear a publicação/promoção do contrato.</summary>
    public bool BlockOnViolation { get; private set; }

    /// <summary>Indica se o gate está ativo e deve ser avaliado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Identificador do utilizador que criou o gate.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Momento de criação do gate.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do tenant para isolamento multi-tenant.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo gate de compliance contratual com estado ativo.
    /// </summary>
    public static ContractComplianceGate Create(
        string name,
        string? description,
        string? rules,
        ComplianceGateScope scope,
        string scopeId,
        bool blockOnViolation,
        string? createdBy,
        DateTimeOffset createdAt,
        string? tenantId)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 200);
        Guard.Against.NullOrWhiteSpace(scopeId);
        Guard.Against.StringTooLong(scopeId, 200);
        Guard.Against.EnumOutOfRange(scope);

        if (description is not null)
            Guard.Against.StringTooLong(description, 2000);

        if (createdBy is not null)
            Guard.Against.StringTooLong(createdBy, 200);

        return new ContractComplianceGate
        {
            Id = ContractComplianceGateId.New(),
            Name = name,
            Description = description,
            Rules = rules,
            Scope = scope,
            ScopeId = scopeId,
            BlockOnViolation = blockOnViolation,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Desativa o gate de compliance. Gates inativos não são avaliados.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reativa o gate de compliance.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Atualiza a configuração de regras do gate de compliance.
    /// </summary>
    public void UpdateRules(string? rules)
    {
        Rules = rules;
    }
}

/// <summary>Identificador fortemente tipado de ContractComplianceGate.</summary>
public sealed record ContractComplianceGateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractComplianceGateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractComplianceGateId From(Guid id) => new(id);
}
