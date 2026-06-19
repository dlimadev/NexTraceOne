using System.CommandLine;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTrace.Sdk;
using NexTrace.Sdk.Clients;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex integration' — acelera a criação de integrações entre serviços.
/// Subcomandos: scaffold (gera cliente consumidor tipado), register (regista relação de consumo).
/// </summary>
public static class IntegrationCommand
{
    private const int ExitSuccess = 0;
    private const int ExitError = 1;
    private const int ExitProviderNotFound = 2;
    private const int ExitNoContracts = 3;

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var command = new Command("integration", "Accelerate service-to-service integrations using the catalog and contracts.");
        command.Add(CreateScaffoldCommand());
        command.Add(CreateRegisterCommand());
        return command;
    }

    private static Command CreateScaffoldCommand()
    {
        var providerOpt = new Option<string>("--provider") { Description = "Canonical name of the provider service.", Required = true };
        var consumerOpt = new Option<string>("--consumer") { Description = "Canonical name of the consuming service.", Required = true };
        var routesOpt = new Option<string?>("--routes") { Description = "Comma-separated routes or operationIds to include (optional)." };
        var langOpt = new Option<string>("--lang")
        {
            Description = "Target language for generated code. Only 'csharp' is supported by the current backend generator.",
            DefaultValueFactory = _ => "csharp"
        };
        var outputOpt = new Option<string>("--output")
        {
            Description = "Output directory for generated files.",
            DefaultValueFactory = _ => "./generated"
        };
        var namespaceOpt = new Option<string?>("--namespace") { Description = "Root namespace for generated code." };
        var registerOpt = new Option<bool>("--register") { Description = "Register consumer relationships in the catalog after generation." };
        var confidenceOpt = new Option<decimal>("--confidence") { DefaultValueFactory = _ => 0.95m, Description = "Confidence score (0.01-1.0) when registering consumers." };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("scaffold", "Generate a typed consumer client from a provider's catalog contracts.");
        command.Add(providerOpt);
        command.Add(consumerOpt);
        command.Add(routesOpt);
        command.Add(langOpt);
        command.Add(outputOpt);
        command.Add(namespaceOpt);
        command.Add(registerOpt);
        command.Add(confidenceOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var provider = parseResult.GetValue(providerOpt)!;
            var consumer = parseResult.GetValue(consumerOpt)!;
            var routes = parseResult.GetValue(routesOpt);
            var lang = parseResult.GetValue(langOpt) ?? "csharp";
            var output = parseResult.GetValue(outputOpt) ?? "./generated";
            var rootNamespace = parseResult.GetValue(namespaceOpt);
            var register = parseResult.GetValue(registerOpt);
            var confidence = parseResult.GetValue(confidenceOpt);
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ScaffoldAsync(
                provider, consumer, routes, lang, output, rootNamespace, register, confidence,
                url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static Command CreateRegisterCommand()
    {
        var apiOpt = new Option<string>("--provider-api") { Description = "API asset ID (GUID) of the provider API.", Required = true };
        var consumerOpt = new Option<string>("--consumer") { Description = "Name of the consuming service.", Required = true };
        var kindOpt = new Option<string>("--kind") { DefaultValueFactory = _ => "Service", Description = "Consumer kind." };
        var envOpt = new Option<string>("--environment") { DefaultValueFactory = _ => "Production", Description = "Consumer environment." };
        var sourceOpt = new Option<string>("--source") { DefaultValueFactory = _ => "cli", Description = "Source system identifier." };
        var referenceOpt = new Option<string>("--reference") { DefaultValueFactory = _ => "nex integration register", Description = "External reference." };
        var confidenceOpt = new Option<decimal>("--confidence") { DefaultValueFactory = _ => 0.95m, Description = "Confidence score (0.01-1.0)." };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = CreateFormatOption();

        var command = new Command("register", "Register a consumer relationship for a provider API asset.");
        command.Add(apiOpt);
        command.Add(consumerOpt);
        command.Add(kindOpt);
        command.Add(envOpt);
        command.Add(sourceOpt);
        command.Add(referenceOpt);
        command.Add(confidenceOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var apiAssetId = parseResult.GetValue(apiOpt)!;
            var consumer = parseResult.GetValue(consumerOpt)!;
            var kind = parseResult.GetValue(kindOpt) ?? "Service";
            var env = parseResult.GetValue(envOpt) ?? "Production";
            var source = parseResult.GetValue(sourceOpt) ?? "cli";
            var reference = parseResult.GetValue(referenceOpt) ?? "nex integration register";
            var confidence = parseResult.GetValue(confidenceOpt);
            var url = Services.CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = Services.CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await RegisterAsync(
                apiAssetId, consumer, kind, env, source, reference, confidence,
                url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ScaffoldAsync(
        string provider, string consumer, string? routes, string lang, string output,
        string? rootNamespace, bool register, decimal confidence, string url, string? token, string format,
        CancellationToken cancellationToken, NexTraceSdkClient? injectedClient = null)
    {
        if (!IsSupportedLanguage(lang))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Language [blue]{lang.EscapeMarkup()}[/] is not supported. The current backend generator only supports [green]csharp[/].");
            return ExitError;
        }

        if (!Directory.Exists(output))
        {
            try
            {
                Directory.CreateDirectory(output);
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Cannot create output directory: [yellow]{ex.Message.EscapeMarkup()}[/]");
                return ExitError;
            }
        }

        try
        {
            using var client = injectedClient ?? CreateSdkClient(url, token);

            var routesFilter = routes is null
                ? null
                : routes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var request = new GenerateConsumerClientRequest
            {
                ProviderName = provider,
                ConsumerName = consumer,
                RootNamespace = rootNamespace,
                Routes = routesFilter
            };

            var result = await client.Integrations.GenerateConsumerClientAsync(request, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (!result.Success)
            {
                var reason = string.IsNullOrWhiteSpace(result.Error) ? "Unknown error." : result.Error;
                AnsiConsole.MarkupLine($"[red]Error:[/] {reason.EscapeMarkup()}");
                return reason.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    ? ExitProviderNotFound
                    : result.Error?.Contains("No contracts", StringComparison.OrdinalIgnoreCase) == true
                        ? ExitNoContracts
                        : ExitError;
            }

            var writtenFiles = new List<string>();
            foreach (var contract in result.GeneratedContracts)
            {
                var contractDir = Path.Combine(output, ToSafeDirectoryName(contract.ApiName ?? contract.ServiceName ?? "contract"));
                Directory.CreateDirectory(contractDir);

                foreach (var file in contract.Files)
                {
                    if (string.IsNullOrWhiteSpace(file.Path))
                        continue;

                    var targetPath = Path.Combine(contractDir, file.Path.TrimStart('/', '\\'));
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrWhiteSpace(targetDir))
                        Directory.CreateDirectory(targetDir);

                    await File.WriteAllTextAsync(targetPath, file.Content ?? string.Empty, cancellationToken).ConfigureAwait(false);
                    writtenFiles.Add(targetPath);
                }
            }

            var impactSummaries = new List<IntegrationManifestImpact>();
            if (!string.IsNullOrWhiteSpace(result.ProviderServiceId))
            {
                try
                {
                    var impact = await client.Integrations.GetImpactAsync(result.ProviderServiceId, 2, cancellationToken).ConfigureAwait(false);
                    if (impact is not null)
                    {
                        impactSummaries.AddRange(
                            impact.AffectedNodes.Select(n => new IntegrationManifestImpact
                            {
                                NodeId = n.NodeId,
                                Name = n.Name,
                                Kind = n.Kind,
                                Depth = n.Depth
                            }));
                    }
                }
                catch (HttpRequestException ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not retrieve impact analysis: {ex.Message.EscapeMarkup()}");
                }
            }

            var manifest = new IntegrationManifest
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                ProviderName = result.ProviderName,
                ProviderServiceId = result.ProviderServiceId,
                ConsumerName = result.ConsumerName,
                Language = lang,
                TotalFiles = result.TotalFiles,
                TotalOperations = result.TotalOperations,
                Contracts = result.GeneratedContracts.Select(c => new IntegrationManifestContract
                {
                    ContractVersionId = c.ContractVersionId,
                    ApiAssetId = c.ApiAssetId,
                    ApiName = c.ApiName,
                    SemVer = c.SemVer,
                    OperationCount = c.OperationCount
                }).ToList(),
                Impact = impactSummaries,
                WrittenFiles = writtenFiles
            };

            var manifestPath = Path.Combine(output, "nexone-integration.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, JsonPrintOptions),
                cancellationToken).ConfigureAwait(false);

            if (register)
            {
                foreach (var contract in result.GeneratedContracts)
                {
                    if (string.IsNullOrWhiteSpace(contract.ApiAssetId))
                        continue;

                    try
                    {
                        await client.Integrations.RegisterConsumerAsync(new RegisterConsumerRequest
                        {
                            ApiAssetId = contract.ApiAssetId,
                            ConsumerName = consumer,
                            ConsumerKind = "Service",
                            ConsumerEnvironment = "Production",
                            SourceType = "cli",
                            ExternalReference = $"nex integration scaffold --provider {provider} --consumer {consumer}",
                            ConfidenceScore = confidence
                        }, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpRequestException ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not register consumer for API [blue]{contract.ApiAssetId}[/]: {ex.Message.EscapeMarkup()}");
                    }
                }
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(manifest, JsonPrintOptions));
                return ExitSuccess;
            }

            AnsiConsole.MarkupLine($"\n  [bold cyan]Integration client generated[/] — [blue]{provider.EscapeMarkup()}[/] → [green]{consumer.EscapeMarkup()}[/]\n");
            AnsiConsole.MarkupLine($"  Contracts: [blue]{result.GeneratedContracts.Count}[/]  Files: [green]{result.TotalFiles}[/]  Operations: [yellow]{result.TotalOperations}[/]");
            AnsiConsole.MarkupLine($"  Output: [grey]{Path.GetFullPath(output).EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  Manifest: [grey]{manifestPath.EscapeMarkup()}[/]");

            if (register)
                AnsiConsole.MarkupLine("  [green]✓[/] Consumer relationships registered.");

            return ExitSuccess;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Provider service not found: [yellow]{provider.EscapeMarkup()}[/]");
            return ExitProviderNotFound;
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

    private static async Task<int> RegisterAsync(
        string apiAssetId, string consumer, string kind, string environment,
        string source, string reference, decimal confidence,
        string url, string? token, string format, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateSdkClient(url, token);

            var relationship = await client.Integrations.RegisterConsumerAsync(new RegisterConsumerRequest
            {
                ApiAssetId = apiAssetId,
                ConsumerName = consumer,
                ConsumerKind = kind,
                ConsumerEnvironment = environment,
                SourceType = source,
                ExternalReference = reference,
                ConfidenceScore = confidence
            }, cancellationToken).ConfigureAwait(false);

            if (relationship is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from API.");
                return ExitError;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(relationship, JsonPrintOptions));
                return ExitSuccess;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Consumer relationship registered:");
            AnsiConsole.MarkupLine($"  API: [blue]{relationship.ApiAssetId.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  Consumer: [green]{relationship.ConsumerName.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  Relationship ID: [grey]{relationship.RelationshipId.EscapeMarkup()}[/]");
            return ExitSuccess;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] API asset not found: [yellow]{apiAssetId.EscapeMarkup()}[/]");
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

    private static NexTraceSdkClient CreateSdkClient(string url, string? token)
    {
        return new NexTraceSdkClient(new NexTraceSdkOptions
        {
            BaseUrl = url,
            ApiToken = token ?? string.Empty,
            TimeoutSeconds = 60
        });
    }

    private static bool IsSupportedLanguage(string lang) =>
        string.Equals(lang, "csharp", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(lang, "c#", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(lang, "cs", StringComparison.OrdinalIgnoreCase);

    private static Option<string> CreateUrlOption() => new("--url")
    {
        Description = "NexTraceOne API base URL.",
        DefaultValueFactory = _ => Services.CliConfig.ResolveUrl(null)
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

    private static string ToSafeDirectoryName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "contract";

        var safe = string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(safe) ? "contract" : safe;
    }

    private sealed class IntegrationManifest
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderServiceId { get; set; }
        public string? ConsumerName { get; set; }
        public string? Language { get; set; }
        public int TotalFiles { get; set; }
        public int TotalOperations { get; set; }
        public List<IntegrationManifestContract> Contracts { get; set; } = [];
        public List<IntegrationManifestImpact> Impact { get; set; } = [];
        public List<string> WrittenFiles { get; set; } = [];
    }

    private sealed class IntegrationManifestContract
    {
        public string? ContractVersionId { get; set; }
        public string? ApiAssetId { get; set; }
        public string? ApiName { get; set; }
        public string? SemVer { get; set; }
        public int OperationCount { get; set; }
    }

    private sealed class IntegrationManifestImpact
    {
        public string? NodeId { get; set; }
        public string? Name { get; set; }
        public string? Kind { get; set; }
        public int Depth { get; set; }
    }
}
