using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Entidade que representa uma permissão granular organizada por módulo.
/// </summary>
public sealed class Permission : Entity<PermissionId>
{
    private Permission() { }

    /// <summary>Código único da permissão, no formato módulo:ação.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Nome amigável da permissão.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Módulo ao qual a permissão pertence.</summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>Cria uma permissão com identificador conhecido.</summary>
    public static Permission Create(PermissionId id, string code, string name, string module)
        => new()
        {
            Id = Guard.Against.Null(id),
            Code = Guard.Against.NullOrWhiteSpace(code),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Module = Guard.Against.NullOrWhiteSpace(module)
        };
}

/// <summary>Identificador fortemente tipado de Permission.</summary>
public sealed record PermissionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PermissionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PermissionId From(Guid id) => new(id);
}
