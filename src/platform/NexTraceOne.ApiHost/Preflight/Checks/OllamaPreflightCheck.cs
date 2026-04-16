using Microsoft.Extensions.Configuration;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica se o Ollama (IA local) está acessível no endpoint configurado.
/// Check de aviso — não bloqueia o startup. IA features ficam indisponíveis se o check falhar.
/// </summary>
public sealed class OllamaPreflightCheck(IConfiguration configuration) : IPreflightCheck
{
    private const string CheckName = "Ollama (AI Local)";

    public async Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
        => [await ExecuteAsync(ct)];

    private async Task<PreflightCheckResult> ExecuteAsync(CancellationToken ct)
    {
        var ollamaBaseUrl = configuration["AiRuntime:Ollama:BaseUrl"] ?? "http://localhost:11434";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await http.GetAsync($"{ollamaBaseUrl}/api/tags", ct);
            return response.IsSuccessStatusCode
                ? new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Ok,
                    $"Ollama accessible at {ollamaBaseUrl}.")
                : new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    $"Ollama at {ollamaBaseUrl} returned HTTP {(int)response.StatusCode}.",
                    "Start Ollama and ensure the configured model is pulled. AI features will be unavailable.",
                    IsRequired: false);
        }
        catch (OperationCanceledException)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                "Ollama check timed out or was cancelled. AI features may be unavailable.",
                $"Ensure Ollama is running and accessible at {ollamaBaseUrl}.",
                IsRequired: false);
        }
        catch
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                $"Ollama not detected at {ollamaBaseUrl}. AI features will be unavailable.",
                $"Install and start Ollama (https://ollama.ai) to enable local AI. Not required for core platform functions.",
                IsRequired: false);
        }
    }
}
