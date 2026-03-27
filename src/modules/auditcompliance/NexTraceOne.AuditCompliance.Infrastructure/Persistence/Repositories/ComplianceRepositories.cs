using Microsoft.EntityFrameworkCore;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Repositories;

internal sealed class CompliancePolicyRepository(AuditDbContext context) : ICompliancePolicyRepository
{
    public async Task<CompliancePolicy?> GetByIdAsync(CompliancePolicyId id, CancellationToken cancellationToken)
        => await context.CompliancePolicies
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CompliancePolicy>> ListAsync(bool? isActive, string? category, CancellationToken cancellationToken)
    {
        var query = context.CompliancePolicies.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(CompliancePolicy policy) => context.CompliancePolicies.Add(policy);

    public void Update(CompliancePolicy policy) => context.CompliancePolicies.Update(policy);
}

internal sealed class AuditCampaignRepository(AuditDbContext context) : IAuditCampaignRepository
{
    public async Task<AuditCampaign?> GetByIdAsync(AuditCampaignId id, CancellationToken cancellationToken)
        => await context.AuditCampaigns
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AuditCampaign>> ListAsync(CampaignStatus? status, CancellationToken cancellationToken)
    {
        var query = context.AuditCampaigns.AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(AuditCampaign campaign) => context.AuditCampaigns.Add(campaign);

    public void Update(AuditCampaign campaign) => context.AuditCampaigns.Update(campaign);
}

internal sealed class ComplianceResultRepository(AuditDbContext context) : IComplianceResultRepository
{
    public async Task<ComplianceResult?> GetByIdAsync(ComplianceResultId id, CancellationToken cancellationToken)
        => await context.ComplianceResults
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ComplianceResult>> ListByPolicyIdAsync(CompliancePolicyId policyId, CancellationToken cancellationToken)
        => await context.ComplianceResults
            .Where(r => r.PolicyId == policyId)
            .OrderByDescending(r => r.EvaluatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ComplianceResult>> ListByCampaignIdAsync(AuditCampaignId campaignId, CancellationToken cancellationToken)
        => await context.ComplianceResults
            .Where(r => r.CampaignId == campaignId)
            .OrderByDescending(r => r.EvaluatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ComplianceResult>> ListAsync(
        CompliancePolicyId? policyId, AuditCampaignId? campaignId, ComplianceOutcome? outcome,
        CancellationToken cancellationToken)
    {
        var query = context.ComplianceResults.AsQueryable();

        if (policyId is not null)
            query = query.Where(r => r.PolicyId == policyId);

        if (campaignId is not null)
            query = query.Where(r => r.CampaignId == campaignId);

        if (outcome.HasValue)
            query = query.Where(r => r.Outcome == outcome.Value);

        return await query
            .OrderByDescending(r => r.EvaluatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(ComplianceResult result) => context.ComplianceResults.Add(result);
}

/// <summary>
/// Repositório de políticas de retenção de eventos de auditoria.
/// P7.4 — implementa IRetentionPolicyRepository para tornar RetentionPolicy funcionalmente real.
/// </summary>
internal sealed class RetentionPolicyRepository(AuditDbContext context) : IRetentionPolicyRepository
{
    public async Task<IReadOnlyList<RetentionPolicy>> ListActiveAsync(CancellationToken cancellationToken)
        => await context.RetentionPolicies
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RetentionPolicy>> ListAllAsync(CancellationToken cancellationToken)
        => await context.RetentionPolicies
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<RetentionPolicy?> GetByIdAsync(RetentionPolicyId id, CancellationToken cancellationToken)
        => await context.RetentionPolicies
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<RetentionPolicy?> GetMostRestrictiveActiveAsync(CancellationToken cancellationToken)
        => await context.RetentionPolicies
            .Where(p => p.IsActive)
            .OrderBy(p => p.RetentionDays)  // lowest days = most restrictive
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(RetentionPolicy policy) => context.RetentionPolicies.Add(policy);
}
