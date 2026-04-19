using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex report' — geração de relatórios operacionais e de engenharia.
/// Subcomandos: dora, changes-summary.
/// Expõe os endpoints de métricas DORA e sumário de mudanças ao pipeline CI/CD e terminais.
/// </summary>
public static class ReportCommand
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
        var command = new Command("report", "Generate operational and engineering reports from NexTraceOne.");
        command.Add(CreateDoraCommand());
        command.Add(CreateChangesSummaryCommand());
        return command;
    }

    // ── Shared option factories ────────────────────────────────────────────────

    private static Option<string> CreateUrlOption() => new("--url")
    {
        Description = "NexTraceOne API base URL.",
        DefaultValueFactory = _ => CliConfig.ResolveUrl(null)
    };

    private static Option<string> CreateTokenOption() => new("--token")
    {
        Description = "API authentication token (or set NEXTRACE_TOKEN env var)."
    };

    private static Option<string> CreateFormatOption() => new("--format")
    {
        Description = "Output format: text (default) or json.",
        DefaultValueFactory = _ => "text"
    };

    // ── DORA subcommand ────────────────────────────────────────────────────────

    private static Command CreateDoraCommand()
    {
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();
        var serviceOpt = new Option<string>("--service", "Filter by service name.");
        var teamOpt = new Option<string>("--team", "Filter by team name.");
        var envOpt = new Option<string>("--environment", "Filter by environment name.");
        var fromOpt = new Option<DateTimeOffset?>("--from", "Start of the time window (ISO 8601).");
        var toOpt = new Option<DateTimeOffset?>("--to", "End of the time window (ISO 8601).");

        var command = new Command("dora", "Display DORA metrics (Deployment Frequency, Lead Time, MTTR, Change Failure Rate).");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);
        command.Add(serviceOpt);
        command.Add(teamOpt);
        command.Add(envOpt);
        command.Add(fromOpt);
        command.Add(toOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var service = parseResult.GetValue(serviceOpt);
            var team = parseResult.GetValue(teamOpt);
            var env = parseResult.GetValue(envOpt) ?? CliConfig.ResolveEnvironment(null);
            var from = parseResult.GetValue(fromOpt);
            var to = parseResult.GetValue(toOpt);

            return await GetDoraAsync(url, token, format, service, team, env, from, to, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> GetDoraAsync(
        string apiUrl, string? token, string format,
        string? service, string? team, string? environment,
        DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(apiUrl, token);

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(service)) queryParams.Add($"serviceName={Uri.EscapeDataString(service)}");
            if (!string.IsNullOrWhiteSpace(team)) queryParams.Add($"teamName={Uri.EscapeDataString(team)}");
            if (!string.IsNullOrWhiteSpace(environment)) queryParams.Add($"environment={Uri.EscapeDataString(environment)}");
            if (from.HasValue) queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
            if (to.HasValue) queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            var response = await client.GetAsync($"/api/v1/changes/dora-metrics{query}", cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var element = JsonSerializer.Deserialize<JsonElement>(body, JsonReadOptions);
                    Console.WriteLine(JsonSerializer.Serialize(element, JsonPrintOptions));
                }
                catch { Console.WriteLine(body); }
                return ExitSuccess;
            }

            DoraMetricsResponse? metrics = null;
            try { metrics = JsonSerializer.Deserialize<DoraMetricsResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (metrics is null)
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            var title = "DORA Metrics";
            if (!string.IsNullOrWhiteSpace(service)) title += $" — {service}";
            else if (!string.IsNullOrWhiteSpace(team)) title += $" — {team}";

            AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").LeftJustified());
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Metric[/]"))
                .AddColumn(new TableColumn("[bold]Value[/]"))
                .AddColumn(new TableColumn("[bold]Level[/]"));

            AddDoraRow(table, "Deployment Frequency", metrics.DeploymentFrequency, metrics.DeploymentFrequencyLevel);
            AddDoraRow(table, "Lead Time for Changes", metrics.LeadTimeForChanges, metrics.LeadTimeLevel);
            AddDoraRow(table, "MTTR", metrics.MeanTimeToRestore, metrics.MttrLevel);
            AddDoraRow(table, "Change Failure Rate", metrics.ChangeFailureRate, metrics.ChangeFailureRateLevel);

            AnsiConsole.Write(table);

            if (!string.IsNullOrWhiteSpace(metrics.OverallPerformance))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Overall DORA Performance:[/] {FormatDoraLevel(metrics.OverallPerformance)}");
            }

            if (metrics.PeriodStart.HasValue && metrics.PeriodEnd.HasValue)
            {
                AnsiConsole.MarkupLine(
                    $"[grey]Period: {metrics.PeriodStart.Value:yyyy-MM-dd} → {metrics.PeriodEnd.Value:yyyy-MM-dd}[/]");
            }

            AnsiConsole.WriteLine();
            return ExitSuccess;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            return ExitError;
        }
    }

    private static void AddDoraRow(Table table, string label, string? value, string? level)
    {
        table.AddRow(
            label.EscapeMarkup(),
            (value ?? "-").EscapeMarkup(),
            FormatDoraLevel(level));
    }

    private static string FormatDoraLevel(string? level) =>
        level?.ToLowerInvariant() switch
        {
            "elite" => "[bold green]Elite[/]",
            "high" => "[green]High[/]",
            "medium" => "[yellow]Medium[/]",
            "low" => "[red]Low[/]",
            _ => (level ?? "-").EscapeMarkup()
        };

    // ── CHANGES-SUMMARY subcommand ─────────────────────────────────────────────

    private static Command CreateChangesSummaryCommand()
    {
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();
        var teamOpt = new Option<string>("--team", "Filter by team name.");
        var envOpt = new Option<string>("--environment", "Filter by environment name.");
        var fromOpt = new Option<DateTimeOffset?>("--from", "Start of the time window (ISO 8601).");
        var toOpt = new Option<DateTimeOffset?>("--to", "End of the time window (ISO 8601).");

        var command = new Command("changes-summary", "Display an aggregated summary of changes (counts by type, status, environment).");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);
        command.Add(teamOpt);
        command.Add(envOpt);
        command.Add(fromOpt);
        command.Add(toOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var team = parseResult.GetValue(teamOpt);
            var env = parseResult.GetValue(envOpt) ?? CliConfig.ResolveEnvironment(null);
            var from = parseResult.GetValue(fromOpt);
            var to = parseResult.GetValue(toOpt);

            return await GetChangesSummaryAsync(url, token, format, team, env, from, to, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> GetChangesSummaryAsync(
        string apiUrl, string? token, string format,
        string? team, string? environment,
        DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(apiUrl, token);

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(team)) queryParams.Add($"teamName={Uri.EscapeDataString(team)}");
            if (!string.IsNullOrWhiteSpace(environment)) queryParams.Add($"environment={Uri.EscapeDataString(environment)}");
            if (from.HasValue) queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
            if (to.HasValue) queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            var response = await client.GetAsync($"/api/v1/changes/summary{query}", cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var element = JsonSerializer.Deserialize<JsonElement>(body, JsonReadOptions);
                    Console.WriteLine(JsonSerializer.Serialize(element, JsonPrintOptions));
                }
                catch { Console.WriteLine(body); }
                return ExitSuccess;
            }

            ChangesSummaryResponse? summary = null;
            try { summary = JsonSerializer.Deserialize<ChangesSummaryResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (summary is null)
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            AnsiConsole.Write(new Rule("[bold cyan]Changes Summary[/]").LeftJustified());
            AnsiConsole.WriteLine();

            var overviewTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Overview[/]")
                .AddColumn("[bold]Metric[/]")
                .AddColumn(new TableColumn("[bold]Count[/]").RightAligned());

            overviewTable.AddRow("Total Changes", summary.TotalChanges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            overviewTable.AddRow("[green]Successful[/]", summary.SuccessfulChanges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            overviewTable.AddRow("[red]Failed[/]", summary.FailedChanges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            overviewTable.AddRow("[yellow]Pending[/]", summary.PendingChanges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            overviewTable.AddRow("[blue]Rollbacks[/]", summary.Rollbacks.ToString(System.Globalization.CultureInfo.InvariantCulture));

            AnsiConsole.Write(overviewTable);

            if (summary.ByEnvironment is { Count: > 0 })
            {
                AnsiConsole.WriteLine();
                var envTable = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]By Environment[/]")
                    .AddColumn("[bold]Environment[/]")
                    .AddColumn(new TableColumn("[bold]Changes[/]").RightAligned());

                foreach (var kv in summary.ByEnvironment)
                    envTable.AddRow(kv.Key.EscapeMarkup(), kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                AnsiConsole.Write(envTable);
            }

            AnsiConsole.WriteLine();
            return ExitSuccess;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            return ExitError;
        }
    }

    // ── HTTP helper ────────────────────────────────────────────────────────────

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── DTOs ────────────────────────────────────────────────────────────────────

    private sealed class DoraMetricsResponse
    {
        [JsonPropertyName("deploymentFrequency")]
        public string? DeploymentFrequency { get; init; }

        [JsonPropertyName("deploymentFrequencyLevel")]
        public string? DeploymentFrequencyLevel { get; init; }

        [JsonPropertyName("leadTimeForChanges")]
        public string? LeadTimeForChanges { get; init; }

        [JsonPropertyName("leadTimeLevel")]
        public string? LeadTimeLevel { get; init; }

        [JsonPropertyName("meanTimeToRestore")]
        public string? MeanTimeToRestore { get; init; }

        [JsonPropertyName("mttrLevel")]
        public string? MttrLevel { get; init; }

        [JsonPropertyName("changeFailureRate")]
        public string? ChangeFailureRate { get; init; }

        [JsonPropertyName("changeFailureRateLevel")]
        public string? ChangeFailureRateLevel { get; init; }

        [JsonPropertyName("overallPerformance")]
        public string? OverallPerformance { get; init; }

        [JsonPropertyName("periodStart")]
        public DateTimeOffset? PeriodStart { get; init; }

        [JsonPropertyName("periodEnd")]
        public DateTimeOffset? PeriodEnd { get; init; }
    }

    private sealed class ChangesSummaryResponse
    {
        [JsonPropertyName("totalChanges")]
        public int TotalChanges { get; init; }

        [JsonPropertyName("successfulChanges")]
        public int SuccessfulChanges { get; init; }

        [JsonPropertyName("failedChanges")]
        public int FailedChanges { get; init; }

        [JsonPropertyName("pendingChanges")]
        public int PendingChanges { get; init; }

        [JsonPropertyName("rollbacks")]
        public int Rollbacks { get; init; }

        [JsonPropertyName("byEnvironment")]
        public IReadOnlyDictionary<string, int>? ByEnvironment { get; init; }
    }
}
