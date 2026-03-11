using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Vínculo da licença com a impressão digital do hardware autorizado.
/// </summary>
public sealed class HardwareBinding : Entity<HardwareBindingId>
{
    private HardwareBinding() { }

    /// <summary>Fingerprint do hardware autorizado.</summary>
    public string Fingerprint { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC do vínculo inicial.</summary>
    public DateTimeOffset BoundAt { get; private set; }

    /// <summary>Data/hora UTC da última validação do hardware.</summary>
    public DateTimeOffset LastValidatedAt { get; private set; }

    /// <summary>Cria um vínculo de hardware para a licença.</summary>
    public static HardwareBinding Create(string fingerprint, DateTimeOffset boundAt)
        => new()
        {
            Id = HardwareBindingId.New(),
            Fingerprint = Guard.Against.NullOrWhiteSpace(fingerprint),
            BoundAt = boundAt,
            LastValidatedAt = boundAt
        };

    /// <summary>Valida se o hardware informado corresponde ao vínculo existente.</summary>
    public bool Matches(string fingerprint)
        => string.Equals(Fingerprint, Guard.Against.NullOrWhiteSpace(fingerprint), StringComparison.Ordinal);

    /// <summary>Atualiza a data/hora da última validação do hardware.</summary>
    public void MarkValidated(DateTimeOffset validatedAt) => LastValidatedAt = validatedAt;
}

/// <summary>Identificador fortemente tipado de HardwareBinding.</summary>
public sealed record HardwareBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static HardwareBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static HardwareBindingId From(Guid id) => new(id);
}
