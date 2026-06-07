using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Converters;

/// <summary>
/// Value converter for strongly-typed IDs that implement <see cref="ITypedId"/>.
/// Stores the raw <see cref="Guid"/> in the database while using the typed wrapper in the domain model.
/// </summary>
public sealed class TypedIdValueConverter<TTypedId> : ValueConverter<TTypedId, Guid>
    where TTypedId : ITypedId
{
    public TypedIdValueConverter()
        : base(
            id => id.Value,
            value => CreateTypedId(value))
    {
    }

    private static TTypedId CreateTypedId(Guid value)
    {
        var instance = Activator.CreateInstance(typeof(TTypedId), value);
        return instance is null
            ? throw new InvalidOperationException($"Could not create instance of {typeof(TTypedId).Name} with Guid value.")
            : (TTypedId)instance;
    }
}
