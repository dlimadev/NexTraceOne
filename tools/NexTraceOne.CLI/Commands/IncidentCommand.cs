using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex incident' — gestão e consulta de incidentes operacionais no NexTraceOne.
/// Subcomandos: list, get, report.
/// Integra com o módulo OperationalIntelligence para visibilidade de incidentes em produção.
/// </summary>
public static class IncidentCommand
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
        var command = new Command("incident", "List, inspect and report operational incidents.");
        command.Add(CreateListCommand());
        command.Add(CreateGetCommand());
        command.Add(CreateReportCommand());
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

    // ── LIST subcommand ────────────────────────────────────────────────────────

    private static Command CreateListCommand()
    {
        var serviceOpt = new Option<string>("--service", "Filter by service name or ID.");
        var statusOpt = new Option<string>("--status", "Filter by status (e.g., Open, Resolved, Investigating).");
        var severityOpt = new Option<string>("--severity", "Filter by severity (e.g., Critical, High, Medium, Low).");
        var limitOpt = new Option<int>("--limit")
        {
            Description = "Maximum number of records to return.",
            DefaultValueFactory = _ => 20
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("list", "List recent incidents from NexTraceOne.");
        command.Add(serviceOpt);
        command.Add(statusOpt);
        command.Add(severityOpt);
        command.Add(limitOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var service = parseResult.GetValue(serviceOpt);
            var status = parseResult.GetValue(statusOpt);
            var severity = parseResult.GetValue(severityOpt);
            var limit = parseResult.GetValue(limitOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ListIncidentsAsync(service, status, severity, limit, url, token, format, cancellationToken)
                .ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ListIncidentsAsync(
        string? service, string? status, string? severity, int limit,
        string url, string? token, string format, CancellationToken cancellationToken)
    {
        var queryParts = new List<string> { $"pageSize={limit}" };
        if (!string.IsNullOrWhiteSpace(service))
            queryParts.Add($"serviceId={Uri.EscapeDataString(service)}");
        if (!string.IsNullOrWhiteSpace(status))
            queryParts.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(severity))
            queryParts.Add($"severity={Uri.EscapeDataString(severity)}");

        var query = string.Join("&", queryParts);

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.GetAsync(
                $"/api/v1/incidents?{query}", cancellationToken).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            IncidentListResponse? result = null;
            try { result = JsonSerializer.Deserialize<IncidentListResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (result is null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No incidents found.[/]");
                return ExitSuccess;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Incidents[/]")
                .AddColumn("[bold]ID[/]")
                .AddColumn("[bold]Title[/]")
                .AddColumn("[bold]Service[/]")
                .AddColumn("[bold]Severity[/]")
                .AddColumn("[bold]Status[/]")
                .AddColumn("[bold]Reported At[/]");

            foreach (var item in result.Items)
            {
                var sevColor = item.Severity?.ToLowerInvariant() switch
                {
                    "critical" => "red",
                    "high" => "red",
                    "medium" => "yellow",
                    _ => "grey"
                };
                var statusColor = item.Status?.ToLowerInvariant() switch
                {
                    "open" or "investigating" => "yellow",
                    "resolved" or "closed" => "green",
                    _ => "grey"
                };

                table.AddRow(
                    item.IncidentId.ToString("D")[..8].EscapeMarkup() + "[grey]…[/]",
                    (item.Title ?? "-").EscapeMarkup(),
                    (item.ServiceName ?? "-").EscapeMarkup(),
                    $"[{sevColor}]{(item.Severity ?? "-").EscapeMarkup()}[/{sevColor}]",
                    $"[{statusColor}]{(item.Status ?? "-").EscapeMarkup()}[/{statusColor}]",
                    item.ReportedAt.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Showing {result.Items.Count} of {result.TotalCount} incident(s)[/]");
            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── GET subcommand ─────────────────────────────────────────────────────────

    private static Command CreateGetCommand()
    {
        var idArg = new Argument<string>("id")
        {
            Description = "Incident ID (GUID)."
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("get", "Get details of a specific incident.");
        command.Add(idArg);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg)!;
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await GetIncidentAsync(id, url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> GetIncidentAsync(
        string id, string url, string? token, string format, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.GetAsync(
                $"/api/v1/incidents/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                AnsiConsole.MarkupLine($"[yellow]Incident '{id.EscapeMarkup()}' not found.[/]");
                return ExitError;
            }

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var el = JsonSerializer.Deserialize<JsonElement>(body);
                    Console.WriteLine(JsonSerializer.Serialize(el, JsonPrintOptions));
                }
                catch { Console.WriteLine(body); }

                return ExitSuccess;
            }

            IncidentDetail? incident = null;
            try { incident = JsonSerializer.Deserialize<IncidentDetail>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (incident is null)
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            var sevColor = incident.Severity?.ToLowerInvariant() switch
            {
                "critical" or "high" => "red",
                "medium" => "yellow",
                _ => "grey"
            };
            var statusColor = incident.Status?.ToLowerInvariant() switch
            {
                "open" or "investigating" => "yellow",
                "resolved" or "closed" => "green",
                _ => "grey"
            };

            var grid = new Grid()
                .AddColumn(new GridColumn().PadRight(4))
                .AddColumn();

            void AddRow(string label, string? value) =>
                grid.AddRow(new Markup($"[bold]{label.EscapeMarkup()}:[/]"), new Markup((value ?? "-").EscapeMarkup()));

            AddRow("ID", incident.IncidentId.ToString("D"));
            AddRow("Title", incident.Title);
            AddRow("Service", incident.ServiceName);
            AddRow("Severity", incident.Severity);
            AddRow("Status", incident.Status);
            AddRow("Environment", incident.Environment);
            AddRow("Description", incident.Description);
            if (incident.ReportedAt != default)
                AddRow("Reported At", incident.ReportedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture));
            if (incident.ResolvedAt.HasValue)
                AddRow("Resolved At", incident.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture));
            if (incident.CorrelatedChangesCount > 0)
                AddRow("Correlated Changes", incident.CorrelatedChangesCount.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var panel = new Panel(grid)
                .Header($"[bold cyan]Incident: [{sevColor}]{(incident.Severity ?? "").EscapeMarkup()}[/{sevColor}] — [{statusColor}]{(incident.Status ?? "").EscapeMarkup()}[/{statusColor}][/]")
                .Border(BoxBorder.Rounded)
                .Expand();

            AnsiConsole.Write(panel);
            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── REPORT subcommand ──────────────────────────────────────────────────────

    private static Command CreateReportCommand()
    {
        var titleOpt = new Option<string>("--title", "Incident title.") { Required = true };
        var serviceOpt = new Option<string>("--service", "Affected service name or ID.") { Required = true };
        var severityOpt = new Option<string>("--severity")
        {
            Description = "Severity: Critical | High | Medium | Low.",
            DefaultValueFactory = _ => "Medium"
        };
        var envOpt = new Option<string>("--environment", "Affected environment (e.g., production, staging).");
        var descOpt = new Option<string>("--description", "Incident description.");
        var externalIdOpt = new Option<string>("--external-id", "External ticket/incident ID (e.g., JIRA, PagerDuty).");
        var externalSystemOpt = new Option<string>("--external-system")
        {
            Description = "Source system of the external ID (e.g., pagerduty, jira, opsgenie).",
            DefaultValueFactory = _ => "cli"
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("report", "Report a new incident to NexTraceOne.");
        command.Add(titleOpt);
        command.Add(serviceOpt);
        command.Add(severityOpt);
        command.Add(envOpt);
        command.Add(descOpt);
        command.Add(externalIdOpt);
        command.Add(externalSystemOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var title = parseResult.GetValue(titleOpt)!;
            var service = parseResult.GetValue(serviceOpt)!;
            var severity = parseResult.GetValue(severityOpt) ?? "Medium";
            var environment = parseResult.GetValue(envOpt);
            var description = parseResult.GetValue(descOpt);
            var externalId = parseResult.GetValue(externalIdOpt);
            var externalSystem = parseResult.GetValue(externalSystemOpt) ?? "cli";
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ReportIncidentAsync(title, service, severity, environment, description,
                externalId, externalSystem, url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ReportIncidentAsync(
        string title, string service, string severity, string? environment,
        string? description, string? externalId, string externalSystem,
        string url, string? token, string format, CancellationToken cancellationToken)
    {
        var payload = new
        {
            title,
            serviceId = service,
            severity,
            environment,
            description,
            externalIncidentId = externalId,
            externalSystem,
            reportedFrom = "cli",
            reportedAt = DateTimeOffset.UtcNow
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync("/api/v1/incidents", payload, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            IncidentCreatedResponse? record = null;
            try { record = JsonSerializer.Deserialize<IncidentCreatedResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            AnsiConsole.MarkupLine($"[green]✓[/] Incident reported: [bold]{title.EscapeMarkup()}[/] — [yellow]{severity.EscapeMarkup()}[/]");
            if (record?.IncidentId != null)
                AnsiConsole.MarkupLine($"  [dim]Incident ID:[/] {record.IncidentId.ToString().EscapeMarkup()}");
            if (!string.IsNullOrWhiteSpace(environment))
                AnsiConsole.MarkupLine($"  [dim]Environment:[/] {environment.EscapeMarkup()}");
            if (!string.IsNullOrWhiteSpace(externalId))
                AnsiConsole.MarkupLine($"  [dim]External ID:[/] {externalId.EscapeMarkup()} [grey]({externalSystem.EscapeMarkup()})[/]");

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
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

    private sealed class IncidentListResponse
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<IncidentListItem> Items { get; init; } = [];

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; init; }
    }

    private sealed class IncidentListItem
    {
        [JsonPropertyName("incidentId")]
        public Guid IncidentId { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("reportedAt")]
        public DateTimeOffset ReportedAt { get; init; }
    }

    private sealed class IncidentDetail
    {
        [JsonPropertyName("incidentId")]
        public Guid IncidentId { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("environment")]
        public string? Environment { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("reportedAt")]
        public DateTimeOffset ReportedAt { get; init; }

        [JsonPropertyName("resolvedAt")]
        public DateTimeOffset? ResolvedAt { get; init; }

        [JsonPropertyName("correlatedChangesCount")]
        public int CorrelatedChangesCount { get; init; }
    }

    private sealed class IncidentCreatedResponse
    {
        [JsonPropertyName("incidentId")]
        public Guid? IncidentId { get; init; }
    }
}
