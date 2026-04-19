using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BackgroundWorkers;

/// <summary>
/// Provedor de tempo do host de workers.
/// </summary>
internal sealed class WorkerDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

/// <summary>
/// Usuário técnico padrão usado pelos workers da plataforma.
/// </summary>
internal sealed class WorkerCurrentUser : ICurrentUser
{
    public string Id => "background-worker";

    public string Name => "Background Worker";

    public string Email => "background-worker@nextraceone.local";

    public bool IsAuthenticated => true;

    public string? Persona => null;

    public bool HasPermission(string permission) => true;
}

/// <summary>
/// Tenant neutro usado pelos workers da plataforma quando não há tenant na execução.
/// </summary>
internal sealed class WorkerCurrentTenant : ICurrentTenant
{
    public Guid Id => Guid.Empty;

    public string Slug => string.Empty;

    public string Name => string.Empty;

    public bool IsActive => false;

    public bool HasCapability(string capability) => true;
}
