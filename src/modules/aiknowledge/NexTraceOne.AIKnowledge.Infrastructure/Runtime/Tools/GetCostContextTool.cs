using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: retorna contexto de custo atual de IA por serviço via IAiTokenUsageLedgerRepository.
/// Agrega tokens consumidos nas últimas 24h e 30 dias para o tenant/serviço especificado.
/// </summary>
public sealed class GetCostContextTool : IAgentTool
{
    private readonly IAiTokenUsageLedgerRepository _ledgerRepository;
    private readonly ILogger<GetCostContextTool> _logger;

    public GetCostContextTool(
        IAiTokenUsageLedgerRepository ledgerRepository,
        ILogger<GetCostContextTool> logger)
    {
        _ledgerRepository = ledgerRepository;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_cost_context",
        "Returns AI token consumption costs for a service over the last 24h and 30 days.",
        "cost_intelligence",
        [
            new ToolParameterDefinition("userId", "User or service account identifier to query", "string", required: true),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var userId = ParseUserId(argumentsJson);

            _logger.LogInformation("GetCostContextTool: userId={UserId}", userId);

            var now = DateTimeOffset.UtcNow;
            var yesterday = now.AddDays(-1);
            var thirtyDaysAgo = now.AddDays(-30);

            var daily24hTask = _ledgerRepository.GetTotalTokensForPeriodAsync(userId, yesterday, now, cancellationToken);
            var monthly30dTask = _ledgerRepository.GetTotalTokensForPeriodAsync(userId, thirtyDaysAgo, now, cancellationToken);

            await Task.WhenAll(daily24hTask, monthly30dTask);

            var tokens24h = daily24hTask.Result;
            var tokens30d = monthly30dTask.Result;

            var result = new
            {
                tool = "get_cost_context",
                userId,
                period = new
                {
                    last24hTokens = tokens24h,
                    last30dTokens = tokens30d,
                    estimatedUsdLast24h = Math.Round(tokens24h * 0.000002, 4),
                    estimatedUsdLast30d = Math.Round(tokens30d * 0.000002, 4),
                    costNote = "Estimate based on $0.002 per 1k tokens (blended average). Actual cost depends on provider and model.",
                },
                simulatedNote = "Cross-module cost attribution by service/team requires FinOps module integration (AI-4.3).",
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "get_cost_context",
                JsonSerializer.Serialize(result),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetCostContextTool failed");
            return new ToolExecutionResult(false, "get_cost_context", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static string ParseUserId(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("userId", out var p) ? p.GetString() ?? "" : "";
        }
        catch { return ""; }
    }
}
