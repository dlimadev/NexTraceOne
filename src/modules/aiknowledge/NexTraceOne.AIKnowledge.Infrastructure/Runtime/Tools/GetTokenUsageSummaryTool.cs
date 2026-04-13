using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: retorna estatísticas de consumo de tokens de IA por utilizador, equipa ou tenant.
/// Consulta o IAiTokenUsageLedgerRepository para dados reais de governança de IA.
/// </summary>
public sealed class GetTokenUsageSummaryTool : IAgentTool
{
    private readonly IAiTokenUsageLedgerRepository _ledgerRepository;
    private readonly ILogger<GetTokenUsageSummaryTool> _logger;

    public GetTokenUsageSummaryTool(
        IAiTokenUsageLedgerRepository ledgerRepository,
        ILogger<GetTokenUsageSummaryTool> logger)
    {
        _ledgerRepository = ledgerRepository;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_token_usage_summary",
        "Retrieves AI token usage statistics for a user, team, or tenant to support governance decisions.",
        "ai_governance",
        [
            new ToolParameterDefinition("scope", "Scope of the summary: 'user', 'tenant'", "string", Required: true),
            new ToolParameterDefinition("scopeValue", "Identifier for the scope (userId or tenantId)", "string", Required: true),
            new ToolParameterDefinition("period", "Period: 'day', 'week', 'month' (default: 'week')", "string"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            if (string.IsNullOrWhiteSpace(args.Scope))
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "get_token_usage_summary", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'scope' is required. Valid values: 'user', 'tenant'.");
            }

            if (string.IsNullOrWhiteSpace(args.ScopeValue))
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "get_token_usage_summary", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'scopeValue' is required.");
            }

            _logger.LogInformation(
                "GetTokenUsageSummaryTool executing for scope={Scope}, scopeValue={ScopeValue}, period={Period}",
                args.Scope, args.ScopeValue, args.Period);

            var (periodStart, periodEnd) = ResolvePeriod(args.Period);

            long totalTokens;
            int entryCount;

            if (string.Equals(args.Scope, "user", StringComparison.OrdinalIgnoreCase))
            {
                totalTokens = await _ledgerRepository.GetTotalTokensForPeriodAsync(
                    args.ScopeValue, periodStart, periodEnd, cancellationToken);

                var entries = await _ledgerRepository.GetByUserAsync(args.ScopeValue, cancellationToken);
                entryCount = entries.Count;
            }
            else if (string.Equals(args.Scope, "tenant", StringComparison.OrdinalIgnoreCase))
            {
                if (!Guid.TryParse(args.ScopeValue, out var tenantGuid))
                {
                    sw.Stop();
                    return new ToolExecutionResult(
                        false, "get_token_usage_summary", string.Empty, sw.ElapsedMilliseconds,
                        "For scope='tenant', scopeValue must be a valid GUID (tenant identifier).");
                }

                var entries = await _ledgerRepository.GetByTenantAsync(tenantGuid, cancellationToken);
                totalTokens = entries
                    .Where(e => e.Timestamp >= periodStart && e.Timestamp <= periodEnd)
                    .Sum(e => (long)(e.PromptTokens + e.CompletionTokens));
                entryCount = entries.Count(e => e.Timestamp >= periodStart && e.Timestamp <= periodEnd);
            }
            else
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "get_token_usage_summary", string.Empty, sw.ElapsedMilliseconds,
                    $"Invalid scope '{args.Scope}'. Valid values: 'user', 'tenant'.");
            }

            var result = new
            {
                tool = "get_token_usage_summary",
                status = "executed",
                scope = args.Scope,
                scopeValue = args.ScopeValue,
                period = args.Period ?? "week",
                periodStart = periodStart,
                periodEnd = periodEnd,
                totalTokens = totalTokens,
                requestCount = entryCount,
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "get_token_usage_summary", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetTokenUsageSummaryTool failed");
            return new ToolExecutionResult(
                false, "get_token_usage_summary", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static (DateTimeOffset Start, DateTimeOffset End) ResolvePeriod(string? period)
    {
        var end = DateTimeOffset.UtcNow;
        var start = period?.ToLowerInvariant() switch
        {
            "day" => end.AddDays(-1),
            "month" => end.AddDays(-30),
            _ => end.AddDays(-7), // default: week
        };
        return (start, end);
    }

    private static TokenUsageSummaryArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new TokenUsageSummaryArgs(null, null, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var scope = root.TryGetProperty("scope", out var scopeProp) ? scopeProp.GetString() : null;
            var scopeValue = root.TryGetProperty("scopeValue", out var valProp) ? valProp.GetString() : null;
            var period = root.TryGetProperty("period", out var periodProp) ? periodProp.GetString() : null;

            return new TokenUsageSummaryArgs(scope, scopeValue, period);
        }
        catch
        {
            return new TokenUsageSummaryArgs(null, null, null);
        }
    }

    private sealed record TokenUsageSummaryArgs(string? Scope, string? ScopeValue, string? Period);
}
