namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>
/// Repositório de registos de uso da extensão IDE por utilizador.
/// Alimenta GetDeveloperActivityReport (Wave AC.2) e métricas de adopção.
/// Wave AK.1 — IDE Context API.
/// </summary>
public interface IIDEUsageRepository
{
    Task AddAsync(IdeUsageRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<IdeUsageRecord>> ListByUserAsync(string userId, DateTimeOffset since, CancellationToken ct = default);
    Task<IReadOnlyList<IdeUsageRecord>> ListByTenantAsync(string tenantId, DateTimeOffset since, CancellationToken ct = default);

    /// <summary>Registo de uso IDE com tipo de evento e payload compacto.</summary>
    public sealed record IdeUsageRecord(
        Guid Id,
        string UserId,
        string TenantId,
        IdeEventType EventType,
        string? ResourceName,
        DateTimeOffset OccurredAt);

    /// <summary>Tipos de evento de uso da extensão IDE.</summary>
    public enum IdeEventType
    {
        ContractLookup = 1,
        ServiceLookup = 2,
        ChangeLookup = 3,
        AiAssistUsed = 4,
        HealthCheck = 5
    }
}
