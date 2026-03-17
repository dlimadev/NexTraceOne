using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Tests.TestDoubles;

/// <summary>
/// Provedor de data/hora fixo para cenários determinísticos nos testes.
/// </summary>
internal sealed class TestDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = utcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
