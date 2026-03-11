namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para Value Objects do domínio.
/// Value Objects são imutáveis e sua igualdade é baseada nos valores
/// de suas propriedades, não em identidade. São descartados e recriados
/// quando precisam mudar — nunca modificados in-place.
/// Exemplos: SemanticVersion, TenantId, Money, Email, GitContext.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Retorna os componentes que definem a igualdade deste Value Object.
    /// Toda subclasse DEVE sobrescrever este método retornando seus campos relevantes.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !Equals(left, right);
}
