using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Services;

/// <summary>
/// Implementação do correlator de eventos legacy com ativos do catálogo.
/// Utiliza ISender (MediatR) para consultar o módulo Catalog de forma desacoplada.
/// </summary>
internal sealed class LegacyEventCorrelator(
    ISender sender,
    ILogger<LegacyEventCorrelator> logger) : ILegacyEventCorrelator
{
    public async Task<CorrelationResult> CorrelateByJobNameAsync(
        string jobName, string? systemName, CancellationToken ct)
    {
        logger.LogDebug("Correlating by job name {JobName}, system {SystemName}", jobName, systemName);
        return await SearchCatalogAsync(jobName, "JobName", ct);
    }

    public async Task<CorrelationResult> CorrelateByQueueAsync(
        string? queueManagerName, string? queueName, CancellationToken ct)
    {
        var searchTerm = queueName ?? queueManagerName;
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Unmatched("QueueName", "No queue name or manager name provided");

        logger.LogDebug("Correlating by queue {QueueName}, manager {QueueManager}", queueName, queueManagerName);
        return await SearchCatalogAsync(searchTerm, "QueueName", ct);
    }

    public async Task<CorrelationResult> CorrelateByTransactionAsync(
        string? transactionId, string? systemName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return Unmatched("TransactionId", "No transaction ID provided");

        logger.LogDebug("Correlating by transaction {TransactionId}, system {SystemName}", transactionId, systemName);
        return await SearchCatalogAsync(transactionId, "TransactionId", ct);
    }

    public async Task<CorrelationResult> CorrelateByProgramNameAsync(
        string programName, string? systemName, CancellationToken ct)
    {
        logger.LogDebug("Correlating by program name {ProgramName}, system {SystemName}", programName, systemName);
        return await SearchCatalogAsync(programName, "ProgramName", ct);
    }

    public async Task<CorrelationResult> CorrelateBySystemNameAsync(
        string systemName, CancellationToken ct)
    {
        logger.LogDebug("Correlating by system name {SystemName}", systemName);
        return await SearchCatalogAsync(systemName, "SystemName", ct);
    }

    private async Task<CorrelationResult> SearchCatalogAsync(
        string searchTerm, string matchMethod, CancellationToken ct)
    {
        try
        {
            var query = new ListLegacyAssets.Query(
                TeamName: null,
                Domain: null,
                Criticality: null,
                LifecycleStatus: null,
                SearchTerm: searchTerm);

            var result = await sender.Send(query, ct);

            if (result.IsFailure)
            {
                logger.LogWarning("Catalog query failed for search term {SearchTerm}: {Error}",
                    searchTerm, result.Error);
                return Unmatched(matchMethod, $"Catalog query failed: {result.Error}");
            }

            var matched = result.Value.Items
                .FirstOrDefault(item =>
                    string.Equals(item.Name, searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.DisplayName, searchTerm, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                logger.LogDebug("No exact match found for {SearchTerm} via {MatchMethod}", searchTerm, matchMethod);
                return Unmatched(matchMethod, $"No catalog asset matched '{searchTerm}'");
            }

            return new CorrelationResult(
                IsCorrelated: true,
                AssetType: matched.AssetType,
                AssetName: matched.Name,
                AssetId: matched.Id,
                ServiceName: matched.DisplayName,
                MatchMethod: matchMethod,
                Details: $"Matched {matched.AssetType} '{matched.DisplayName}' (team: {matched.TeamName})");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error correlating by {MatchMethod} with search term {SearchTerm}",
                matchMethod, searchTerm);
            return Unmatched(matchMethod, $"Correlation error: {ex.Message}");
        }
    }

    private static CorrelationResult Unmatched(string matchMethod, string? details = null) =>
        new(IsCorrelated: false,
            AssetType: null,
            AssetName: null,
            AssetId: null,
            ServiceName: null,
            MatchMethod: matchMethod,
            Details: details);
}
