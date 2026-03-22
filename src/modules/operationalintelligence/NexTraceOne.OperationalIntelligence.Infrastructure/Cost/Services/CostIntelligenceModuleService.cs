using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Services;

/// <summary>
/// Implementação do contrato público do módulo CostIntelligence.
/// Usa CostIntelligenceDbContext diretamente para consultas de leitura otimizadas.
/// Outros módulos consomem este serviço via ICostIntelligenceModule — nunca acessam o DbContext.
/// </summary>
internal sealed class CostIntelligenceModuleService(
    CostIntelligenceDbContext context,
    ILogger<CostIntelligenceModuleService> logger) : ICostIntelligenceModule
{
    /// <inheritdoc />
    public async Task<decimal?> GetCurrentMonthlyCostAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching current monthly cost for service '{ServiceName}' in environment '{Environment}'",
            serviceName, environment);

        var profile = await context.ServiceCostProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                p => p.ServiceName == serviceName && p.Environment == environment,
                cancellationToken);

        return profile?.CurrentMonthCost;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetCostTrendPercentageAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching cost trend percentage for service '{ServiceName}' in environment '{Environment}'",
            serviceName, environment);

        var trend = await context.CostTrends
            .AsNoTracking()
            .Where(t => t.ServiceName == serviceName && t.Environment == environment)
            .OrderByDescending(t => t.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        return trend?.PercentageChange;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostRecordSummary>> GetCostRecordsAsync(
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching cost records for period '{Period}'", period ?? "all");

        var query = context.CostRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.TotalCost)
            .Select(r => new CostRecordSummary(
                r.ServiceId,
                r.ServiceName,
                r.Team,
                r.Domain,
                r.Environment,
                r.TotalCost,
                r.Currency,
                r.Period,
                r.Source))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CostRecordSummary?> GetServiceCostAsync(
        string serviceId,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching cost for service '{ServiceId}', period '{Period}'",
            serviceId, period ?? "latest");

        var query = context.CostRecords
            .AsNoTracking()
            .Where(r => r.ServiceId == serviceId);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        var record = await query
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return null;

        return new CostRecordSummary(
            record.ServiceId,
            record.ServiceName,
            record.Team,
            record.Domain,
            record.Environment,
            record.TotalCost,
            record.Currency,
            record.Period,
            record.Source);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostRecordSummary>> GetCostsByTeamAsync(
        string team,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching costs for team '{Team}', period '{Period}'", team, period ?? "all");

        var query = context.CostRecords
            .AsNoTracking()
            .Where(r => r.Team == team);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.TotalCost)
            .Select(r => new CostRecordSummary(
                r.ServiceId,
                r.ServiceName,
                r.Team,
                r.Domain,
                r.Environment,
                r.TotalCost,
                r.Currency,
                r.Period,
                r.Source))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostRecordSummary>> GetCostsByDomainAsync(
        string domain,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching costs for domain '{Domain}', period '{Period}'", domain, period ?? "all");

        var query = context.CostRecords
            .AsNoTracking()
            .Where(r => r.Domain == domain);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.TotalCost)
            .Select(r => new CostRecordSummary(
                r.ServiceId,
                r.ServiceName,
                r.Team,
                r.Domain,
                r.Environment,
                r.TotalCost,
                r.Currency,
                r.Period,
                r.Source))
            .ToListAsync(cancellationToken);
    }
}
