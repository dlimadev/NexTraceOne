using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.AI;

/// <summary>
/// Implementação de IAiDashboardComposerService que delega ao IChatCompletionProvider do módulo AIKnowledge.
/// Quando o provider não está configurado, IsConfigured = false e o handler usa fallback keyword-based.
/// </summary>
internal sealed class AiDashboardComposerService(
    IChatCompletionProvider? chatProvider,
    ILogger<AiDashboardComposerService> logger) : IAiDashboardComposerService
{
    public bool IsConfigured => chatProvider is not null;

    public async Task<AiDashboardProposal?> ComposeAsync(
        AiDashboardCompositionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (chatProvider is null) return null;

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request);

        var completionRequest = new ChatCompletionRequest(
            ModelId: "default",
            Messages: [new ChatMessage("user", userPrompt)],
            Temperature: 0.3,
            MaxTokens: 1500,
            SystemPrompt: systemPrompt);

        ChatCompletionResult result;
        try
        {
            result = await chatProvider.CompleteAsync(completionRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI dashboard composition failed; falling back to keyword analysis");
            return null;
        }

        if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
        {
            logger.LogWarning("AI provider returned no content for dashboard composition");
            return null;
        }

        return ParseProposal(result.Content, result.ModelId, result.ProviderId);
    }

    // ── Prompt builders ──────────────────────────────────────────────────────

    private static string BuildSystemPrompt() =>
        """
        You are a dashboard design assistant. Given a user request, return a JSON object describing a dashboard.
        The JSON must follow this schema:
        {
          "title": "string",
          "layout": "grid|two-column|single-column",
          "variables": [{"key":"$var","label":"Label","type":"env|timeRange|service|team","default":"value"}],
          "widgets": [{"widgetType":"service-scorecard","title":"Title","serviceFilter":null,"nqlQuery":null,"gridX":0,"gridY":0,"gridWidth":6,"gridHeight":4}]
        }
        Available widget types: service-scorecard, slo-gauge, reliability-slo, incident-summary,
        alert-status, change-confidence, deployment-frequency, cost-trend, dora-metrics,
        top-services, team-health.
        Return ONLY the JSON object, no markdown or explanation.
        """;

    private static string BuildUserPrompt(AiDashboardCompositionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Persona: {request.Persona}");
        if (request.TeamId is not null) sb.AppendLine($"Team: {request.TeamId}");
        if (request.EnvironmentId is not null) sb.AppendLine($"Environment: {request.EnvironmentId}");
        if (request.ServiceIds is { Count: > 0 })
            sb.AppendLine($"Services: {string.Join(", ", request.ServiceIds.Take(5))}");
        sb.AppendLine($"Request: {request.Prompt}");
        return sb.ToString();
    }

    // ── Response parser ──────────────────────────────────────────────────────

    private AiDashboardProposal? ParseProposal(string content, string modelId, string providerId)
    {
        try
        {
            using var doc = JsonDocument.Parse(content.Trim());
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "AI Dashboard" : "AI Dashboard";
            var layout = root.TryGetProperty("layout", out var l) ? l.GetString() ?? "grid" : "grid";

            var variables = new List<ProposedVariable>();
            if (root.TryGetProperty("variables", out var varsEl))
            {
                foreach (var v in varsEl.EnumerateArray())
                {
                    variables.Add(new ProposedVariable(
                        v.TryGetProperty("key", out var k) ? k.GetString() ?? "$var" : "$var",
                        v.TryGetProperty("label", out var lb) ? lb.GetString() ?? "" : "",
                        v.TryGetProperty("type", out var tp) ? tp.GetString() ?? "env" : "env",
                        v.TryGetProperty("default", out var d) ? d.GetString() : null));
                }
            }

            var widgets = new List<ProposedWidget>();
            if (root.TryGetProperty("widgets", out var widgetsEl))
            {
                foreach (var w in widgetsEl.EnumerateArray())
                {
                    widgets.Add(new ProposedWidget(
                        w.TryGetProperty("widgetType", out var wt) ? wt.GetString() ?? "service-scorecard" : "service-scorecard",
                        w.TryGetProperty("title", out var wTitle) ? wTitle.GetString() : null,
                        w.TryGetProperty("serviceFilter", out var sf) ? sf.GetString() : null,
                        w.TryGetProperty("nqlQuery", out var nql) ? nql.GetString() : null,
                        w.TryGetProperty("gridX", out var gx) ? gx.GetInt32() : 0,
                        w.TryGetProperty("gridY", out var gy) ? gy.GetInt32() : 0,
                        w.TryGetProperty("gridWidth", out var gw) ? gw.GetInt32() : 6,
                        w.TryGetProperty("gridHeight", out var gh) ? gh.GetInt32() : 4));
                }
            }

            return new AiDashboardProposal(title, layout, variables, widgets, modelId, providerId);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse AI dashboard proposal JSON");
            return null;
        }
    }
}
