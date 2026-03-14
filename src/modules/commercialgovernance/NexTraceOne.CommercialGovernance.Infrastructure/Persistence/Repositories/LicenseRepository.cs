using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de licenças com eager loading completo do aggregate.
/// Inclui suporte a listagem paginada para vendor operations.
/// </summary>
internal sealed class LicenseRepository(LicensingDbContext context)
    : RepositoryBase<License, LicenseId>(context), ILicenseRepository
{
    public override async Task<License?> GetByIdAsync(LicenseId id, CancellationToken ct = default)
        => await IncludeGraph(context.Licenses)
            .SingleOrDefaultAsync(license => license.Id == id, ct);

    public async Task<License?> GetByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken)
        => await IncludeGraph(context.Licenses)
            .SingleOrDefaultAsync(license => license.LicenseKey == licenseKey, cancellationToken);

    /// <summary>Lista licenças com paginação para vendor ops.</summary>
    public async Task<(IReadOnlyList<License> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await context.Licenses.CountAsync(cancellationToken);

        var items = await IncludeGraph(context.Licenses)
            .OrderByDescending(l => l.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    private static IQueryable<License> IncludeGraph(IQueryable<License> query)
        => query
            .Include(license => license.HardwareBinding)
            .Include(license => license.Capabilities)
            .Include(license => license.Activations)
            .Include(license => license.UsageQuotas);
}
