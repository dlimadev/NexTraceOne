using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comandos de verificação de conformidade de contratos.
/// Subcomandos: verify, diff, changelog, sync, migrate.
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
        command.Add(CreateListCommand());
        command.Add(CreateVerifyCommand());
        command.Add(CreateDiffCommand());
        command.Add(CreateChangelogCommand());
        command.Add(CreateSyncCommand());
        command.Add(CreateMigrateCommand());
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

    private static Command CreateListCommand()
    {
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();
        var protocolOpt = new Option<string>("--protocol", "Filter by protocol: REST, SOAP, Kafka, AsyncAPI.");
        var stateOpt = new Option<string>("--state", "Filter by lifecycle state: Active, Deprecated, Draft.");
        var searchOpt = new Option<string>("--search", "Free-text search term.");
        var pageOpt = new Option<int>("--page") { DefaultValueFactory = _ => 1, Description = "Page number (default: 1)." };
        var pageSizeOpt = new Option<int>("--page-size") { DefaultValueFactory = _ => 20, Description = "Results per page (default: 20)." };

        var command = new Command("list", "List contracts registered in NexTraceOne.");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);
        command.Add(protocolOpt);
        command.Add(stateOpt);
        command.Add(searchOpt);
        command.Add(pageOpt);
        command.Add(pageSizeOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";
            var protocol = parseResult.GetValue(protocolOpt);
            var state = parseResult.GetValue(stateOpt);
            var search = parseResult.GetValue(searchOpt);
            var page = parseResult.GetValue(pageOpt);
            var pageSize = parseResult.GetValue(pageSizeOpt);

            return await ListContractsAsync(url, token, format, protocol, state, search, page, pageSize, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ListContractsAsync(
        string apiUrl, string? token, string format,
        string? protocol, string? lifecycleState, string? searchTerm,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(apiUrl, token);

            var queryParts = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };
            if (!string.IsNullOrWhiteSpace(protocol)) queryParts.Add($"protocol={Uri.EscapeDataString(protocol)}");
            if (!string.IsNullOrWhiteSpace(lifecycleState)) queryParts.Add($"lifecycleState={Uri.EscapeDataString(lifecycleState)}");
            if (!string.IsNullOrWhiteSpace(searchTerm)) queryParts.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            var query = string.Join("&", queryParts);
            var response = await client.GetAsync($"/api/v1/contracts/list?{query}", cancellationToken).ConfigureAwait(false);
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
                    var el = JsonSerializer.Deserialize<JsonElement>(body, JsonReadOptions);
                    Console.WriteLine(JsonSerializer.Serialize(el, JsonPrintOptions));
                }
                catch { Console.WriteLine(body); }
                return ExitSuccess;
            }

            ContractListResponse? result = null;
            try { result = JsonSerializer.Deserialize<ContractListResponse>(body, JsonReadOptions); }
            catch { /* fallback */ }

            if (result is null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No contracts found.[/]");
                return ExitSuccess;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Contracts[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Protocol[/]")
                .AddColumn("[bold]Version[/]")
                .AddColumn("[bold]Service[/]")
                .AddColumn("[bold]State[/]")
                .AddColumn("[bold]Updated[/]");

            foreach (var item in result.Items)
            {
                var stateColor = item.LifecycleState?.ToLowerInvariant() switch
                {
                    "active" => "green",
                    "deprecated" => "yellow",
                    "draft" => "blue",
                    _ => "grey"
                };
                var protocolColor = item.Protocol?.ToLowerInvariant() switch
                {
                    "rest" => "cyan",
                    "kafka" or "asyncapi" => "magenta",
                    "soap" => "yellow",
                    _ => "grey"
                };

                table.AddRow(
                    (item.Name ?? "-").EscapeMarkup(),
                    $"[{protocolColor}]{(item.Protocol ?? "-").EscapeMarkup()}[/{protocolColor}]",
                    (item.SemVer ?? "-").EscapeMarkup(),
                    (item.ServiceName ?? "-").EscapeMarkup(),
                    $"[{stateColor}]{(item.LifecycleState ?? "-").EscapeMarkup()}[/{stateColor}]",
                    item.UpdatedAt?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "-");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Page {page} — Showing {result.Items.Count} of {result.TotalCount} contract(s)[/]");
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

    private sealed class ContractListResponse
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<ContractListItem> Items { get; init; } = [];

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; init; }
    }

    private sealed class ContractListItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("protocol")]
        public string? Protocol { get; init; }

        [JsonPropertyName("semVer")]
        public string? SemVer { get; init; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        [JsonPropertyName("lifecycleState")]
        public string? LifecycleState { get; init; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset? UpdatedAt { get; init; }
    }

    // === MIGRATE subcommand ===

    private static Command CreateMigrateCommand()
    {
        var baseOpt = new Option<string>("--base") { Description = "Base contract version ID (GUID).", Required = true };
        var targetOpt = new Option<string>("--target-version") { Description = "Target contract version ID (GUID).", Required = true };
        var targetSideOpt = new Option<string>("--target-side")
        {
            Description = "Side to generate for: provider, consumer, all (default: all).",
            DefaultValueFactory = _ => "all"
        };
        var langOpt = new Option<string>("--language")
        {
            Description = "Implementation language for code hints (e.g. C#, Java, TypeScript, Python).",
            DefaultValueFactory = _ => "C#"
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("migrate", "Generate code migration hints for a contract change (provider/consumer).");
        command.Options.Add(baseOpt);
        command.Options.Add(targetOpt);
        command.Options.Add(targetSideOpt);
        command.Options.Add(langOpt);
        command.Options.Add(urlOpt);
        command.Options.Add(tokenOpt);
        command.Options.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var baseId = parseResult.GetValue(baseOpt)!;
            var targetId = parseResult.GetValue(targetOpt)!;
            var side = parseResult.GetValue(targetSideOpt) ?? "all";
            var language = parseResult.GetValue(langOpt) ?? "C#";
            var url = parseResult.GetValue(urlOpt)!;
            var token = parseResult.GetValue(tokenOpt);
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await MigrateAsync(baseId, targetId, side, language, url, token, format, cancellationToken)
                .ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> MigrateAsync(
        string baseId,
        string targetId,
        string side,
        string language,
        string url,
        string? token,
        string format,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(baseId, out _) || !Guid.TryParse(targetId, out _))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] --base and --target-version must be valid GUIDs.");
            return ExitError;
        }

        using var client = new HttpClient { BaseAddress = new Uri(url) };
        var resolved = Services.CliConfig.ResolveToken(token);
        if (!string.IsNullOrWhiteSpace(resolved))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {resolved}");

        try
        {
            var payload = new
            {
                baseVersionId = baseId,
                targetVersionId = targetId,
                target = side,
                language
            };

            var response = await client
                .PostAsJsonAsync("/api/v1/contracts/migration-patch", payload, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error {(int)response.StatusCode}:[/] {body.EscapeMarkup()}");
                return ExitError;
            }

            if (format == "json")
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            RenderMigrationPatch(body);
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Request failed:[/] {ex.Message.EscapeMarkup()}");
            return ExitError;
        }
    }

    private static void RenderMigrationPatch(string body)
    {
        MigrationPatchResponse? patch = null;
        try { patch = JsonSerializer.Deserialize<MigrationPatchResponse>(body, JsonReadOptions); }
        catch { /* fallback */ }

        if (patch is null)
        {
            Console.WriteLine(body);
            return;
        }

        AnsiConsole.MarkupLine(
            $"\n  [bold cyan]Contract Migration Patch[/]" +
            $"  Protocol: [yellow]{patch.Protocol.EscapeMarkup()}[/]" +
            $"  Language: [yellow]{patch.Language.EscapeMarkup()}[/]" +
            $"  Change Level: [red]{patch.ChangeLevel.EscapeMarkup()}[/]" +
            $"  Breaking Changes: [red]{patch.BreakingChangeCount}[/]\n");

        if (patch.ProviderSuggestions.Count > 0)
        {
            AnsiConsole.MarkupLine("  [bold green]Provider Suggestions[/]");
            foreach (var s in patch.ProviderSuggestions)
            {
                var severity = s.Severity?.ToLowerInvariant() switch
                {
                    "high" => "[red]HIGH[/]",
                    "medium" => "[yellow]MEDIUM[/]",
                    _ => "[grey]LOW[/]"
                };
                AnsiConsole.MarkupLine($"  {severity} [{s.Kind.EscapeMarkup()}] {s.Description.EscapeMarkup()}");
                if (!string.IsNullOrWhiteSpace(s.CodeHint))
                {
                    AnsiConsole.MarkupLine($"  [grey]{s.CodeHint.EscapeMarkup()}[/]");
                }
            }
        }

        if (patch.ConsumerSuggestions.Count > 0)
        {
            AnsiConsole.MarkupLine("\n  [bold yellow]Consumer Suggestions[/]");
            foreach (var s in patch.ConsumerSuggestions)
            {
                var severity = s.Severity?.ToLowerInvariant() switch
                {
                    "high" => "[red]HIGH[/]",
                    "medium" => "[yellow]MEDIUM[/]",
                    _ => "[grey]LOW[/]"
                };
                AnsiConsole.MarkupLine($"  {severity} [{s.Kind.EscapeMarkup()}] {s.Description.EscapeMarkup()}");
                if (!string.IsNullOrWhiteSpace(s.CodeHint))
                {
                    AnsiConsole.MarkupLine($"  [grey]{s.CodeHint.EscapeMarkup()}[/]");
                }
            }
        }

        if (patch.ProviderSuggestions.Count == 0 && patch.ConsumerSuggestions.Count == 0)
        {
            AnsiConsole.MarkupLine("  [grey]No migration suggestions generated (no detectable changes).[/]");
        }
    }

    private sealed class MigrationPatchResponse
    {
        [JsonPropertyName("protocol")]
        public string Protocol { get; init; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; init; } = string.Empty;

        [JsonPropertyName("changeLevel")]
        public string ChangeLevel { get; init; } = string.Empty;

        [JsonPropertyName("breakingChangeCount")]
        public int BreakingChangeCount { get; init; }

        [JsonPropertyName("providerSuggestions")]
        public IReadOnlyList<MigrationSuggestionItem> ProviderSuggestions { get; init; } = [];

        [JsonPropertyName("consumerSuggestions")]
        public IReadOnlyList<MigrationSuggestionItem> ConsumerSuggestions { get; init; } = [];
    }

    private sealed class MigrationSuggestionItem
    {
        [JsonPropertyName("kind")]
        public string Kind { get; init; } = string.Empty;

        [JsonPropertyName("side")]
        public string Side { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("codeHint")]
        public string? CodeHint { get; init; }

        [JsonPropertyName("severity")]
        public string Severity { get; init; } = string.Empty;
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
