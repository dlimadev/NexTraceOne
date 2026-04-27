using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Repositório para DashboardTemplate (V3.8 — Marketplace &amp; Plugin SDK).</summary>
public interface IDashboardTemplateRepository
{
    Task<DashboardTemplate?> GetByIdAsync(DashboardTemplateId id, string? tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<DashboardTemplate> Items, int Total)> ListAsync(string tenantId, string? category, string? persona, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(DashboardTemplate template, CancellationToken ct = default);
    Task UpdateAsync(DashboardTemplate template, CancellationToken ct = default);
    Task DeleteAsync(DashboardTemplateId id, string tenantId, CancellationToken ct = default);
}
