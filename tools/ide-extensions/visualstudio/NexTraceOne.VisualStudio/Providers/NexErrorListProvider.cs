using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexTraceOne.VisualStudio.Providers;

/// <summary>
/// Integra o NexTraceOne com a Error List do Visual Studio.
/// Verifica a saúde dos serviços e contratos do workspace ao carregar a solução
/// e apresenta avisos e erros na Error List para problemas encontrados.
/// </summary>
public sealed class NexErrorListProvider : IDisposable
{
    private readonly AsyncPackage _package;
    private readonly HttpClient _httpClient;
    private readonly ErrorListProvider _errorListProvider;
    private bool _disposed;

    public NexErrorListProvider(AsyncPackage package)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _errorListProvider = new ErrorListProvider(package)
        {
            ProviderName = "NexTraceOne",
            ProviderGuid = new Guid("9e8f7a6b-5c4d-3e2f-1a0b-9c8d7e6f5a4b")
        };
    }

    /// <summary>
    /// Solicita ao servidor NexTraceOne um relatório de saúde do workspace actual.
    /// Os resultados são publicados na Error List do Visual Studio.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _errorListProvider.Tasks.Clear();

        var options = _package.GetDialogPage(typeof(NexTraceOneOptionsPage)) as NexTraceOneOptionsPage;
        if (options is null || string.IsNullOrWhiteSpace(options.ApiKey))
            return;

        List<NexGovernanceIssue> issues;
        try
        {
            issues = await FetchGovernanceIssuesAsync(
                options.ServerUrl, options.ApiKey, options.DefaultEnvironment, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // Log a single warning if the call itself fails (non-fatal)
            await AddTaskAsync(
                TaskErrorCategory.Warning,
                $"NexTraceOne: Could not retrieve governance issues — {ex.Message}",
                string.Empty, 0, 0);
            return;
        }

        foreach (var issue in issues)
        {
            await AddTaskAsync(
                issue.Severity == "error" ? TaskErrorCategory.Error : TaskErrorCategory.Warning,
                $"[NexTraceOne] {issue.ServiceName}: {issue.Message}",
                issue.FilePath ?? string.Empty,
                issue.Line,
                issue.Column);
        }
    }

    private async Task AddTaskAsync(
        TaskErrorCategory category, string text,
        string document, int line, int column)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var task = new ErrorTask
        {
            Category = TaskCategory.BuildCompile,
            ErrorCategory = category,
            Text = text,
            Document = document,
            Line = line > 0 ? line - 1 : 0,
            Column = column > 0 ? column - 1 : 0,
            HierarchyItem = null,
            Priority = category == TaskErrorCategory.Error ? TaskPriority.High : TaskPriority.Normal
        };
        task.Navigate += (s, _) =>
        {
            if (s is ErrorTask t && !string.IsNullOrWhiteSpace(t.Document))
            {
                t.Line++;
                t.Column++;
                _errorListProvider.Navigate(t, new Guid(EnvDTE.Constants.vsViewKindCode));
                t.Line--;
                t.Column--;
            }
        };

        _errorListProvider.Tasks.Add(task);
    }

    // ── API call ─────────────────────────────────────────────────────────────

    private async Task<List<NexGovernanceIssue>> FetchGovernanceIssuesAsync(
        string serverUrl, string apiKey, string environment, CancellationToken cancellationToken)
    {
        // Call the NexTraceOne governance issues endpoint for the configured environment
        var url = $"{serverUrl.TrimEnd('/')}/api/v1/governance/issues?environment={Uri.EscapeDataString(environment)}&pageSize=50";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return [];

        var body = await response.Content.ReadAsStringAsync();
        return ParseIssues(body);
    }

    private static List<NexGovernanceIssue> ParseIssues(string json)
    {
        var result = new List<NexGovernanceIssue>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var items = root.ValueKind == JsonValueKind.Array
                ? root
                : (root.TryGetProperty("items", out var arr) ? arr : default);

            if (items.ValueKind != JsonValueKind.Array) return result;

            foreach (var el in items.EnumerateArray())
            {
                result.Add(new NexGovernanceIssue
                {
                    ServiceName = GetString(el, "serviceName") ?? GetString(el, "service") ?? "unknown",
                    Message = GetString(el, "message") ?? GetString(el, "description") ?? "Governance issue",
                    Severity = GetString(el, "severity") ?? "warning",
                    FilePath = GetString(el, "filePath"),
                    Line = GetInt(el, "line"),
                    Column = GetInt(el, "column")
                });
            }
        }
        catch { /* non-fatal — return empty list */ }
        return result;
    }

    private static string? GetString(JsonElement el, string property) =>
        el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() : null;

    private static int GetInt(JsonElement el, string property) =>
        el.TryGetProperty(property, out var p) && p.TryGetInt32(out var v) ? v : 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _errorListProvider.Tasks.Clear();
        _errorListProvider.Dispose();
        _httpClient.Dispose();
    }
}

/// <summary>Representa um problema de governança reportado pelo NexTraceOne.</summary>
internal sealed class NexGovernanceIssue
{
    public string ServiceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string? FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}
