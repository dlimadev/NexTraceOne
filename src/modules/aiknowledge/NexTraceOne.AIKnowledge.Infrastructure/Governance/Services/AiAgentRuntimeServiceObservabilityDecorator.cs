using System.Diagnostics;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Observability.Metrics;
using NexTraceOne.BuildingBlocks.Observability.Tracing;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Decorator de observabilidade para <see cref="IAiAgentRuntimeService"/>.
/// Emite spans OpenTelemetry e métricas para cada execução de agente.
/// </summary>
public sealed class AiAgentRuntimeServiceObservabilityDecorator : IAiAgentRuntimeService
{
    private readonly IAiAgentRuntimeService _inner;

    public AiAgentRuntimeServiceObservabilityDecorator(IAiAgentRuntimeService inner)
    {
        _inner = inner;
    }

    public async Task<Result<AgentExecutionResult>> ExecuteAsync(
        AiAgentId agentId,
        string input,
        Guid? modelIdOverride,
        string? contextJson,
        string? callerTeamId,
        CancellationToken cancellationToken)
    {
        using var activity = NexTraceActivitySources.AIKnowledge.StartActivity("AiAgentRuntimeService.ExecuteAsync");
        activity?.SetTag("agent.id", agentId.Value);
        activity?.SetTag("agent.input_length", input.Length);

        var sw = Stopwatch.StartNew();
        var result = await _inner.ExecuteAsync(agentId, input, modelIdOverride, contextJson, callerTeamId, cancellationToken);
        sw.Stop();

        var status = result.IsSuccess && result.Value.Status == "Completed" ? "success" : "failure";
        var tags = new KeyValuePair<string, object?>[]
        {
            new("agent.id", agentId.Value.ToString()),
            new("status", status)
        };

        NexTraceMeters.AiRequestsTotal.Add(1, tags);
        NexTraceMeters.AiRequestDuration.Record(sw.ElapsedMilliseconds, tags);

        if (result.IsSuccess)
        {
            var r = result.Value;
            var tokenTags = tags.Append(new KeyValuePair<string, object?>("token.type", "prompt")).ToArray();
            var completionTags = tags.Append(new KeyValuePair<string, object?>("token.type", "completion")).ToArray();

            NexTraceMeters.AiTokensTotal.Add(r.PromptTokens, tokenTags);
            NexTraceMeters.AiTokensTotal.Add(r.CompletionTokens, completionTags);

            var estimatedCost = (r.PromptTokens + r.CompletionTokens) * 0.000002;
            NexTraceMeters.AiRequestCostUsd.Record(estimatedCost, tags);

            activity?.SetTag("tokens.prompt", r.PromptTokens);
            activity?.SetTag("tokens.completion", r.CompletionTokens);
            activity?.SetTag("tokens.total", r.PromptTokens + r.CompletionTokens);
            activity?.SetTag("duration.ms", sw.ElapsedMilliseconds);
            activity?.SetTag("success", true);
        }
        else
        {
            activity?.SetTag("success", false);
            activity?.SetTag("error.code", result.Error.Code);
        }

        return result;
    }
}
