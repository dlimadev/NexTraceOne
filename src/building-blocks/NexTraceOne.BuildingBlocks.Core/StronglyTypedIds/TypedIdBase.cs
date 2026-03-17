namespace NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

/// <summary>
/// Implementação base para identificadores fortemente tipados.
/// Cada módulo cria seus próprios Ids herdando desta classe.
/// Exemplo: public sealed record ReleaseId(Guid Value) : TypedIdBase(Value);
/// </summary>
public abstract record TypedIdBase(Guid Value) : ITypedId
{
    /// <summary>Cria um novo Id com Guid gerado automaticamente.</summary>
    public static Guid NewId() => Guid.NewGuid();

    /// <summary>Representação string do Id (formato UUID padrão).</summary>
    public override string ToString() => Value.ToString();
}
