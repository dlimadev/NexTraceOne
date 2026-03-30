using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Services;

/// <summary>
/// Implementação do contrato cross-module <see cref="IReliabilityModule"/>.
/// Expõe dados de reliability para outros módulos (Governance, ChangeGovernance)
/// sem permitir acesso directo ao DbContext ou repositórios internos.
/// Leituras apenas — sem tracking para melhor performance.
/// </summary>
internal sealed class ReliabilityModuleService(ReliabilityDbContext context) : IReliabilityModule
{
    /// <inheritdoc />
    public async Task<string?> GetCurrentReliabilityStatusAsync(
        string serviceName, string environment, CancellationToken cancellationToken = default)
    {
        var snapshot = await context.ReliabilitySnapshots
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot?.RuntimeHealthStatus;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetRemainingErrorBudgetAsync(
        string serviceName, string environment, CancellationToken cancellationToken = default)
    {
        // Find the most recent error budget for any active SLO of this service/environment.
        // Uses a join instead of Include for compatibility with all EF providers.
        var activeSloIds = context.SloDefinitions
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceName && s.Environment == environment && s.IsActive)
            .Select(s => s.Id);

        var budget = await context.ErrorBudgetSnapshots
            .AsNoTracking()
            .Where(b => b.ServiceId == serviceName
                        && b.Environment == environment
                        && activeSloIds.Contains(b.SloDefinitionId))
            .OrderByDescending(b => b.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (budget is null || budget.TotalBudgetMinutes <= 0)
            return null;

        // Return remaining as fraction 0.0 – 1.0
        var remaining = budget.RemainingBudgetMinutes / budget.TotalBudgetMinutes;
        return Math.Clamp(Math.Round(remaining, 4), 0m, 1m);
    }

    /// <inheritdoc />
    public async Task<decimal?> GetCurrentBurnRateAsync(
        string serviceName, string environment, CancellationToken cancellationToken = default)
    {
        var burnRate = await context.BurnRateSnapshots
            .AsNoTracking()
            .Where(b => b.ServiceId == serviceName && b.Environment == environment)
            .OrderByDescending(b => b.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return burnRate?.BurnRate;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SloSummary>> GetServiceSlosAsync(
        string serviceName, string environment, CancellationToken cancellationToken = default)
    {
        var slos = await context.SloDefinitions
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceName && s.Environment == environment && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return slos
            .Select(s => new SloSummary(
                SloId: s.Id.Value.ToString(),
                ServiceName: s.ServiceId,
                Environment: s.Environment,
                SloType: s.Type.ToString(),
                TargetPercentage: s.TargetPercent,
                Status: DeriveStatus(s)))
            .ToList();
    }

    /// <summary>
    /// Derives the display status of an SLO by checking the latest error budget snapshot.
    /// Falls back to "Unknown" if no budget data is available.
    /// </summary>
    private static string DeriveStatus(Domain.Reliability.Entities.SloDefinition slo)
    {
        // The SloDefinition entity doesn't carry live status — that lives in ErrorBudgetSnapshot.
        // For the contract, we return "Active" for active SLOs. Consumers needing live budget
        // status should call GetRemainingErrorBudgetAsync separately.
        return slo.IsActive ? "Active" : "Inactive";
    }
}
