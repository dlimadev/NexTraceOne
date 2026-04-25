using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: lista evidence packs de uma versão de contrato via IContractGroundingReader.
/// </summary>
public sealed class GetEvidencePackTool : IAgentTool
{
    private readonly IContractGroundingReader _contractReader;
    private readonly ILogger<GetEvidencePackTool> _logger;

    public GetEvidencePackTool(
        IContractGroundingReader contractReader,
        ILogger<GetEvidencePackTool> logger)
    {
        _contractReader = contractReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_evidence_pack",
        "Lists contract evidence packs (compliance proofs, test results, SLA evidence) for a service or API asset.",
        "contracts",
        [
            new ToolParameterDefinition("apiAssetId", "API asset UUID to query", "string"),
            new ToolParameterDefinition("contractVersionId", "Specific contract version UUID", "string"),
            new ToolParameterDefinition("lifecycleState", "Filter by lifecycle state (e.g. Published, Deprecated)", "string"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (apiAssetId, contractVersionId, lifecycle) = ParseArgs(argumentsJson);

            _logger.LogInformation(
                "GetEvidencePackTool: apiAsset={AssetId}, contractVersion={VersionId}",
                apiAssetId, contractVersionId);

            var contracts = await _contractReader.FindContractVersionsAsync(
                contractVersionId,
                apiAssetId,
                lifecycle,
                maxResults: 20,
                ct: cancellationToken);

            var result = new
            {
                tool = "get_evidence_pack",
                apiAssetId,
                contractVersionId,
                lifecycleFilter = lifecycle,
                total = contracts.Count,
                contractVersions = contracts.Select(c => new
                {
                    c.ContractVersionId,
                    c.ApiAssetId,
                    c.Version,
                    c.Protocol,
                    c.LifecycleState,
                    c.IsLocked,
                    c.LockedAt,
                    evidenceNote = c.IsLocked
                        ? "Contract is locked — evidence pack frozen"
                        : "Contract is active — evidence pack may be updated",
                }),
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "get_evidence_pack",
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetEvidencePackTool failed");
            return new ToolExecutionResult(false, "get_evidence_pack", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static (Guid? apiAssetId, Guid? contractVersionId, string? lifecycle) ParseArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Guid? assetId = root.TryGetProperty("apiAssetId", out var a) && Guid.TryParse(a.GetString(), out var ag)
                ? ag : null;
            Guid? versionId = root.TryGetProperty("contractVersionId", out var v) && Guid.TryParse(v.GetString(), out var vg)
                ? vg : null;
            var lifecycle = root.TryGetProperty("lifecycleState", out var l) ? l.GetString() : null;

            return (assetId, versionId, lifecycle);
        }
        catch { return (null, null, null); }
    }
}
