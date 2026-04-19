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

    /// <inheritdoc />
    public async Task<BudgetForecastSummary?> GetLatestBudgetForecastAsync(
        string serviceId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching latest budget forecast for service '{ServiceId}' in environment '{Environment}'",
            serviceId, environment);

        var forecast = await context.BudgetForecasts
            .AsNoTracking()
            .Where(f => f.ServiceId == serviceId && f.Environment == environment)
            .OrderByDescending(f => f.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (forecast is null)
            return null;

        return new BudgetForecastSummary(
            forecast.Id.Value,
            forecast.ServiceId,
            forecast.ForecastPeriod,
            forecast.ProjectedCost,
            forecast.BudgetLimit,
            forecast.IsOverBudgetProjected,
            forecast.Method,
            forecast.ComputedAt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EfficiencyRecommendationSummary>> GetUnacknowledgedRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching unacknowledged efficiency recommendations");

        return await context.EfficiencyRecommendations
            .AsNoTracking()
            .Where(r => !r.IsAcknowledged)
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new EfficiencyRecommendationSummary(
                r.Id.Value,
                r.ServiceId,
                r.ServiceName,
                r.DeviationPercent,
                r.RecommendationText,
                r.Priority))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CostContextPerDay?> GetCostContextPerDayAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Computing cost context per day for service '{ServiceName}' in environment '{Environment}'",
            serviceName,
            environment);

        var now = DateTimeOffset.UtcNow;
        var currentPeriod = now.ToString("yyyy-MM");
        var previousPeriod = now.AddMonths(-1).ToString("yyyy-MM");

        var records = await context.CostRecords
            .AsNoTracking()
            .Where(r => r.ServiceName == serviceName
                        && (r.Environment == null || r.Environment == environment)
                        && (r.Period == currentPeriod || r.Period == previousPeriod))
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            return null;

        var currentRecords = records.Where(r => r.Period == currentPeriod).ToList();
        var previousRecords = records.Where(r => r.Period == previousPeriod).ToList();

        var currentMonthlyCost = currentRecords.Sum(r => r.TotalCost);
        var previousMonthlyCost = previousRecords.Sum(r => r.TotalCost);
        var currency = records.First().Currency;

        // Normalize to daily cost — current month uses elapsed days, previous full month
        var daysElapsedThisMonth = Math.Max(1, now.Day);
        var daysInPreviousMonth = DateTime.DaysInMonth(now.AddMonths(-1).Year, now.AddMonths(-1).Month);

        var actualCostPerDay = daysElapsedThisMonth > 0
            ? Math.Round(currentMonthlyCost / daysElapsedThisMonth, 4)
            : 0m;

        var baselineCostPerDay = previousRecords.Count > 0
            ? Math.Round(previousMonthlyCost / daysInPreviousMonth, 4)
            : actualCostPerDay; // If no baseline, use current as baseline (no comparison)

        return new CostContextPerDay(
            ActualCostPerDay: actualCostPerDay,
            BaselineCostPerDay: baselineCostPerDay,
            Currency: currency,
            ServiceName: serviceName,
            Environment: environment);
    }
}
