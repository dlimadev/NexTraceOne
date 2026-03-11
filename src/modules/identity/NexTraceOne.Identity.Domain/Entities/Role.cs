using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Entidade que representa um papel de autorização dentro de um tenant.
/// </summary>
public sealed class Role : Entity<RoleId>
{
    /// <summary>Nome do papel administrativo total.</summary>
    public const string Admin = "Admin";
    /// <summary>Nome do papel gerencial.</summary>
    public const string Manager = "Manager";
    /// <summary>Nome do papel técnico de desenvolvimento.</summary>
    public const string Developer = "Developer";
    /// <summary>Nome do papel somente leitura.</summary>
    public const string Viewer = "Viewer";
    /// <summary>Nome do papel de auditoria.</summary>
    public const string Auditor = "Auditor";

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

    /// <summary>Retorna as permissões padrão de um papel conhecido.</summary>
    public static IReadOnlyList<string> GetPermissionsForRole(string roleName)
        => roleName switch
        {
            Admin => [
                "identity:users:read",
                "identity:users:write",
                "identity:roles:assign",
                "identity:sessions:revoke",
                "platform:audit:read"],
            Manager => [
                "identity:users:read",
                "identity:roles:assign",
                "platform:audit:read"],
            Developer => [
                "identity:users:read"],
            Viewer => [
                "identity:users:read"],
            Auditor => [
                "identity:users:read",
                "platform:audit:read"],
            _ => []
        };
}

/// <summary>Identificador fortemente tipado de Role.</summary>
public sealed record RoleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RoleId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RoleId From(Guid id) => new(id);
}
