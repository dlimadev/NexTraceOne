using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa uma política de acesso granular a módulos, páginas e ações
/// do NexTraceOne, persistida em base de dados para permitir configuração sem redeploy.
///
/// Modelo enterprise (bancos/seguradoras):
/// Cada registo define o que um papel pode fazer num contexto específico:
/// - <see cref="Module"/>: módulo da plataforma (ex.: "Catalog", "Contracts", "Operations").
/// - <see cref="Page"/>: página ou área dentro do módulo (ex.: "ServiceCatalog", "ContractStudio").
/// - <see cref="Action"/>: ação específica (ex.: "Create", "Edit", "Delete", "Approve", "Export").
///
/// Este modelo permite controlo granular por tenant, superando a limitação
/// do modelo anterior baseado exclusivamente em permission codes planos.
///
/// Exemplo: RoleId=Developer, Module="Contracts", Page="ContractStudio", Action="Import"
/// → Controla se Developer pode importar contratos no Contract Studio.
/// </summary>
public sealed class ModuleAccessPolicy : Entity<ModuleAccessPolicyId>
{
    private ModuleAccessPolicy() { }

    /// <summary>Papel ao qual a política se aplica.</summary>
    public RoleId RoleId { get; private set; } = default!;

    /// <summary>
    /// Tenant ao qual a política pertence. Nulo para políticas padrão do sistema.
    /// Políticas específicas de tenant sobrepõem as padrões do sistema.
    /// </summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>Módulo da plataforma (ex.: "Identity", "Catalog", "Contracts", "Operations").</summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>Página ou sub-área dentro do módulo (ex.: "ServiceCatalog", "ContractStudio"). Wildcard "*" permite acesso a todas.</summary>
    public string Page { get; private set; } = string.Empty;

    /// <summary>Ação granular (ex.: "Read", "Create", "Edit", "Delete", "Approve", "Export"). Wildcard "*" permite todas.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Se o acesso está concedido ou negado. False funciona como deny explícito.</summary>
    public bool IsAllowed { get; private set; }

    /// <summary>Indica se a política está ativa. Permite desativação sem remoção.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação da política.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador de quem criou a política, para trilha de auditoria.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Identificador de quem modificou a política por último.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>
    /// Cria uma nova política de acesso a módulo/página/ação.
    /// </summary>
    /// <param name="roleId">Papel ao qual a política se aplica.</param>
    /// <param name="tenantId">Tenant associado, nulo para políticas padrão do sistema.</param>
    /// <param name="module">Módulo da plataforma.</param>
    /// <param name="page">Página ou sub-área do módulo. Use "*" para wildcard.</param>
    /// <param name="action">Ação granular. Use "*" para wildcard.</param>
    /// <param name="isAllowed">Se o acesso está concedido.</param>
    /// <param name="now">Data/hora UTC atual.</param>
    /// <param name="createdBy">Identificador do criador.</param>
    public static ModuleAccessPolicy Create(
        RoleId roleId,
        TenantId? tenantId,
        string module,
        string page,
        string action,
        bool isAllowed,
        DateTimeOffset now,
        string? createdBy)
        => new()
        {
            Id = ModuleAccessPolicyId.New(),
            RoleId = Guard.Against.Null(roleId),
            TenantId = tenantId,
            Module = Guard.Against.NullOrWhiteSpace(module),
            Page = Guard.Against.NullOrWhiteSpace(page),
            Action = Guard.Against.NullOrWhiteSpace(action),
            IsAllowed = isAllowed,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy
        };

    /// <summary>Atualiza a decisão de acesso (permitir/negar).</summary>
    public void UpdateAccess(bool isAllowed, DateTimeOffset now, string? updatedBy)
    {
        IsAllowed = isAllowed;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>Desativa a política sem remoção física.</summary>
    public void Deactivate(DateTimeOffset now, string? updatedBy)
    {
        IsActive = false;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>Reativa uma política previamente desativada.</summary>
    public void Activate(DateTimeOffset now, string? updatedBy)
    {
        IsActive = true;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Verifica se esta política corresponde a um contexto específico de módulo/página/ação.
    /// Suporta wildcard "*" para página e ação.
    /// </summary>
    public bool Matches(string module, string page, string action)
    {
        if (!string.Equals(Module, module, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Page != "*" && !string.Equals(Page, page, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Action != "*" && !string.Equals(Action, action, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}

/// <summary>Identificador fortemente tipado de ModuleAccessPolicy.</summary>
public sealed record ModuleAccessPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ModuleAccessPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ModuleAccessPolicyId From(Guid id) => new(id);
}
