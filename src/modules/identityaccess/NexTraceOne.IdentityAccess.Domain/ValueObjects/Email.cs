using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using System.Net.Mail;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que representa um email normalizado e validado.
/// </summary>
public sealed class Email : ValueObject
{
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>Valor normalizado do email.</summary>
    public string Value { get; }

    /// <summary>Cria um email válido e normalizado em lowercase.</summary>
    public static Email Create(string value)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(value).Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out _))
        {
            throw new ArgumentException($"Email '{value}' is invalid.", nameof(value));
        }

        return new Email(normalized);
    }

    /// <summary>Reconstrói um email persistido sem alterar a normalização.</summary>
    public static Email FromDatabase(string value)
        => new(Guard.Against.NullOrWhiteSpace(value));

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>Retorna o email como string.</summary>
    public override string ToString() => Value;
}
