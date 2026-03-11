namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para todas as entidades do domínio da plataforma NexTraceOne.
/// Implementa igualdade baseada em identidade (Id), não em referência de objeto.
/// Toda entidade possui um identificador fortemente tipado que garante que
/// Ids de tipos diferentes nunca sejam comparados acidentalmente.
/// </summary>
/// <typeparam name="TId">Tipo do identificador, deve implementar ITypedId.</typeparam>
public abstract class Entity<TId> where TId : ITypedId
{
    /// <summary>Identificador único e imutável desta entidade.</summary>
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Id.Equals(default(TId)) || other.Id.Equals(default(TId))) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}
