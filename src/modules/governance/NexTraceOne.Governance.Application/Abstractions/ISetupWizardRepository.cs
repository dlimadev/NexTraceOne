using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Repositório para SetupWizardStep (F-04 — Wizard persistence).</summary>
public interface ISetupWizardRepository
{
    Task<IReadOnlyList<SetupWizardStep>> ListByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<SetupWizardStep?> GetByStepIdAsync(string tenantId, string stepId, CancellationToken ct = default);
    Task AddAsync(SetupWizardStep step, CancellationToken ct = default);
    Task UpdateAsync(SetupWizardStep step, CancellationToken ct = default);
}
