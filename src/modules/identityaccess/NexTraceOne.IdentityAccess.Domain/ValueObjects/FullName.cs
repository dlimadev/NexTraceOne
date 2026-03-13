using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o nome completo do usuário.
/// </summary>
public sealed class FullName : ValueObject
{
    private FullName()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>Primeiro nome do usuário.</summary>
    public string FirstName { get; private set; }

    /// <summary>Sobrenome do usuário.</summary>
    public string LastName { get; private set; }

    /// <summary>Nome completo formatado.</summary>
    public string Value => $"{FirstName} {LastName}";

    /// <summary>Cria um nome completo validando tamanho dos componentes.</summary>
    public static FullName Create(string firstName, string lastName)
    {
        var normalizedFirstName = Guard.Against.NullOrWhiteSpace(firstName).Trim();
        var normalizedLastName = Guard.Against.NullOrWhiteSpace(lastName).Trim();

        if (normalizedFirstName.Length > 100 || normalizedLastName.Length > 100)
        {
            throw new ArgumentException("Full name exceeds the maximum allowed length.");
        }

        return new FullName(normalizedFirstName, normalizedLastName);
    }

    /// <summary>Cria um nome completo a partir de um nome único.</summary>
    public static FullName FromDisplayName(string displayName)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(displayName).Trim();
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 1
            ? Create(parts[0], parts[0])
            : Create(parts[0], string.Join(' ', parts.Skip(1)));
    }

    /// <summary>Reconstrói o nome completo a partir dos componentes persistidos.</summary>
    public static FullName FromDatabase(string firstName, string lastName)
        => new(
            Guard.Against.NullOrWhiteSpace(firstName),
            Guard.Against.NullOrWhiteSpace(lastName));

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    /// <summary>Retorna o nome completo formatado.</summary>
    public override string ToString() => Value;
}
