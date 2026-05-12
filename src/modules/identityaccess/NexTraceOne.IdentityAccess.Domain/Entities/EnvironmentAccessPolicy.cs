using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Identificador fortemente tipado de EnvironmentAccessPolicy.</summary>
public sealed record EnvironmentAccessPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentAccessPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentAccessPolicyId From(Guid id) => new(id);
}

/// <summary>
/// Política de acesso granular por ambiente.
/// Define quais roles têm acesso a um ambiente e quais requerem aprovação JIT.
/// </summary>
public sealed class EnvironmentAccessPolicy : AuditableEntity<EnvironmentAccessPolicyId>
{
    private EnvironmentAccessPolicy() { }

    /// <summary>Nome único da política dentro do tenant.</summary>
    public string PolicyName { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant proprietário.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Lista de ambientes cobertos por esta política (ex: ["production", "staging"]).</summary>
    public IReadOnlyList<string> Environments { get; private set; } = [];

    /// <summary>Roles com acesso permitido (sem JIT) a estes ambientes.</summary>
    public IReadOnlyList<string> AllowedRoles { get; private set; } = [];

    /// <summary>Roles que requerem aprovação JIT para aceder a estes ambientes.</summary>
    public IReadOnlyList<string> RequireJitForRoles { get; private set; } = [];

    /// <summary>Identificador do utilizador ou role que aprova pedidos JIT (opcional).</summary>
    public string? JitApprovalRequiredFrom { get; private set; }

    /// <summary>Indica se a política está activa.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Cria uma nova política de acesso por ambiente.</summary>
    public static EnvironmentAccessPolicy Create(
        string policyName,
        Guid tenantId,
        IReadOnlyList<string> environments,
        IReadOnlyList<string> allowedRoles,
        IReadOnlyList<string> requireJitForRoles,
        string? jitApprovalRequiredFrom,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(policyName);
        Guard.Against.Default(tenantId);
        Guard.Against.Null(environments);
        Guard.Against.Null(allowedRoles);
        Guard.Against.Null(requireJitForRoles);

        return new EnvironmentAccessPolicy
        {
            Id = EnvironmentAccessPolicyId.New(),
            PolicyName = policyName.Trim(),
            TenantId = tenantId,
            Environments = environments,
            AllowedRoles = allowedRoles,
            RequireJitForRoles = requireJitForRoles,
            JitApprovalRequiredFrom = jitApprovalRequiredFrom?.Trim(),
            IsActive = true,
        };
    }

    /// <summary>Actualiza os campos editáveis da política.</summary>
    public void Update(
        string policyName,
        IReadOnlyList<string> environments,
        IReadOnlyList<string> allowedRoles,
        IReadOnlyList<string> requireJitForRoles,
        string? jitApprovalRequiredFrom)
    {
        Guard.Against.NullOrWhiteSpace(policyName);
        PolicyName = policyName.Trim();
        Environments = environments;
        AllowedRoles = allowedRoles;
        RequireJitForRoles = requireJitForRoles;
        JitApprovalRequiredFrom = jitApprovalRequiredFrom?.Trim();
    }

    /// <summary>Desactiva a política sem a eliminar da base de dados.</summary>
    public void Deactivate() => IsActive = false;
}
