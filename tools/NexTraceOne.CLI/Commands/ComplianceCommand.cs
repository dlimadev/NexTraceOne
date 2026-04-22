using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex compliance' — consulta cobertura de compliance no NexTraceOne.
/// Subcomandos: check.
/// Permite que equipas de engenharia e auditores verifiquem gaps de compliance em CI/CD ou terminal.
/// </summary>
public static class ComplianceCommand
{
    private const int ExitSuccess = 0;
    private const int ExitError = 2;

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Command Create()
    {
        var command = new Command("compliance", "Check compliance coverage against known standards.");
        command.Add(CreateCheckCommand());
        return command;
    }

    private static Command CreateCheckCommand()
    {
        var standardOpt = new Option<string>("--standard")
        {
            Description = "Compliance standard to check (e.g., GDPR, SOC2, ISO27001).",
            DefaultValueFactory = _ => "GDPR"
        };
        var urlOpt = new Option<string>("--url")
        {
            Description = "NexTraceOne API base URL.",
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

        var checkCmd = new Command("check", "Check compliance coverage for a specific standard.");
        checkCmd.Add(standardOpt);
        checkCmd.Add(urlOpt);
        checkCmd.Add(tokenOpt);
        checkCmd.Add(formatOpt);

        checkCmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var standard = parseResult.GetValue(standardOpt) ?? "GDPR";
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await FetchAndDisplayCoverageAsync(standard, url, token, format, cancellationToken)
                .ConfigureAwait(false);
        });

        return checkCmd;
    }

    private static async Task<int> FetchAndDisplayCoverageAsync(
        string standard, string serverUrl, string? token, string format, CancellationToken ct)
    {
        using var client = CreateHttpClient(serverUrl, token);

        try
        {
            var response = await client
                .GetAsync($"/api/v1/compliance/coverage-matrix?standard={Uri.EscapeDataString(standard)}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]✗ API error {(int)response.StatusCode}[/]");
                return ExitError;
            }

            var coverage = await response.Content
                .ReadFromJsonAsync<CoverageResponse>(JsonReadOptions, ct)
                .ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(coverage, JsonPrintOptions));
                return ExitSuccess;
            }

            var pct = coverage?.CoveragePercent ?? 0;
            var met = coverage?.ControlsMet ?? 0;
            var total = coverage?.ControlsTotal ?? 0;
            var (icon, color) = pct >= 80 ? ("✓", "green") : pct >= 50 ? ("⚠", "yellow") : ("✗", "red");

            AnsiConsole.MarkupLine($"[{color}]{icon} {standard.EscapeMarkup()} Coverage: {pct:F1}%[/]  ({met}/{total} controls)");

            if (coverage?.Gaps is { Length: > 0 })
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Gaps:[/]");
                foreach (var gap in coverage.Gaps)
                    AnsiConsole.MarkupLine($"  [yellow]•[/] {gap.EscapeMarkup()}");
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Unreachable[/]  {serverUrl.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15)
        };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class CoverageResponse
    {
        [JsonPropertyName("standard")]
        public string? Standard { get; init; }

        [JsonPropertyName("coveragePercent")]
        public double CoveragePercent { get; init; }

        [JsonPropertyName("controlsMet")]
        public int ControlsMet { get; init; }

        [JsonPropertyName("controlsTotal")]
        public int ControlsTotal { get; init; }

        [JsonPropertyName("gaps")]
        public string[]? Gaps { get; init; }
    }
}
