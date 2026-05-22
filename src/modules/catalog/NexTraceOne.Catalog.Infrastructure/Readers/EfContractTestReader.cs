using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IContractTestReader.
/// Cruza ContractVerification (execuções de CI/CD) com ConsumerExpectation (CDCT)
/// para reportar cobertura de testes de contrato por par produtor-consumidor.
/// Wave AE.1 — GetContractTestCoverageReport.
/// </summary>
internal sealed class EfContractTestReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : IContractTestReader
{
    public async Task<IReadOnlyList<ContractTestEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

        var verifications = await contractsDb.ContractVerifications
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.VerifiedAt >= since)
            .ToListAsync(ct);

        if (verifications.Count == 0)
            return [];

        var apiAssetIds = verifications.Select(v => v.ApiAssetId).Distinct().ToList();

        var apiAssetGuids = apiAssetIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        var expectations = await contractsDb.ConsumerExpectations
            .AsNoTracking()
            .Where(e => e.IsActive && apiAssetGuids.Contains(e.ApiAssetId))
            .ToListAsync(ct);

        var expectationsByApi = expectations
            .GroupBy(e => e.ApiAssetId.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        var serviceNames = verifications.Select(v => v.ServiceName).Distinct().ToList();

        var tierByName = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => serviceNames.Contains(s.Name))
            .Select(s => new { s.Name, s.Tier })
            .ToDictionaryAsync(s => s.Name, s => s.Tier.ToString(), ct);

        var verifByApi = verifications
            .GroupBy(v => v.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ContractTestEntry>();

        foreach (var (apiAssetId, apiVerifs) in verifByApi)
        {
            var latest = apiVerifs.OrderByDescending(v => v.VerifiedAt).First();
            var producerName = latest.ServiceName;
            var tier = tierByName.GetValueOrDefault(producerName, "Standard");

            var total = apiVerifs.Count;
            var passed = apiVerifs.Count(v => v.Status is VerificationStatus.Pass or VerificationStatus.Warn);
            var failed = apiVerifs.Count(v => v.Status is VerificationStatus.Block or VerificationStatus.Error);
            var latestStatus = latest.Status is VerificationStatus.Pass or VerificationStatus.Warn
                ? ContractTestStatus.Passed
                : ContractTestStatus.Failed;

            if (expectationsByApi.TryGetValue(apiAssetId, out var apiExpectations))
            {
                foreach (var exp in apiExpectations)
                {
                    result.Add(new ContractTestEntry(
                        ApiAssetId: apiAssetId,
                        ProducerServiceName: producerName,
                        ConsumerServiceName: exp.ConsumerServiceName,
                        ProducerServiceTier: tier,
                        LatestStatus: latestStatus,
                        TotalExecutions: total,
                        PassedCount: passed,
                        FailedCount: failed,
                        LastTestedAt: latest.VerifiedAt));
                }
            }
            else
            {
                result.Add(new ContractTestEntry(
                    ApiAssetId: apiAssetId,
                    ProducerServiceName: producerName,
                    ConsumerServiceName: string.Empty,
                    ProducerServiceTier: tier,
                    LatestStatus: latestStatus,
                    TotalExecutions: total,
                    PassedCount: passed,
                    FailedCount: failed,
                    LastTestedAt: latest.VerifiedAt));
            }
        }

        return result
            .OrderBy(e => e.ProducerServiceName)
            .ThenBy(e => e.ConsumerServiceName)
            .ToList();
    }
}
