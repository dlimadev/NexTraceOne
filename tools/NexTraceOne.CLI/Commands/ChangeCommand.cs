using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex change' — gere registos de mudança e análise de blast radius.
/// Subcomandos: report, blast-radius, list.
/// Chave para pipelines CI/CD que precisam de registar e consultar mudanças em NexTraceOne.
/// </summary>
public static class ChangeCommand
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
        var command = new Command("change", "Report, inspect and list NexTraceOne change records.");
        command.Add(CreateReportCommand());
        command.Add(CreateBlastRadiusCommand());
        command.Add(CreateListCommand());
        command.Add(CreatePromoteCommand());
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

    // ── REPORT subcommand ──────────────────────────────────────────────────────

    private static Command CreateReportCommand()
    {
        var serviceOpt = new Option<string>("--service", "Service name.") { Required = true };
        var versionOpt = new Option<string>("--version", "Semantic version (e.g., 1.2.0).") { Required = true };
        var envOpt = new Option<string>("--environment", "Target environment name (e.g., production, staging).") { Required = true };
        var changeTypeOpt = new Option<string>("--type")
        {
            Description = "Change type: Deploy | ConfigChange | SchemaChange | Rollback.",
            DefaultValueFactory = _ => "Deploy"
        };
        var commitShaOpt = new Option<string>("--commit-sha", "Git commit SHA.");
        var branchOpt = new Option<string>("--branch", "Git branch name.");
        var pipelineIdOpt = new Option<string>("--pipeline-id", "CI/CD pipeline run identifier.");
        var notesOpt = new Option<string>("--notes", "Additional release notes.");
        var externalIdOpt = new Option<string>("--external-id", "External release/deployment identifier (e.g., GitHub run ID, GitLab pipeline ID).");
        var externalSystemOpt = new Option<string>("--external-system")
        {
            Description = "Source system that owns the external ID (e.g., github, gitlab, jenkins, azuredevops).",
            DefaultValueFactory = _ => "cli"
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("report", "Report a new change/deploy to NexTraceOne.");
        command.Add(serviceOpt);
        command.Add(versionOpt);
        command.Add(envOpt);
        command.Add(changeTypeOpt);
        command.Add(commitShaOpt);
        command.Add(branchOpt);
        command.Add(pipelineIdOpt);
        command.Add(notesOpt);
        command.Add(externalIdOpt);
        command.Add(externalSystemOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var service = parseResult.GetValue(serviceOpt)!;
            var version = parseResult.GetValue(versionOpt)!;
            var environment = parseResult.GetValue(envOpt)!;
            var changeType = parseResult.GetValue(changeTypeOpt) ?? "Deploy";
            var commitSha = parseResult.GetValue(commitShaOpt);
            var branch = parseResult.GetValue(branchOpt);
            var pipelineId = parseResult.GetValue(pipelineIdOpt);
            var notes = parseResult.GetValue(notesOpt);
            var externalId = parseResult.GetValue(externalIdOpt);
            var externalSystem = parseResult.GetValue(externalSystemOpt) ?? "cli";
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ReportChangeAsync(service, version, environment, changeType, commitSha, branch,
                pipelineId, notes, externalId, externalSystem, url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ReportChangeAsync(
        string service, string version, string environment, string changeType,
        string? commitSha, string? branch, string? pipelineId, string? notes,
        string? externalId, string externalSystem,
        string url, string? token, string format, CancellationToken cancellationToken)
    {
        var payload = new
        {
            serviceName = service,
            semVer = version,
            environment,
            changeType,
            commitSha,
            branch,
            pipelineRunId = pipelineId,
            releaseNotes = notes,
            externalReleaseId = externalId,
            externalSystem,
            reportedFrom = "cli",
            reportedAt = DateTimeOffset.UtcNow
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync("/api/v1/changes", payload, cancellationToken)
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

            ChangeRecord? record = null;
            try { record = JsonSerializer.Deserialize<ChangeRecord>(body, JsonReadOptions); }
            catch { /* fallback to plain output */ }

            AnsiConsole.MarkupLine($"[green]✓[/] Change recorded for [bold]{service.EscapeMarkup()}[/] v[yellow]{version.EscapeMarkup()}[/] → [blue]{environment.EscapeMarkup()}[/]");

            if (record?.ChangeId is not null)
                AnsiConsole.MarkupLine($"  [dim]Change ID:[/] {record.ChangeId.ToString().EscapeMarkup()}");
            if (record?.ConfidenceScore is not null)
                AnsiConsole.MarkupLine($"  [dim]Confidence Score:[/] {record.ConfidenceScore:F2}");

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── BLAST-RADIUS subcommand ────────────────────────────────────────────────

    private static Command CreateBlastRadiusCommand()
    {
        var serviceOpt = new Option<string>("--service", "Service name.") { Required = true };
        var versionOpt = new Option<string>("--version", "Semantic version to analyse.");
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("blast-radius", "Estimate the blast radius of a change for a service.");
        command.Add(serviceOpt);
        command.Add(versionOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var service = parseResult.GetValue(serviceOpt)!;
            var version = parseResult.GetValue(versionOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await GetBlastRadiusAsync(service, version, url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> GetBlastRadiusAsync(
        string service, string? version, string url, string? token, string format, CancellationToken cancellationToken)
    {
        var query = $"service={Uri.EscapeDataString(service)}";
        if (!string.IsNullOrWhiteSpace(version))
            query += $"&version={Uri.EscapeDataString(version)}";

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.GetAsync(
                $"/api/v1/changes/blast-radius?{query}", cancellationToken).ConfigureAwait(false);

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

            BlastRadiusResult? result = null;
            try { result = JsonSerializer.Deserialize<BlastRadiusResult>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (result is null)
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            var riskColor = result.RiskLevel?.ToLowerInvariant() switch
            {
                "high" or "critical" => "red",
                "medium" => "yellow",
                _ => "green"
            };

            AnsiConsole.MarkupLine($"\n  [bold]Blast Radius:[/] [bold]{service.EscapeMarkup()}[/]{(version is not null ? $" v{version.EscapeMarkup()}" : "")}");
            AnsiConsole.MarkupLine($"  Risk Level:     [{riskColor}]{(result.RiskLevel ?? "Unknown").EscapeMarkup()}[/{riskColor}]");
            AnsiConsole.MarkupLine($"  Affected Svcs:  {result.AffectedServicesCount}");
            AnsiConsole.MarkupLine($"  Consumers:      {result.ConsumerCount}");
            AnsiConsole.MarkupLine($"  Contract Deps:  {result.ContractDependencyCount}");

            if (result.AffectedServices?.Count > 0)
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Affected Services[/]")
                    .AddColumn("[bold]Service[/]")
                    .AddColumn("[bold]Dependency Type[/]")
                    .AddColumn("[bold]Risk[/]");

                foreach (var svc in result.AffectedServices)
                {
                    var depRisk = svc.Risk?.ToLowerInvariant() switch
                    {
                        "high" or "critical" => $"[red]{(svc.Risk ?? "-").EscapeMarkup()}[/]",
                        "medium" => $"[yellow]{(svc.Risk ?? "-").EscapeMarkup()}[/]",
                        _ => (svc.Risk ?? "-").EscapeMarkup()
                    };
                    table.AddRow(
                        (svc.Name ?? "-").EscapeMarkup(),
                        (svc.DependencyType ?? "-").EscapeMarkup(),
                        depRisk);
                }

                AnsiConsole.WriteLine();
                AnsiConsole.Write(table);
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── LIST subcommand ────────────────────────────────────────────────────────

    private static Command CreateListCommand()
    {
        var serviceOpt = new Option<string>("--service", "Filter by service name.");
        var envOpt = new Option<string>("--environment", "Filter by environment.");
        var limitOpt = new Option<int>("--limit")
        {
            Description = "Maximum number of records to return.",
            DefaultValueFactory = _ => 20
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("list", "List recent change records from NexTraceOne.");
        command.Add(serviceOpt);
        command.Add(envOpt);
        command.Add(limitOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var service = parseResult.GetValue(serviceOpt);
            var environment = parseResult.GetValue(envOpt);
            var limit = parseResult.GetValue(limitOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ListChangesAsync(service, environment, limit, url, token, format, cancellationToken)
                .ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ListChangesAsync(
        string? service, string? environment, int limit,
        string url, string? token, string format, CancellationToken cancellationToken)
    {
        var queryParts = new List<string> { $"pageSize={limit}" };
        if (!string.IsNullOrWhiteSpace(service))
            queryParts.Add($"service={Uri.EscapeDataString(service)}");
        if (!string.IsNullOrWhiteSpace(environment))
            queryParts.Add($"environment={Uri.EscapeDataString(environment)}");

        var query = string.Join("&", queryParts);

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.GetAsync(
                $"/api/v1/changes?{query}", cancellationToken).ConfigureAwait(false);

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

            ChangeListResponse? result = null;
            try { result = JsonSerializer.Deserialize<ChangeListResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (result is null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No change records found.[/]");
                return ExitSuccess;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Change Records[/]")
                .AddColumn("[bold]Service[/]")
                .AddColumn("[bold]Version[/]")
                .AddColumn("[bold]Environment[/]")
                .AddColumn("[bold]Type[/]")
                .AddColumn("[bold]Confidence[/]")
                .AddColumn("[bold]Reported At[/]");

            foreach (var item in result.Items)
            {
                var confColor = item.ConfidenceScore switch
                {
                    >= 0.8 => "green",
                    >= 0.5 => "yellow",
                    _ => "red"
                };
                var confDisplay = item.ConfidenceScore > 0
                    ? $"[{confColor}]{item.ConfidenceScore:P0}[/{confColor}]"
                    : "[grey]N/A[/]";

                table.AddRow(
                    (item.ServiceName ?? "-").EscapeMarkup(),
                    (item.SemVer ?? "-").EscapeMarkup(),
                    (item.Environment ?? "-").EscapeMarkup(),
                    (item.ChangeType ?? "-").EscapeMarkup(),
                    confDisplay,
                    item.ReportedAt.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Showing {result.Items.Count} of {result.TotalCount} record(s)[/]");
            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── HTTP helper ────────────────────────────────────────────────────────────

    private static Command CreatePromoteCommand()
    {
        var releaseIdOpt = new Option<string>("--release-id", "The release/change ID (UUID) to promote.") { Required = true };
        var targetEnvOpt = new Option<string>("--target-environment", "Target environment to promote to (e.g., staging, production).") { Required = true };
        var justificationOpt = new Option<string>("--justification", "Justification/notes for the promotion.");
        var requestedByOpt = new Option<string>("--requested-by", "Identity of the requester (defaults to current user).");
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("promote", "Create a promotion request to move a release to the next environment.");
        command.Add(releaseIdOpt);
        command.Add(targetEnvOpt);
        command.Add(justificationOpt);
        command.Add(requestedByOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var releaseId = parseResult.GetValue(releaseIdOpt)!;
            var targetEnv = parseResult.GetValue(targetEnvOpt)!;
            var justification = parseResult.GetValue(justificationOpt);
            var requestedBy = parseResult.GetValue(requestedByOpt) ?? System.Environment.UserName;
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await PromoteReleaseAsync(releaseId, targetEnv, justification, requestedBy, url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> PromoteReleaseAsync(
        string releaseId, string targetEnvironment,
        string? justification, string requestedBy,
        string url, string? token, string format,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            releaseId,
            targetEnvironment,
            justification,
            requestedBy,
            source = "cli"
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync(
                "/api/v1/promotion/requests", payload, cancellationToken).ConfigureAwait(false);
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

            PromotionResponse? result = null;
            try { result = JsonSerializer.Deserialize<PromotionResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            AnsiConsole.MarkupLine($"[green]✓[/] Promotion request created for release [bold]{releaseId.EscapeMarkup()}[/] → [yellow]{targetEnvironment.EscapeMarkup()}[/]");

            if (result?.PromotionRequestId is not null)
                AnsiConsole.MarkupLine($"  [dim]Request ID:[/] {result.PromotionRequestId.EscapeMarkup()}");
            if (!string.IsNullOrWhiteSpace(result?.Status))
                AnsiConsole.MarkupLine($"  [dim]Status:[/] {result.Status.EscapeMarkup()}");

            AnsiConsole.WriteLine();
            return ExitSuccess;
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

    private sealed class PromotionResponse
    {
        [JsonPropertyName("promotionRequestId")]
        public string? PromotionRequestId { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }

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

    private sealed class ChangeRecord
    {
        [JsonPropertyName("changeId")]
        public Guid? ChangeId { get; init; }

        [JsonPropertyName("confidenceScore")]
        public double ConfidenceScore { get; init; }
    }

    private sealed class ChangeListResponse
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<ChangeListItem> Items { get; init; } = [];

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; init; }
    }

    private sealed class ChangeListItem
    {
        [JsonPropertyName("changeId")]
        public Guid ChangeId { get; init; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        [JsonPropertyName("semVer")]
        public string? SemVer { get; init; }

        [JsonPropertyName("environment")]
        public string? Environment { get; init; }

        [JsonPropertyName("changeType")]
        public string? ChangeType { get; init; }

        [JsonPropertyName("confidenceScore")]
        public double ConfidenceScore { get; init; }

        [JsonPropertyName("reportedAt")]
        public DateTimeOffset ReportedAt { get; init; }
    }

    private sealed class BlastRadiusResult
    {
        [JsonPropertyName("riskLevel")]
        public string? RiskLevel { get; init; }

        [JsonPropertyName("affectedServicesCount")]
        public int AffectedServicesCount { get; init; }

        [JsonPropertyName("consumerCount")]
        public int ConsumerCount { get; init; }

        [JsonPropertyName("contractDependencyCount")]
        public int ContractDependencyCount { get; init; }

        [JsonPropertyName("affectedServices")]
        public IReadOnlyList<AffectedService>? AffectedServices { get; init; }
    }

    private sealed class AffectedService
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("dependencyType")]
        public string? DependencyType { get; init; }

        [JsonPropertyName("risk")]
        public string? Risk { get; init; }
    }
}
