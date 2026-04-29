using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para SetupWizardStep (F-04).</summary>
internal sealed class SetupWizardRepository(GovernanceDbContext db) : ISetupWizardRepository
{
    public async Task<IReadOnlyList<SetupWizardStep>> ListByTenantAsync(
        string tenantId, CancellationToken ct = default)
        => await db.SetupWizardSteps
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<SetupWizardStep?> GetByStepIdAsync(
        string tenantId, string stepId, CancellationToken ct = default)
        => await db.SetupWizardSteps
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.StepId == stepId.ToLowerInvariant(), ct);

    public async Task AddAsync(SetupWizardStep step, CancellationToken ct = default)
        => await db.SetupWizardSteps.AddAsync(step, ct);

    public Task UpdateAsync(SetupWizardStep step, CancellationToken ct = default)
    {
        db.SetupWizardSteps.Update(step);
        return Task.CompletedTask;
    }
}
