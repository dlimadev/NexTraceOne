using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex health' — verifica a conectividade e saúde do servidor NexTraceOne.
/// Exit codes: 0 = healthy, 1 = degraded, 2 = unreachable.
/// </summary>
public static class HealthCommand
{
    private const int ExitHealthy = 0;
    private const int ExitDegraded = 1;
    private const int ExitUnreachable = 2;

    private static readonly System.Text.Json.JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var urlOpt = new Option<string>("--url")
        {
            Description = "NexTraceOne server URL.",
            DefaultValueFactory = _ => CliConfig.ResolveUrl(null)
        };
        var tokenOpt = new Option<string>("--token")
        {
            Description = "API authentication token."
        };
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format: text (default) or json.",
            DefaultValueFactory = _ => "text"
        };

        var command = new Command("health", "Check NexTraceOne server connectivity and health.");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await CheckHealthAsync(url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> CheckHealthAsync(
        string serverUrl, string? token, string format, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient(serverUrl, token);

        try
        {
            var response = await client.GetAsync("/api/v1/health", cancellationToken).ConfigureAwait(false);

            HealthResponse? health = null;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    health = await response.Content.ReadFromJsonAsync<HealthResponse>(
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore deserialization errors — use status code only
                }
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                var output = new
                {
                    status = health?.Status ?? (response.IsSuccessStatusCode ? "Healthy" : "Degraded"),
                    version = health?.Version,
                    serverUrl,
                    httpStatus = (int)response.StatusCode,
                    modules = health?.Modules,
                    checkedAt = DateTimeOffset.UtcNow
                };
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(output, JsonPrintOptions));
                return DetermineExitCode(health?.Status, response.IsSuccessStatusCode);
            }

            return RenderTextAndExit(serverUrl, health, response.IsSuccessStatusCode, (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = "Unreachable",
                    serverUrl,
                    error = ex.Message,
                    checkedAt = DateTimeOffset.UtcNow
                }, JsonPrintOptions));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Unreachable[/]  {serverUrl.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            }

            return ExitUnreachable;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine($"[red]✗ Timeout[/]  Could not reach {serverUrl.EscapeMarkup()} within timeout.");
            return ExitUnreachable;
        }
    }

    private static int RenderTextAndExit(string serverUrl, HealthResponse? health, bool isSuccess, int httpStatus)
    {
        var status = health?.Status ?? (isSuccess ? "Healthy" : "Degraded");
        var (icon, color) = status.ToLowerInvariant() switch
        {
            "healthy" => ("✓", "green"),
            "degraded" => ("⚠", "yellow"),
            _ => ("✗", "red")
        };

        AnsiConsole.MarkupLine($"[{color}]{icon} {status.EscapeMarkup()}[/]  {serverUrl.EscapeMarkup()}");

        if (health?.Version is not null)
            AnsiConsole.MarkupLine($"  [dim]Version:[/] {health.Version.EscapeMarkup()}");

        AnsiConsole.MarkupLine($"  [dim]HTTP Status:[/] {httpStatus}");

        if (health?.Modules is { Count: > 0 })
        {
            AnsiConsole.WriteLine();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Module[/]")
                .AddColumn("[bold]Status[/]")
                .AddColumn("[bold]Details[/]");

            foreach (var module in health.Modules)
            {
                var (mIcon, mColor) = module.Status?.ToLowerInvariant() switch
                {
                    "up" or "healthy" => ("●", "green"),
                    "degraded" => ("●", "yellow"),
                    _ => ("●", "red")
                };
                table.AddRow(
                    (module.Name ?? "-").EscapeMarkup(),
                    $"[{mColor}]{mIcon} {(module.Status ?? "-").EscapeMarkup()}[/{mColor}]",
                    (module.Details ?? string.Empty).EscapeMarkup());
            }

            AnsiConsole.Write(table);
        }

        return DetermineExitCode(health?.Status, isSuccess);
    }

    private static int DetermineExitCode(string? status, bool isSuccess)
    {
        if (!isSuccess)
            return ExitDegraded;

        return status?.ToLowerInvariant() switch
        {
            "healthy" => ExitHealthy,
            "degraded" => ExitDegraded,
            _ => isSuccess ? ExitHealthy : ExitDegraded
        };
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(10)
        };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // === DTOs ===

    private sealed class HealthResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("modules")]
        public IReadOnlyList<ModuleHealth>? Modules { get; init; }
    }

    private sealed class ModuleHealth
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("details")]
        public string? Details { get; init; }
    }
}
