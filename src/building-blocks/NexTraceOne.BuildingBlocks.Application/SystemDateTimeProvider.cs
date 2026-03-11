using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Application;

/// <summary>
/// Implementação padrão do provedor de data/hora usando UTC.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
