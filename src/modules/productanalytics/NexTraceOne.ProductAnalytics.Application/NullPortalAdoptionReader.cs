using NexTraceOne.ProductAnalytics.Application.Abstractions;

namespace NexTraceOne.ProductAnalytics.Application;

/// <summary>
/// Implementação nula de <see cref="IPortalAdoptionReader"/>.
/// Utilizada quando não existe fonte de dados de adopção configurada.
/// </summary>
public sealed class NullPortalAdoptionReader : IPortalAdoptionReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IPortalAdoptionReader.TeamAdoptionEntry>> ListTeamAdoptionAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPortalAdoptionReader.TeamAdoptionEntry>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IPortalAdoptionReader.InactiveUserEntry>> ListInactiveUsersAsync(
        string tenantId, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPortalAdoptionReader.InactiveUserEntry>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IPortalAdoptionReader.DailyAdoptionSnapshot>> GetAdoptionTrendAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPortalAdoptionReader.DailyAdoptionSnapshot>>([]);
}
