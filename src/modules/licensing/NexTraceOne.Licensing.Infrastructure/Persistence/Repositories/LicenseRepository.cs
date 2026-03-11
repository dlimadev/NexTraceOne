using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;

internal sealed class LicenseRepository(LicensingDbContext context)
    : RepositoryBase<License, LicenseId>(context), ILicenseRepository
{
    public override async Task<License?> GetByIdAsync(LicenseId id, CancellationToken ct = default)
        => await IncludeGraph(context.Licenses)
            .SingleOrDefaultAsync(license => license.Id == id, ct);

    public async Task<License?> GetByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken)
        => await IncludeGraph(context.Licenses)
            .SingleOrDefaultAsync(license => license.LicenseKey == licenseKey, cancellationToken);

    private static IQueryable<License> IncludeGraph(IQueryable<License> query)
        => query
            .Include(license => license.HardwareBinding)
            .Include(license => license.Capabilities)
            .Include(license => license.Activations)
            .Include(license => license.UsageQuotas);
}
