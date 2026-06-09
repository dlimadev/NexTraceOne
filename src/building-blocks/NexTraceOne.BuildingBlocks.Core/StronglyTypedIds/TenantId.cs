namespace NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

/// <summary>
/// Identificador fortemente tipado para Tenant, partilhado por todos os módulos.
///
/// Motivação: hoje cada módulo usa <c>Guid TenantId</c> sem tipo forte, criando
/// inconsistência (alguns módulos usam <c>string</c>, outros <c>Guid</c>).
/// Este tipo partilhado deve ser o único <em>TenantId</em> em todo o sistema.
///
/// Migração gradual: entidades existentes mantêm o tipo actual até refactor isolado.
/// Novas entidades devem usar este tipo desde o início.
///
/// Conversão EF Core recomendada:
/// <code>
/// builder.Property(x => x.TenantId)
///     .HasConversion(id => id.Value, value => new TenantId(value));
/// </code>
/// </summary>
public sealed record TenantId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria um novo TenantId a partir de um Guid existente.</summary>
    public static TenantId From(Guid value) => new(value);

    /// <summary>Tenta converter uma string UUID para TenantId. Retorna null se inválido.</summary>
    public static TenantId? TryParse(string? value)
        => Guid.TryParse(value, out var g) ? new TenantId(g) : null;
}
