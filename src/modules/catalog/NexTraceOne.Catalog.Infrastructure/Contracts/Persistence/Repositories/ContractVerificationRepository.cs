using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ContractVerification usando EF Core.
/// </summary>
internal sealed class ContractVerificationRepository(ServiceCatalogDbContext context)
    : IContractVerificationRepository
{
    public async Task<ContractVerification?> GetByIdAsync(
        ContractVerificationId id, CancellationToken cancellationToken)
        => await context.ContractVerifications.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ContractVerification>> ListByApiAssetAsync(
        string apiAssetId, CancellationToken cancellationToken)
        => await context.ContractVerifications
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderByDescending(v => v.VerifiedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ContractVerification>> ListByServiceAsync(
        string serviceName, int page, int pageSize, CancellationToken cancellationToken)
        => await context.ContractVerifications
            .Where(v => v.ServiceName == serviceName)
            .OrderByDescending(v => v.VerifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<ContractVerification?> GetLatestByApiAssetAsync(
        string apiAssetId, CancellationToken cancellationToken)
        => await context.ContractVerifications
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderByDescending(v => v.VerifiedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(ContractVerification verification, CancellationToken cancellationToken)
        => await context.ContractVerifications.AddAsync(verification, cancellationToken);
}
