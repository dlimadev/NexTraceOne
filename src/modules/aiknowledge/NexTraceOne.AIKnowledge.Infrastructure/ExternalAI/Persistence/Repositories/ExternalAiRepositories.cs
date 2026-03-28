using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Repositories;

/// <summary>
/// Repositório de captures de conhecimento de IA externa.
/// Implementa IKnowledgeCaptureRepository com operações persistidas no ExternalAiDbContext.
/// </summary>
internal sealed class KnowledgeCaptureRepository(ExternalAiDbContext context) : IKnowledgeCaptureRepository
{
    public async Task<KnowledgeCapture?> GetByIdAsync(KnowledgeCaptureId id, CancellationToken ct)
        => await context.KnowledgeCaptures.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(KnowledgeCapture capture, CancellationToken ct)
    {
        await context.KnowledgeCaptures.AddAsync(capture, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(KnowledgeCapture capture, CancellationToken ct)
    {
        context.KnowledgeCaptures.Update(capture);
        await context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<KnowledgeCapture> Items, int Total)> ListAsync(
        KnowledgeStatus? status,
        string? category,
        string? tags,
        string? textFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.KnowledgeCaptures.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(tags))
            query = query.Where(c => c.Tags.Contains(tags));

        if (!string.IsNullOrWhiteSpace(textFilter))
            query = query.Where(c => c.Title.Contains(textFilter) || c.Content.Contains(textFilter));

        if (from.HasValue)
            query = query.Where(c => c.CapturedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(c => c.CapturedAt <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ExternalAiUsageMetrics> GetUsageMetricsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var consultationQuery = context.Consultations.AsNoTracking();
        var captureQuery = context.KnowledgeCaptures.AsNoTracking();

        if (from.HasValue)
        {
            consultationQuery = consultationQuery.Where(c => c.RequestedAt >= from.Value);
            captureQuery = captureQuery.Where(c => c.CapturedAt >= from.Value);
        }

        if (to.HasValue)
        {
            consultationQuery = consultationQuery.Where(c => c.RequestedAt <= to.Value);
            captureQuery = captureQuery.Where(c => c.CapturedAt <= to.Value);
        }

        var totalConsultations = await consultationQuery.CountAsync(ct);
        var totalTokensUsed = await consultationQuery.SumAsync(c => (long)c.TokensUsed, ct);
        var completedConsultations = await consultationQuery
            .CountAsync(c => c.Status == ConsultationStatus.Completed, ct);
        var failedConsultations = await consultationQuery
            .CountAsync(c => c.Status == ConsultationStatus.Failed, ct);

        var byProvider = await consultationQuery
            .GroupBy(c => c.ProviderId)
            .Select(g => new ProviderUsageMetric(
                g.Key.Value.ToString(),
                g.Count(),
                g.Sum(c => (long)c.TokensUsed)))
            .ToListAsync(ct);

        var totalCaptures = await captureQuery.CountAsync(ct);
        var approvedCaptures = await captureQuery.CountAsync(c => c.Status == KnowledgeStatus.Approved, ct);
        var rejectedCaptures = await captureQuery.CountAsync(c => c.Status == KnowledgeStatus.Rejected, ct);
        var totalReuses = await captureQuery.SumAsync(c => (long)c.ReuseCount, ct);

        return new ExternalAiUsageMetrics(
            totalConsultations,
            completedConsultations,
            failedConsultations,
            totalTokensUsed,
            byProvider,
            totalCaptures,
            approvedCaptures,
            rejectedCaptures,
            totalCaptures - approvedCaptures - rejectedCaptures,
            totalReuses);
    }
}

/// <summary>Repositório de consultas enviadas a provedores externos de IA.</summary>
internal sealed class ExternalAiConsultationRepository(ExternalAiDbContext context) : IExternalAiConsultationRepository
{
    public async Task AddAsync(ExternalAiConsultation consultation, CancellationToken ct)
    {
        await context.Consultations.AddAsync(consultation, ct);
        await context.SaveChangesAsync(ct);
    }
}

/// <summary>Repositório de políticas de governança de IA externa.</summary>
internal sealed class ExternalAiPolicyRepository(ExternalAiDbContext context) : IExternalAiPolicyRepository
{
    public async Task<ExternalAiPolicy?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Policies.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<ExternalAiPolicy>> ListActiveAsync(CancellationToken ct)
        => await context.Policies
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task AddAsync(ExternalAiPolicy policy, CancellationToken ct)
    {
        await context.Policies.AddAsync(policy, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExternalAiPolicy policy, CancellationToken ct)
    {
        context.Policies.Update(policy);
        await context.SaveChangesAsync(ct);
    }
}

/// <summary>Repositório de provedores externos de IA.</summary>
internal sealed class ExternalAiProviderRepository(ExternalAiDbContext context) : IExternalAiProviderRepository
{
    public Task<bool> ExistsAsync(ExternalAiProviderId id, CancellationToken ct)
        => context.Providers.AnyAsync(p => p.Id == id, ct);
}
