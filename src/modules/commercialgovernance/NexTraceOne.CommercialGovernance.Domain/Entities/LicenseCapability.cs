using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Capacidade licenciada que habilita funcionalidades específicas da plataforma.
/// </summary>
public sealed class LicenseCapability : Entity<LicenseCapabilityId>
{
    private LicenseCapability() { }

    /// <summary>Código único da capability.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Nome amigável da capability.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Indica se a capability está habilitada na licença.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Cria uma capability habilitada para a licença.</summary>
    public static LicenseCapability Create(string code, string name, bool isEnabled = true)
        => new()
        {
            Id = LicenseCapabilityId.New(),
            Code = Guard.Against.NullOrWhiteSpace(code),
            Name = Guard.Against.NullOrWhiteSpace(name),
            IsEnabled = isEnabled
        };

    /// <summary>Habilita a capability na licença.</summary>
    public void Enable() => IsEnabled = true;

    /// <summary>Desabilita a capability na licença.</summary>
    public void Disable() => IsEnabled = false;
}

/// <summary>Identificador fortemente tipado de LicenseCapability.</summary>
public sealed record LicenseCapabilityId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseCapabilityId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseCapabilityId From(Guid id) => new(id);
}
