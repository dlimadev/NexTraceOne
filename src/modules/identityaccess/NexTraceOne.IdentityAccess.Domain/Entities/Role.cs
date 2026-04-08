using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa um papel de autorização dentro de um tenant.
/// </summary>
public sealed class Role : Entity<RoleId>
{
    /// <summary>Nome do papel de administrador da plataforma com acesso total.</summary>
    public const string PlatformAdmin = "PlatformAdmin";
    /// <summary>Nome do papel de líder técnico com aprovação e governança.</summary>
    public const string TechLead = "TechLead";
    /// <summary>Nome do papel técnico de desenvolvimento.</summary>
    public const string Developer = "Developer";
    /// <summary>Nome do papel somente leitura.</summary>
    public const string Viewer = "Viewer";
    /// <summary>Nome do papel de auditoria e compliance.</summary>
    public const string Auditor = "Auditor";
    /// <summary>Nome do papel de revisão de segurança.</summary>
    public const string SecurityReview = "SecurityReview";
    /// <summary>Nome do papel restrito apenas a aprovações de workflow.</summary>
    public const string ApprovalOnly = "ApprovalOnly";
    /// <summary>Nome do papel restrito apenas ao assistente de IA.</summary>
    public const string AiUser = "AiUser";

    private Role() { }

    /// <summary>Nome único do papel.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição resumida do papel.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Indica se o papel é pré-definido pelo sistema.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Cria um papel pré-definido do sistema.</summary>
    public static Role CreateSystem(RoleId id, string name, string description)
        => new()
        {
            Id = Guard.Against.Null(id),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Description = Guard.Against.NullOrWhiteSpace(description),
            IsSystem = true
        };

    /// <summary>Cria um papel customizado editável.</summary>
    public static Role CreateCustom(string name, string description)
        => new()
        {
            Id = RoleId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Description = Guard.Against.NullOrWhiteSpace(description),
            IsSystem = false
        };

    /// <summary>Atualiza nome e descrição de um papel customizado. Papéis de sistema não podem ser editados.</summary>
    public void Update(string name, string description)
    {
        if (IsSystem)
            throw new InvalidOperationException("System roles cannot be modified.");

        Name = Guard.Against.NullOrWhiteSpace(name);
        Description = Guard.Against.NullOrWhiteSpace(description);
    }

    /// <summary>
    /// Retorna as permissões padrão de um papel conhecido.
    /// Delega para <see cref="RolePermissionCatalog"/>, que é a fonte única de verdade
    /// para mapeamentos papel→permissões. Mantido aqui para compatibilidade retroativa.
    /// </summary>
    public static IReadOnlyList<string> GetPermissionsForRole(string roleName)
        => RolePermissionCatalog.GetPermissionsForRole(roleName);
}

/// <summary>Identificador fortemente tipado de Role.</summary>
public sealed record RoleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RoleId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RoleId From(Guid id) => new(id);
}
