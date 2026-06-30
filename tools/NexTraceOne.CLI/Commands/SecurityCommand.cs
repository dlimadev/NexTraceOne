using System.CommandLine;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTrace.Sdk;
using NexTrace.Sdk.Clients;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex security' — governança de segurança de dependências (supply chain).
/// Subcomandos: deps (saúde de um serviço), vulnerable (inventário de serviços vulneráveis).
/// Útil em pipelines CI/CD para bloquear builds com vulnerabilidades acima de um limiar.
/// </summary>
public static class SecurityCommand
{
    private const int ExitSuccess = 0;
    private const int ExitGateFailed = 1;
    private const int ExitError = 2;

    private static readonly string[] Severities = ["low", "medium", "high", "critical"];

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var command = new Command("security", "Dependency security governance (supply chain) for services.");
        command.Add(CreateDepsCommand());
        command.Add(CreateVulnerableCommand());
        command.Add(CreateSbomCommand());
        return command;
    }

    private static readonly string[] SbomFormats = ["cyclonedx", "spdx"];

    // ── SBOM subcommand ─────────────────────────────────────────────────────────

    private static Command CreateSbomCommand()
    {
        var serviceArg = new Argument<string>("serviceId") { Description = "Service identifier (GUID)." };
        var formatOpt = new Option<string?>("--format") { Description = "SBOM format: cyclonedx (default) or spdx." };
        var outputOpt = new Option<string?>("--output") { Description = "Write the SBOM to this file instead of stdout." };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();

        var command = new Command("sbom", "Generate the Software Bill of Materials (SBOM) for a service.");
        command.Add(serviceArg);
        command.Add(formatOpt);
        command.Add(outputOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var serviceId = parseResult.GetValue(serviceArg)!;
            var format = parseResult.GetValue(formatOpt);
            var output = parseResult.GetValue(outputOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));

            return await SbomAsync(serviceId, format, output, url, token, ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> SbomAsync(
        string serviceId, string? format, string? output, string url, string? token,
        CancellationToken ct, NexTraceSdkClient? injectedClient = null)
    {
        string? apiFormat = null;
        if (!string.IsNullOrWhiteSpace(format))
        {
            if (!Array.Exists(SbomFormats, f => string.Equals(f, format, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid --format [yellow]{format.EscapeMarkup()}[/]. Use: {string.Join(", ", SbomFormats)}.");
                return ExitError;
            }
            apiFormat = string.Equals(format, "spdx", StringComparison.OrdinalIgnoreCase) ? "Spdx" : "CycloneDx";
        }

        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);
            var sbom = await client.Security.GenerateSbomAsync(serviceId, apiFormat, ct).ConfigureAwait(false);

            if (sbom?.SbomContent is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                await File.WriteAllTextAsync(output, sbom.SbomContent, ct).ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[green]✓[/] SBOM ([blue]{sbom.Format ?? "-"}[/]) written to [grey]{output.EscapeMarkup()}[/]");
            }
            else
            {
                Console.WriteLine(sbom.SbomContent);
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No dependency profile found for service [yellow]{serviceId.EscapeMarkup()}[/].");
            return ExitError;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request to NexTraceOne API timed out.");
            return ExitError;
        }
    }

    // ── DEPS subcommand ─────────────────────────────────────────────────────────

    private static Command CreateDepsCommand()
    {
        var serviceArg = new Argument<string>("serviceId") { Description = "Service identifier (GUID)." };
        var failOnOpt = new Option<string?>("--fail-on")
        {
            Description = "Exit 1 if vulnerabilities at or above this severity exist: low | medium | high | critical."
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("deps", "Show the dependency health of a service (score and vulnerability counts).");
        command.Add(serviceArg);
        command.Add(failOnOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var serviceId = parseResult.GetValue(serviceArg)!;
            var failOn = parseResult.GetValue(failOnOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await DepsAsync(serviceId, failOn, url, token, format, ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> DepsAsync(
        string serviceId, string? failOn, string url, string? token, string format, CancellationToken ct,
        NexTraceSdkClient? injectedClient = null)
    {
        if (failOn is not null && !IsValidSeverity(failOn))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid --fail-on value [yellow]{failOn.EscapeMarkup()}[/]. Use: {string.Join(", ", Severities)}.");
            return ExitError;
        }

        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);
            var health = await client.Security.GetDependencyHealthAsync(serviceId, ct).ConfigureAwait(false);

            if (health is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(health, JsonPrintOptions));
            }
            else
            {
                var scoreColor = health.HealthScore >= 80 ? "green" : health.HealthScore >= 50 ? "yellow" : "red";
                AnsiConsole.MarkupLine($"\n  Dependency health for [blue]{serviceId.EscapeMarkup()}[/]");
                AnsiConsole.MarkupLine($"  Health score: [{scoreColor}]{health.HealthScore}[/]  ·  Dependencies: {health.TotalDeps} ({health.DirectDeps} direct, {health.TransitiveDeps} transitive)");
                AnsiConsole.MarkupLine(
                    $"  Vulns — [red]critical {health.CriticalVulnCount}[/], [red]high {health.HighVulnCount}[/], [yellow]medium {health.MediumVulnCount}[/], [grey]low {health.LowVulnCount}[/]");
                AnsiConsole.MarkupLine($"  Outdated: {health.OutdatedCount}  ·  Deprecated: {health.DeprecatedCount}  ·  Last scan: [grey]{health.LastScanAt:u}[/]");
            }

            if (failOn is not null)
            {
                var offending = CountAtOrAbove(health, failOn);
                if (offending > 0)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Gate failed:[/] {offending} vulnerability(ies) at or above [bold]{failOn.ToLowerInvariant()}[/].");
                    return ExitGateFailed;
                }
                AnsiConsole.MarkupLine($"[green]✓ Gate passed:[/] no vulnerabilities at or above [bold]{failOn.ToLowerInvariant()}[/].");
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No dependency profile found for service [yellow]{serviceId.EscapeMarkup()}[/].");
            return ExitError;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request to NexTraceOne API timed out.");
            return ExitError;
        }
    }

    // ── VULNERABLE subcommand ───────────────────────────────────────────────────

    private static Command CreateVulnerableCommand()
    {
        var minSeverityOpt = new Option<string>("--min-severity")
        {
            Description = "Minimum severity to include: low | medium | high | critical.",
            DefaultValueFactory = _ => "high"
        };
        var failOnFoundOpt = new Option<bool>("--fail-on-found")
        {
            Description = "Exit 1 if any vulnerable service is found (useful as a CI gate)."
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("vulnerable", "List services with vulnerabilities at or above a minimum severity.");
        command.Add(minSeverityOpt);
        command.Add(failOnFoundOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var minSeverity = parseResult.GetValue(minSeverityOpt) ?? "high";
            var failOnFound = parseResult.GetValue(failOnFoundOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await VulnerableAsync(minSeverity, failOnFound, url, token, format, ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> VulnerableAsync(
        string minSeverity, bool failOnFound, string url, string? token, string format, CancellationToken ct,
        NexTraceSdkClient? injectedClient = null)
    {
        if (!IsValidSeverity(minSeverity))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid --min-severity value [yellow]{minSeverity.EscapeMarkup()}[/]. Use: {string.Join(", ", Severities)}.");
            return ExitError;
        }

        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);
            var services = await client.Security
                .ListVulnerableServicesAsync(NormalizeSeverity(minSeverity), ct).ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(services, JsonPrintOptions));
            }
            else if (services.Count == 0)
            {
                AnsiConsole.MarkupLine($"[green]✓ No services with vulnerabilities at or above [bold]{minSeverity.ToLowerInvariant()}[/].[/]");
            }
            else
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title($"[bold cyan]Vulnerable services (>= {minSeverity.ToLowerInvariant()})[/]")
                    .AddColumn("[bold]Service[/]")
                    .AddColumn(new TableColumn("[bold]Score[/]").RightAligned())
                    .AddColumn(new TableColumn("[bold]Critical[/]").RightAligned())
                    .AddColumn(new TableColumn("[bold]High[/]").RightAligned())
                    .AddColumn(new TableColumn("[bold]Medium[/]").RightAligned());

                foreach (var s in services)
                {
                    table.AddRow(
                        (s.ServiceId ?? "-").EscapeMarkup(),
                        s.HealthScore.ToString(),
                        s.CriticalCount.ToString(),
                        s.HighCount.ToString(),
                        s.MediumCount.ToString());
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[grey]Total: {services.Count} service(s)[/]");
            }

            if (failOnFound && services.Count > 0)
            {
                AnsiConsole.MarkupLine($"[red]✗ Gate failed:[/] {services.Count} vulnerable service(s) found.");
                return ExitGateFailed;
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to NexTraceOne API: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request to NexTraceOne API timed out.");
            return ExitError;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static bool IsValidSeverity(string value) =>
        Array.Exists(Severities, s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));

    /// <summary>Normaliza para a capitalização esperada pela API (PascalCase do enum).</summary>
    private static string NormalizeSeverity(string value) =>
        char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    /// <summary>Conta as vulnerabilidades com severidade igual ou superior ao limiar informado.</summary>
    private static int CountAtOrAbove(DependencyHealth health, string severity) =>
        severity.ToLowerInvariant() switch
        {
            "critical" => health.CriticalVulnCount,
            "high" => health.CriticalVulnCount + health.HighVulnCount,
            "medium" => health.CriticalVulnCount + health.HighVulnCount + health.MediumVulnCount,
            "low" => health.CriticalVulnCount + health.HighVulnCount + health.MediumVulnCount + health.LowVulnCount,
            _ => 0
        };

    private static NexTraceSdkClient CreateSdkClient(string url, string? token) =>
        new(new NexTraceSdkOptions
        {
            BaseUrl = url,
            ApiToken = token ?? string.Empty,
            TimeoutSeconds = 60
        });

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
}
