using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comandos de verificação de conformidade de contratos.
/// Subcomandos: verify, diff, changelog, sync.
/// </summary>
public static class ContractCommand
{
    private const int ExitSuccess = 0;
    private const int ExitVerificationFailed = 1;
    private const int ExitError = 2;
    private const int ExitContractNotFound = 3;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var command = new Command("contract", "Contract compliance verification commands.");
        command.Add(CreateVerifyCommand());
        command.Add(CreateDiffCommand());
        command.Add(CreateChangelogCommand());
        command.Add(CreateSyncCommand());
        return command;
    }

    // === Shared option factories ===

    private static Option<string> CreateUrlOption() => new("--url")
    {
        Description = "NexTraceOne API base URL.",
        DefaultValueFactory = _ => Services.CliConfig.ResolveUrl(null)
    };

    private static Option<string> CreateFormatOption() => new("--format")
    {
        Description = "Output format: text, json, junit.",
        DefaultValueFactory = _ => "text"
    };

    private static Option<string> CreateTokenOption() => new("--token")
    {
        Description = "API authentication token (or set NEXTRACE_TOKEN env var)."
    };

    // === VERIFY subcommand ===

    private static Command CreateVerifyCommand()
    {
        var specOpt = new Option<FileInfo>("--spec", "Path to the spec file (OpenAPI, WSDL, AsyncAPI).") { Required = true };
        var serviceOpt = new Option<string>("--service", "Service name in NexTraceOne.") { Required = true };
        var urlOpt = CreateUrlOption();
        var apiAssetOpt = new Option<string>("--api-asset-id", "API asset identifier (defaults to service name).");
        var envOpt = new Option<string>("--environment", "Target environment name.");
        var sourceSystemOpt = new Option<string>("--source-system") { DefaultValueFactory = _ => "cli", Description = "Source system identifier." };
        var commitShaOpt = new Option<string>("--commit-sha", "Git commit SHA.");
        var branchOpt = new Option<string>("--branch", "Git branch name.");
        var pipelineIdOpt = new Option<string>("--pipeline-id", "CI/CD pipeline run identifier.");
        var formatOpt = CreateFormatOption();
        var tokenOpt = CreateTokenOption();
        var strictOpt = new Option<bool>("--strict", "Follow server-side compliance policy.");
        var failOnBreakingOpt = new Option<bool>("--fail-on-breaking", "Exit 1 on breaking changes.");
        var failOnAnyOpt = new Option<bool>("--fail-on-any-change", "Exit 1 on any change.");
        var failOnMissingOpt = new Option<bool>("--fail-on-missing", "Exit 1 if contract not found.");
        var dryRunOpt = new Option<bool>("--dry-run", "Don't persist results.");
        var outputOpt = new Option<string>("--output", "Output file path.");

        var command = new Command("verify", "Verify contract compliance against NexTraceOne.");
        command.Add(specOpt);
        command.Add(serviceOpt);
        command.Add(urlOpt);
        command.Add(apiAssetOpt);
        command.Add(envOpt);
        command.Add(sourceSystemOpt);
        command.Add(commitShaOpt);
        command.Add(branchOpt);
        command.Add(pipelineIdOpt);
        command.Add(formatOpt);
        command.Add(tokenOpt);
        command.Add(strictOpt);
        command.Add(failOnBreakingOpt);
        command.Add(failOnAnyOpt);
        command.Add(failOnMissingOpt);
        command.Add(dryRunOpt);
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var spec = parseResult.GetValue(specOpt)!;
            var service = parseResult.GetValue(serviceOpt)!;
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var apiAssetId = parseResult.GetValue(apiAssetOpt) ?? service;
            var env = parseResult.GetValue(envOpt);
            var sourceSystem = parseResult.GetValue(sourceSystemOpt) ?? "cli";
            var commitSha = parseResult.GetValue(commitShaOpt);
            var branch = parseResult.GetValue(branchOpt);
            var pipelineId = parseResult.GetValue(pipelineIdOpt);
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var strict = parseResult.GetValue(strictOpt);
            var failOnBreaking = parseResult.GetValue(failOnBreakingOpt);
            var failOnAny = parseResult.GetValue(failOnAnyOpt);
            var failOnMissing = parseResult.GetValue(failOnMissingOpt);
            var dryRun = parseResult.GetValue(dryRunOpt);
            var output = parseResult.GetValue(outputOpt);

            return await VerifyAsync(spec, service, url, apiAssetId, env, sourceSystem, commitSha, branch,
                pipelineId, format, token, strict, failOnBreaking, failOnAny, failOnMissing, dryRun, output,
                cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> VerifyAsync(
        FileInfo spec, string service, string url, string apiAssetId,
        string? environment, string sourceSystem, string? commitSha, string? branch,
        string? pipelineId, string format, string? token,
        bool strict, bool failOnBreaking, bool failOnAny, bool failOnMissing,
        bool dryRun, string? output, CancellationToken cancellationToken)
    {
        if (!spec.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Spec file not found: [yellow]{spec.FullName.EscapeMarkup()}[/]");
            return ExitError;
        }

        string specContent;
        try
        {
            specContent = await File.ReadAllTextAsync(spec.FullName, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot read spec file: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }

        var specFormat = DetectSpecFormat(spec.Name, specContent);

        var payload = new
        {
            ApiAssetId = apiAssetId,
            ServiceName = service,
            SpecContent = specContent,
            SpecFormat = specFormat,
            SourceSystem = sourceSystem,
            SourceBranch = branch,
            CommitSha = commitSha,
            PipelineId = pipelineId,
            EnvironmentName = environment,
            DryRun = dryRun
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync(
                "/api/v1/contracts/verifications", payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{errorBody.EscapeMarkup()}[/]");
                return ExitError;
            }

            var result = await response.Content.ReadFromJsonAsync<VerificationResult>(
                JsonReadOptions, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            var outputContent = RenderVerification(result, format);
            if (output is not null)
                await File.WriteAllTextAsync(output, outputContent, cancellationToken).ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine(outputContent);
            else
                RenderVerificationText(result);

            if (string.Equals(result.Status, "Error", StringComparison.OrdinalIgnoreCase))
                return failOnMissing ? ExitContractNotFound : ExitSuccess;

            if (string.Equals(result.Status, "Block", StringComparison.OrdinalIgnoreCase))
                return ExitVerificationFailed;

            if (failOnBreaking && result.BreakingChangesCount > 0)
                return ExitVerificationFailed;

            if (failOnAny && (result.BreakingChangesCount > 0 || result.NonBreakingChangesCount > 0 || result.AdditiveChangesCount > 0))
                return ExitVerificationFailed;

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // === DIFF subcommand ===

    private static Command CreateDiffCommand()
    {
        var specOpt = new Option<FileInfo>("--spec", "Path to local spec file.");
        var serviceOpt = new Option<string>("--service", "Service name.") { Required = true };
        var urlOpt = CreateUrlOption();
        var fromOpt = new Option<string>("--from", "Source version (e.g., 1.2.0).");
        var toOpt = new Option<string>("--to", "Target version (e.g., 1.3.0).");
        var formatOpt = CreateFormatOption();
        var tokenOpt = CreateTokenOption();
        var outputOpt = new Option<string>("--output", "Output file path.");

        var command = new Command("diff", "Compare contract versions or local spec against NexTraceOne.");
        command.Add(specOpt);
        command.Add(serviceOpt);
        command.Add(urlOpt);
        command.Add(fromOpt);
        command.Add(toOpt);
        command.Add(formatOpt);
        command.Add(tokenOpt);
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var spec = parseResult.GetValue(specOpt);
            var service = parseResult.GetValue(serviceOpt)!;
            var from = parseResult.GetValue(fromOpt);
            var to = parseResult.GetValue(toOpt);
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var output = parseResult.GetValue(outputOpt);

            return await DiffAsync(spec, service, from, to, url, token, format, output, cancellationToken)
                .ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> DiffAsync(
        FileInfo? spec, string service, string? from, string? to,
        string url, string? token, string format, string? output,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(url, token);

            DiffResult? result;

            if (spec is not null)
            {
                // POST local spec for inline diff
                if (!spec.Exists)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Spec file not found: [yellow]{spec.FullName.EscapeMarkup()}[/]");
                    return ExitError;
                }

                var specContent = await File.ReadAllTextAsync(spec.FullName, cancellationToken).ConfigureAwait(false);
                var specFormat = DetectSpecFormat(spec.Name, specContent);

                var payload = new
                {
                    apiAssetId = service,
                    specContent,
                    specFormat,
                    fromVersion = from,
                    toVersion = to
                };

                var response = await client.PostAsJsonAsync("/api/v1/contracts/diff", payload, cancellationToken)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                    return ExitError;
                }

                result = JsonSerializer.Deserialize<DiffResult>(body, JsonReadOptions);
            }
            else
            {
                // GET version-to-version diff
                var query = $"service={Uri.EscapeDataString(service)}";
                if (!string.IsNullOrWhiteSpace(from))
                    query += $"&from={Uri.EscapeDataString(from)}";
                if (!string.IsNullOrWhiteSpace(to))
                    query += $"&to={Uri.EscapeDataString(to)}";

                var response = await client.GetAsync($"/api/v1/contracts/diff?{query}", cancellationToken)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                    return ExitError;
                }

                result = JsonSerializer.Deserialize<DiffResult>(body, JsonReadOptions);
            }

            if (result is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            var serialized = JsonSerializer.Serialize(result, JsonPrintOptions);

            if (output is not null)
                await File.WriteAllTextAsync(output, serialized, cancellationToken).ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(serialized);
                return ExitSuccess;
            }

            RenderDiffText(result, service, from, to);
            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    private static void RenderDiffText(DiffResult result, string service, string? from, string? to)
    {
        var fromLabel = from ?? "baseline";
        var toLabel = to ?? "current";
        AnsiConsole.MarkupLine($"\n  [bold cyan]Contract Diff[/] — {service.EscapeMarkup()} [grey]{fromLabel.EscapeMarkup()} → {toLabel.EscapeMarkup()}[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Change[/]")
            .AddColumn("[bold]Path[/]")
            .AddColumn("[bold]Details[/]");

        foreach (var change in result.Changes ?? [])
        {
            var (icon, color) = change.ChangeType?.ToLowerInvariant() switch
            {
                "breaking" => ("[red]✗[/]", "red"),
                "non-breaking" or "nonbreaking" => ("[yellow]~[/]", "yellow"),
                "additive" or "new" => ("[green]+[/]", "green"),
                "removed" => ("[red]-[/]", "red"),
                _ => ("·", "grey")
            };
            table.AddRow(
                $"[{color}]{(change.ChangeType ?? "-").EscapeMarkup()}[/{color}]",
                icon,
                (change.Path ?? "-").EscapeMarkup(),
                (change.Description ?? "-").EscapeMarkup());
        }

        if (result.Changes?.Count > 0)
            AnsiConsole.Write(table);
        else
            AnsiConsole.MarkupLine("  [green]No differences found.[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  Breaking: [red]{result.BreakingCount}[/]  " +
            $"Non-Breaking: [yellow]{result.NonBreakingCount}[/]  " +
            $"Additive: [green]{result.AdditiveCount}[/]");
    }

    // === CHANGELOG subcommand ===

    private static Command CreateChangelogCommand()
    {
        var serviceOpt = new Option<string>("--service", "Service name.") { Required = true };
        var urlOpt = CreateUrlOption();
        var formatOpt = CreateFormatOption();
        var tokenOpt = CreateTokenOption();
        var outputOpt = new Option<string>("--output", "Output file path.");

        var command = new Command("changelog", "Generate contract changelog.");
        command.Add(serviceOpt);
        command.Add(urlOpt);
        command.Add(formatOpt);
        command.Add(tokenOpt);
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var service = parseResult.GetValue(serviceOpt)!;
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var output = parseResult.GetValue(outputOpt);

            try
            {
                using var client = CreateHttpClient(url, token);
                var response = await client.GetAsync(
                    $"/api/v1/contracts/changelogs?apiAssetId={Uri.EscapeDataString(service)}",
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}");
                    return ExitError;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (output is not null)
                    await File.WriteAllTextAsync(output, body, cancellationToken).ConfigureAwait(false);

                if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(body);
                    return ExitSuccess;
                }

                RenderChangelogText(service, body);
                return ExitSuccess;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
                return ExitError;
            }
        });

        return command;
    }

    // === SYNC subcommand ===

    private static Command CreateSyncCommand()
    {
        var specOpt = new Option<FileInfo>("--spec", "Path to spec file.") { Required = true };
        var serviceOpt = new Option<string>("--service", "Service name.") { Required = true };
        var versionOpt = new Option<string>("--version", "Semantic version (e.g., 1.3.0).") { Required = true };
        var urlOpt = CreateUrlOption();
        var sourceSystemOpt = new Option<string>("--source-system") { DefaultValueFactory = _ => "cli", Description = "Source system identifier." };
        var formatOpt = CreateFormatOption();
        var tokenOpt = CreateTokenOption();

        var command = new Command("sync", "Sync local spec to NexTraceOne (import/update).");
        command.Add(specOpt);
        command.Add(serviceOpt);
        command.Add(versionOpt);
        command.Add(urlOpt);
        command.Add(sourceSystemOpt);
        command.Add(formatOpt);
        command.Add(tokenOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var spec = parseResult.GetValue(specOpt)!;
            var service = parseResult.GetValue(serviceOpt)!;
            var version = parseResult.GetValue(versionOpt)!;
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var sourceSystem = parseResult.GetValue(sourceSystemOpt) ?? "cli";
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));

            if (!spec.Exists)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Spec file not found: [yellow]{spec.FullName.EscapeMarkup()}[/]");
                return ExitError;
            }

            var specContent = await File.ReadAllTextAsync(spec.FullName, cancellationToken).ConfigureAwait(false);
            var specFormat = DetectSpecFormat(spec.Name, specContent);

            var payload = new
            {
                Items = new[]
                {
                    new
                    {
                        ApiAssetId = service,
                        SemVer = version,
                        SpecContent = specContent,
                        Format = specFormat,
                        ImportedFrom = sourceSystem,
                        Protocol = DetectProtocol(specContent)
                    }
                },
                SourceSystem = sourceSystem,
                CorrelationId = Guid.NewGuid().ToString()
            };

            try
            {
                using var client = CreateHttpClient(url, token);
                var response = await client.PostAsJsonAsync(
                    "/api/v1/contracts/sync", payload, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{error.EscapeMarkup()}[/]");
                    return ExitError;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(body);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Contract [blue]{service.EscapeMarkup()}[/] v[yellow]{version.EscapeMarkup()}[/] synced successfully.");
                }

                return ExitSuccess;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
                return ExitError;
            }
        });

        return command;
    }

    // === Helper methods ===

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string DetectSpecFormat(string fileName, string content)
    {
        if (fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            return "yaml";
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return "json";
        if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".wsdl", StringComparison.OrdinalIgnoreCase))
            return "xml";

        var trimmed = content.TrimStart();
        if (trimmed.StartsWith('{'))
            return "json";
        if (trimmed.StartsWith('<'))
            return "xml";
        return "yaml";
    }

    private static string DetectProtocol(string content)
    {
        if (content.Contains("openapi", StringComparison.OrdinalIgnoreCase))
            return "OpenApi";
        if (content.Contains("swagger", StringComparison.OrdinalIgnoreCase))
            return "Swagger";
        if (content.Contains("asyncapi", StringComparison.OrdinalIgnoreCase))
            return "AsyncApi";
        if (content.Contains("wsdl", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("definitions", StringComparison.OrdinalIgnoreCase))
            return "Wsdl";
        return "OpenApi";
    }

    private static string RenderVerification(VerificationResult result, string format)
    {
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return JsonSerializer.Serialize(result, JsonPrintOptions);

        return JsonSerializer.Serialize(result, JsonPrintOptions);
    }

    private static void RenderVerificationText(VerificationResult result)
    {
        var statusColor = result.Status switch
        {
            "Pass" => "green",
            "Warn" => "yellow",
            "Block" => "red",
            _ => "grey"
        };

        var statusIcon = result.Status switch
        {
            "Pass" => "✅",
            "Warn" => "⚠️",
            "Block" => "❌",
            _ => "❓"
        };

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  {statusIcon} Contract Compliance: [{statusColor}]{result.Status.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  Reference Version: [blue]{(result.ContractVersionSemVer ?? "N/A").EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Type");
        table.AddColumn("Count");

        table.AddRow("Breaking Changes", result.BreakingChangesCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        table.AddRow("Non-Breaking Changes", result.NonBreakingChangesCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        table.AddRow("Additive Changes", result.AdditiveChangesCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        table.AddRow("Removed Endpoints", (result.RemovedEndpoints?.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture));
        table.AddRow("New Endpoints", (result.NewEndpoints?.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture));

        AnsiConsole.Write(table);

        if (result.RemovedEndpoints?.Count > 0)
        {
            AnsiConsole.MarkupLine("\n  [red]Removed Endpoints:[/]");
            foreach (var ep in result.RemovedEndpoints)
                AnsiConsole.MarkupLine($"    [red]- {ep.EscapeMarkup()}[/]");
        }

        if (result.NewEndpoints?.Count > 0)
        {
            AnsiConsole.MarkupLine("\n  [yellow]New Endpoints:[/]");
            foreach (var ep in result.NewEndpoints)
                AnsiConsole.MarkupLine($"    [yellow]+ {ep.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [dim]{result.Message.EscapeMarkup()}[/]");
    }

    // === DTOs ===

    private sealed record VerificationResult
    {
        public Guid VerificationId { get; init; }
        public string Status { get; init; } = string.Empty;
        public Guid? ContractVersionId { get; init; }
        public string? ContractVersionSemVer { get; init; }
        public int BreakingChangesCount { get; init; }
        public int NonBreakingChangesCount { get; init; }
        public int AdditiveChangesCount { get; init; }
        public IReadOnlyList<string>? RemovedEndpoints { get; init; }
        public IReadOnlyList<string>? NewEndpoints { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTimeOffset VerifiedAt { get; init; }
    }

    private sealed class DiffResult
    {
        [JsonPropertyName("changes")]
        public IReadOnlyList<DiffEntry>? Changes { get; init; }

        [JsonPropertyName("breakingCount")]
        public int BreakingCount { get; init; }

        [JsonPropertyName("nonBreakingCount")]
        public int NonBreakingCount { get; init; }

        [JsonPropertyName("additiveCount")]
        public int AdditiveCount { get; init; }
    }

    private sealed class DiffEntry
    {
        [JsonPropertyName("changeType")]
        public string? ChangeType { get; init; }

        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }

    private sealed class ChangelogResponse
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<ChangelogEntry>? Items { get; init; }
    }

    private sealed class ChangelogEntry
    {
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("semVer")]
        public string? SemVer { get; init; }

        [JsonPropertyName("changeType")]
        public string? ChangeType { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("publishedAt")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset? CreatedAt { get; init; }
    }

    // === Changelog renderer ===

    private static void RenderChangelogText(string service, string body)
    {
        AnsiConsole.MarkupLine($"\n  [bold cyan]Contract Changelog[/] — {service.EscapeMarkup()}\n");

        ChangelogResponse? changelog = null;
        try { changelog = JsonSerializer.Deserialize<ChangelogResponse>(body, JsonReadOptions); }
        catch { /* fallback to raw output */ }

        if (changelog?.Items is null)
        {
            Console.WriteLine(body);
            return;
        }

        if (changelog.Items.Count == 0)
        {
            AnsiConsole.MarkupLine("  [grey]No changelog entries found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Version[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Description[/]")
            .AddColumn("[bold]Date[/]");

        foreach (var entry in changelog.Items)
        {
            var ver = (entry.SemVer ?? entry.Version ?? "-").EscapeMarkup();
            var typeColor = entry.ChangeType?.ToLowerInvariant() switch
            {
                "breaking" => "red",
                "non-breaking" or "nonbreaking" or "improvement" => "yellow",
                "additive" or "feature" or "new" => "green",
                _ => "grey"
            };
            var date = (entry.PublishedAt ?? entry.CreatedAt)?
                .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "-";

            table.AddRow(
                ver,
                $"[{typeColor}]{(entry.ChangeType ?? "-").EscapeMarkup()}[/{typeColor}]",
                (entry.Description ?? "-").EscapeMarkup(),
                date.EscapeMarkup());
        }

        AnsiConsole.Write(table);
    }
}
