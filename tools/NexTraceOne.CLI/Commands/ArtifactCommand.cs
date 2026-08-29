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
/// Comando 'nex artifact' — assinatura digital e verificação de artefatos (Cosign/Rekor).
/// Subcomandos: sign (assina), verify (verifica). Útil como gate de supply chain em CI/CD.
/// </summary>
public static class ArtifactCommand
{
    private const int ExitSuccess = 0;
    private const int ExitInvalid = 1;
    private const int ExitError = 2;

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var command = new Command("artifact", "Sign and verify artifacts (supply chain attestation via Cosign).");
        command.Add(CreateSignCommand());
        command.Add(CreateVerifyCommand());
        return command;
    }

    // ── SIGN ────────────────────────────────────────────────────────────────────

    private static Command CreateSignCommand()
    {
        var pathOpt = new Option<string>("--path") { Description = "Path/reference of the artifact to sign.", Required = true };
        var typeOpt = new Option<string>("--type")
        {
            Description = "Artifact type: docker-image | nuget-package | binary.",
            DefaultValueFactory = _ => "binary"
        };
        var versionOpt = new Option<string>("--version") { Description = "Artifact version.", Required = true };
        var metadataOpt = new Option<string?>("--metadata") { Description = "Optional metadata as comma-separated key=value pairs." };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("sign", "Digitally sign an artifact (generates SBOM + transparency log entry).");
        command.Add(pathOpt);
        command.Add(typeOpt);
        command.Add(versionOpt);
        command.Add(metadataOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathOpt)!;
            var type = parseResult.GetValue(typeOpt) ?? "binary";
            var version = parseResult.GetValue(versionOpt)!;
            var metadata = parseResult.GetValue(metadataOpt);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await SignAsync(path, type, version, metadata, url, token, format, ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> SignAsync(
        string path, string type, string version, string? metadata, string url, string? token, string format,
        CancellationToken ct, NexTraceSdkClient? injectedClient = null)
    {
        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);

            var request = new SignArtifactRequest
            {
                ArtifactPath = path,
                ArtifactType = type,
                Version = version,
                Metadata = ParseMetadata(metadata)
            };

            var signed = await client.Security.SignArtifactAsync(request, ct).ConfigureAwait(false);
            if (signed is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(signed, JsonPrintOptions));
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Artifact signed: [blue]{(signed.ArtifactName ?? signed.ArtifactId ?? "-").EscapeMarkup()}[/]");
                AnsiConsole.MarkupLine($"  Checksum: [grey]{(signed.Checksum ?? "-").EscapeMarkup()}[/]");
                AnsiConsole.MarkupLine($"  Signer: [grey]{(signed.SignerIdentity ?? "-").EscapeMarkup()}[/]  ·  Signed at: [grey]{signed.SignedAt:u}[/]");
                if (!string.IsNullOrWhiteSpace(signed.TransparencyLogEntry))
                    AnsiConsole.MarkupLine($"  Transparency log: [grey]{signed.TransparencyLogEntry.EscapeMarkup()}[/]");
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

    // ── VERIFY ──────────────────────────────────────────────────────────────────

    private static Command CreateVerifyCommand()
    {
        var artifactArg = new Argument<string>("artifactId") { Description = "Identifier of the signed artifact to verify." };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("verify", "Verify the digital signature of an artifact (exit 1 if invalid).");
        command.Add(artifactArg);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var artifactId = parseResult.GetValue(artifactArg)!;
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await VerifyAsync(artifactId, url, token, format, ct).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> VerifyAsync(
        string artifactId, string url, string? token, string format,
        CancellationToken ct, NexTraceSdkClient? injectedClient = null)
    {
        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);
            var result = await client.Security.VerifyArtifactAsync(artifactId, ct).ConfigureAwait(false);

            if (result is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(result, JsonPrintOptions));
            }
            else if (result.IsValid)
            {
                AnsiConsole.MarkupLine($"[green]✓ Valid signature[/] for [blue]{artifactId.EscapeMarkup()}[/]  ·  Signer: [grey]{(result.SignerIdentity ?? "-").EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Invalid signature[/] for [blue]{artifactId.EscapeMarkup()}[/]");
                foreach (var err in result.Errors)
                    AnsiConsole.MarkupLine($"  [red]- {err.EscapeMarkup()}[/]");
            }

            return result.IsValid ? ExitSuccess : ExitInvalid;
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

    private static IReadOnlyDictionary<string, string>? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = pair.IndexOf('=', StringComparison.Ordinal);
            if (idx > 0)
                dict[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
        }

        return dict.Count > 0 ? dict : null;
    }

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
