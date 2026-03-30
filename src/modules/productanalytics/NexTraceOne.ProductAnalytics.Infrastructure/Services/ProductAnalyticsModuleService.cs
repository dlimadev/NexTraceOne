using Microsoft.EntityFrameworkCore;

using NexTraceOne.ProductAnalytics.Contracts;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Services;

/// <summary>
/// Implementação do contrato cross-module <see cref="IProductAnalyticsModule"/>.
/// Expõe métricas de uso do produto para outros bounded contexts
/// (ex: Governance para métricas de adoção, AIOps para correlação de uso).
/// Acede ao <see cref="ProductAnalyticsDbContext"/> em modo read-only.
/// </summary>
internal sealed class ProductAnalyticsModuleService(
    ProductAnalyticsDbContext context) : IProductAnalyticsModule
{
    public async Task<long> GetModuleEventCountAsync(
        string moduleName,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return 0;

        if (!Enum.TryParse<ProductModule>(moduleName, ignoreCase: true, out var module))
            return 0;

        return await context.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.Module == module && e.OccurredAt >= from && e.OccurredAt <= until)
            .LongCountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActivePersonasAsync(
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= until && e.Persona != null)
            .Select(e => e.Persona!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnalyticsSummaryDto?> GetModuleSummaryAsync(
        string moduleName,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return null;

        if (!Enum.TryParse<ProductModule>(moduleName, ignoreCase: true, out var module))
            return null;

        var query = context.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.Module == module && e.OccurredAt >= from && e.OccurredAt <= until);

        var totalEvents = await query.LongCountAsync(cancellationToken);

        if (totalEvents == 0)
            return null;

        var uniqueUsers = await query
            .Where(e => e.UserId != null)
            .Select(e => e.UserId!)
            .Distinct()
            .CountAsync(cancellationToken);

        var uniquePersonas = await query
            .Where(e => e.Persona != null)
            .Select(e => e.Persona!)
            .Distinct()
            .CountAsync(cancellationToken);

        // Adoption rate: proportion of available personas that used this module
        const int totalKnownPersonas = 7; // Engineer, TechLead, Architect, Product, Executive, PlatformAdmin, Auditor
        var adoptionRate = totalKnownPersonas > 0
            ? Math.Round((decimal)uniquePersonas / totalKnownPersonas * 100, 2)
            : 0m;

        return new AnalyticsSummaryDto(
            ModuleName: moduleName,
            TotalEvents: totalEvents,
            UniqueUsers: uniqueUsers,
            UniquePersonas: uniquePersonas,
            AdoptionRate: adoptionRate,
            From: from,
            Until: until);
    }
}
