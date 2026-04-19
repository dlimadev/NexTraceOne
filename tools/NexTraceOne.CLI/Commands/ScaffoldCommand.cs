using System.CommandLine;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex scaffold' — gera scaffolding de projetos a partir de templates governados do NexTraceOne.
///
/// Subcomandos:
///   templates  — lista templates disponíveis
///   init       — gera projeto localmente a partir de um template + serviço registado
///   register   — regista no catálogo um serviço scaffoldado
/// </summary>
public static class ScaffoldCommand
{
    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonPrintOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var command = new Command("scaffold", "Scaffold a new service from a NexTraceOne governed template.");

        command.Add(CreateTemplatesCommand());
        command.Add(CreateInitCommand());
        command.Add(CreateRegisterCommand());

        return command;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // nex scaffold templates
    // ─────────────────────────────────────────────────────────────────────────

    private static Command CreateTemplatesCommand()
    {
        var urlOption = CreateUrlOption();
        var tokenOption = CreateTokenOption();
        var formatOption = CreateFormatOption();
        var typeOption = new Option<string?>("--type")
        {
            Description = "Filter by service type (e.g. RestApi, Worker, EventConsumer, Grpc)."
        };
        var languageOption = new Option<string?>("--language")
        {
            Description = "Filter by language (e.g. CSharp, Java, TypeScript, Python, Go)."
        };
        var searchOption = new Option<string?>("--search")
        {
            Description = "Search templates by name or description."
        };

        var command = new Command("templates", "List available service templates.");
        command.Add(urlOption);
        command.Add(tokenOption);
        command.Add(formatOption);
        command.Add(typeOption);
        command.Add(languageOption);
        command.Add(searchOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOption));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOption));
            var format = parseResult.GetValue(formatOption) ?? "text";
            var type = parseResult.GetValue(typeOption);
            var language = parseResult.GetValue(languageOption);
            var search = parseResult.GetValue(searchOption);
            return ListTemplatesAsync(url, token, format, type, language, search, cancellationToken);
        });

        return command;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // nex scaffold init
    // ─────────────────────────────────────────────────────────────────────────

    private static Command CreateInitCommand()
    {
        var serviceNameArg = new Argument<string>("service-name")
        {
            Description = "Service name in lowercase kebab-case (e.g. payment-service)."
        };

        var templateOption = new Option<string>("--template")
        {
            Description = "Template slug or ID to use for scaffolding.",
            Required = true
        };
        var teamOption = new Option<string?>("--team")
        {
            Description = "Team name that owns the new service."
        };
        var domainOption = new Option<string?>("--domain")
        {
            Description = "Domain the service belongs to."
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory for generated files (defaults to ./<service-name>).",
            DefaultValueFactory = _ => string.Empty
        };
        var repoOption = new Option<string?>("--repo")
        {
            Description = "Repository URL to associate with the service."
        };
        var importContractOption = new Option<bool>("--import-contract")
        {
            Description = "Write the base contract spec to the output directory.",
            DefaultValueFactory = _ => true
        };
        var registerOption = new Option<bool>("--register")
        {
            Description = "Auto-register the scaffolded service in the NexTraceOne catalog after generation.",
            DefaultValueFactory = _ => false
        };
        var urlOption = CreateUrlOption();
        var tokenOption = CreateTokenOption();
        var formatOption = CreateFormatOption();

        var command = new Command("init", "Generate a new service project from a governed template.");
        command.Add(serviceNameArg);
        command.Add(templateOption);
        command.Add(teamOption);
        command.Add(domainOption);
        command.Add(outputOption);
        command.Add(repoOption);
        command.Add(importContractOption);
        command.Add(registerOption);
        command.Add(urlOption);
        command.Add(tokenOption);
        command.Add(formatOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var serviceName = parseResult.GetValue(serviceNameArg)!;
            var template = parseResult.GetValue(templateOption)!;
            var team = parseResult.GetValue(teamOption);
            var domain = parseResult.GetValue(domainOption);
            var outputDir = parseResult.GetValue(outputOption) ?? string.Empty;
            var repo = parseResult.GetValue(repoOption);
            var importContract = parseResult.GetValue(importContractOption);
            var register = parseResult.GetValue(registerOption);
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOption));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOption));
            var format = parseResult.GetValue(formatOption) ?? "text";
            return InitAsync(serviceName, template, team, domain, outputDir, repo, importContract, register, url, token, format, cancellationToken);
        });

        return command;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // nex scaffold register
    // ─────────────────────────────────────────────────────────────────────────

    private static Command CreateRegisterCommand()
    {
        var serviceNameArg = new Argument<string>("service-name")
        {
            Description = "Service name to register in the NexTraceOne catalog."
        };
        var scaffoldDirOption = new Option<string>("--scaffold-dir")
        {
            Description = "Directory containing the .nextraceone.json scaffold config file.",
            DefaultValueFactory = _ => "."
        };
        var urlOption = CreateUrlOption();
        var tokenOption = CreateTokenOption();

        var command = new Command("register", "Register a scaffolded service in the NexTraceOne catalog.");
        command.Add(serviceNameArg);
        command.Add(scaffoldDirOption);
        command.Add(urlOption);
        command.Add(tokenOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var serviceName = parseResult.GetValue(serviceNameArg)!;
            var scaffoldDir = parseResult.GetValue(scaffoldDirOption) ?? ".";
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOption));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOption));
            return RegisterAsync(serviceName, scaffoldDir, url, token, cancellationToken);
        });

        return command;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Implementation: templates
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<int> ListTemplatesAsync(
        string apiUrl, string? token, string format,
        string? type, string? language, string? search,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(apiUrl, token);

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(type)) query.Add($"serviceType={Uri.EscapeDataString(type)}");
            if (!string.IsNullOrWhiteSpace(language)) query.Add($"language={Uri.EscapeDataString(language)}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            query.Add("isActive=true");

            var path = "/api/v1/catalog/templates" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            var response = await client.GetAsync(path, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TemplateListResponse>(
                JsonReadOptions, cancellationToken).ConfigureAwait(false);

            if (result is null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No templates found.[/]");
                return 0;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(result, JsonPrintOptions));
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Service Templates[/]")
                .AddColumn(new TableColumn("[bold]Slug[/]"))
                .AddColumn(new TableColumn("[bold]Display Name[/]"))
                .AddColumn(new TableColumn("[bold]Type[/]"))
                .AddColumn(new TableColumn("[bold]Language[/]"))
                .AddColumn(new TableColumn("[bold]Domain[/]"))
                .AddColumn(new TableColumn("[bold]Contract[/]"))
                .AddColumn(new TableColumn("[bold]Manifest[/]"))
                .AddColumn(new TableColumn("[bold]Usage[/]"));

            foreach (var t in result.Items)
            {
                table.AddRow(
                    (t.Slug ?? "-").EscapeMarkup(),
                    (t.DisplayName ?? "-").EscapeMarkup(),
                    (t.ServiceType?.ToString() ?? "-").EscapeMarkup(),
                    (t.Language?.ToString() ?? "-").EscapeMarkup(),
                    (t.DefaultDomain ?? "-").EscapeMarkup(),
                    t.HasBaseContract ? "[green]✓[/]" : "[grey]-[/]",
                    t.HasScaffoldingManifest ? "[green]✓[/]" : "[grey]-[/]",
                    t.UsageCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]{result.Total} template(s) found. Use [bold]nex scaffold init <name> --template <slug>[/] to scaffold.[/]");
            return 0;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not connect to {apiUrl.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            return 1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Implementation: init
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<int> InitAsync(
        string serviceName,
        string template,
        string? team, string? domain,
        string outputDir, string? repo,
        bool importContract, bool register,
        string apiUrl, string? token, string format,
        CancellationToken cancellationToken)
    {
        // Resolve output directory
        if (string.IsNullOrWhiteSpace(outputDir))
            outputDir = Path.Combine(Directory.GetCurrentDirectory(), serviceName);

        AnsiConsole.MarkupLine($"[bold cyan]NexTraceOne Scaffold[/]");
        AnsiConsole.MarkupLine($"  Service   : [bold]{serviceName.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  Template  : [bold]{template.EscapeMarkup()}[/]");
        if (team is not null) AnsiConsole.MarkupLine($"  Team      : {team.EscapeMarkup()}");
        if (domain is not null) AnsiConsole.MarkupLine($"  Domain    : {domain.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"  Output    : {outputDir.EscapeMarkup()}");
        AnsiConsole.WriteLine();

        try
        {
            using var client = CreateHttpClient(apiUrl, token);

            // Build scaffold request body
            var body = new ScaffoldRequestBody(serviceName, team, domain, repo, null);
            var bodyJson = JsonSerializer.Serialize(body, JsonReadOptions);

            // Determine if template is a GUID or a slug
            var scaffoldPath = Guid.TryParse(template, out var templateGuid)
                ? $"/api/v1/catalog/templates/{templateGuid:D}/scaffold"
                : $"/api/v1/catalog/templates/slug/{Uri.EscapeDataString(template)}/scaffold";

            ScaffoldResponse? scaffoldResult = null;
            await AnsiConsole.Status().StartAsync("Fetching scaffolding plan from NexTraceOne...", async ctx =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, scaffoldPath)
                {
                    Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
                };
                var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new InvalidOperationException($"Template '{template}' not found. Use 'nex scaffold templates' to list available templates.");

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException($"Server returned {(int)response.StatusCode}: {err}");
                }

                scaffoldResult = await response.Content.ReadFromJsonAsync<ScaffoldResponse>(
                    JsonReadOptions, cancellationToken).ConfigureAwait(false);

                ctx.Status("Writing files...");
            });

            if (scaffoldResult is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Empty response from scaffold API.");
                return 1;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(scaffoldResult, JsonPrintOptions));
                return 0;
            }

            // Write files to output directory
            Directory.CreateDirectory(outputDir);
            var filesWritten = 0;

            foreach (var file in scaffoldResult.Files)
            {
                if (file.Path is null) continue;
                var filePath = Path.GetFullPath(Path.Combine(outputDir, file.Path.Replace('/', Path.DirectorySeparatorChar)));

                // Security: ensure the file path stays within outputDir
                if (!filePath.StartsWith(Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    && filePath != Path.GetFullPath(outputDir))
                {
                    AnsiConsole.MarkupLine($"[yellow]Skipped[/] (path traversal): {file.Path.EscapeMarkup()}");
                    continue;
                }

                var fileDir = Path.GetDirectoryName(filePath);
                if (fileDir is not null) Directory.CreateDirectory(fileDir);
                await File.WriteAllTextAsync(filePath, file.Content ?? string.Empty, Encoding.UTF8, cancellationToken)
                    .ConfigureAwait(false);
                AnsiConsole.MarkupLine($"  [green]✓[/] {file.Path.EscapeMarkup()}");
                filesWritten++;
            }

            // Optionally write base contract spec
            if (importContract && !string.IsNullOrWhiteSpace(scaffoldResult.BaseContractSpec))
            {
                var contractFileName = DetermineContractFileName(scaffoldResult.ServiceType, serviceName);
                var contractPath = Path.Combine(outputDir, contractFileName);
                await File.WriteAllTextAsync(contractPath, scaffoldResult.BaseContractSpec, Encoding.UTF8, cancellationToken)
                    .ConfigureAwait(false);
                AnsiConsole.MarkupLine($"  [green]✓[/] {contractFileName.EscapeMarkup()} [grey](base contract)[/]");
                filesWritten++;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold green]✓ Scaffolding complete![/]  {filesWritten} file(s) written to [bold]{outputDir.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[grey]Scaffolding ID : {scaffoldResult.ScaffoldingId}[/]");
            AnsiConsole.MarkupLine($"[grey]Template       : {(scaffoldResult.TemplateSlug ?? "-").EscapeMarkup()} v{(scaffoldResult.TemplateVersion ?? "-").EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[grey]Service type   : {(scaffoldResult.ServiceType ?? "-").EscapeMarkup()} / {(scaffoldResult.Language ?? "-").EscapeMarkup()}[/]");

            if (scaffoldResult.GovernancePolicyIds is { Count: > 0 })
                AnsiConsole.MarkupLine($"[grey]Governance     : {scaffoldResult.GovernancePolicyIds.Count} polic(ies) applied[/]");

            // Auto-register if requested
            if (register)
            {
                AnsiConsole.WriteLine();
                return await RegisterInternalAsync(serviceName, outputDir, apiUrl, token, scaffoldResult, client, cancellationToken);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Next steps:[/]");
            AnsiConsole.MarkupLine($"  [cyan]cd {serviceName.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  [cyan]nex scaffold register {serviceName.EscapeMarkup()} --scaffold-dir .[/]");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not connect to {apiUrl.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            return 1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Implementation: register
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<int> RegisterAsync(
        string serviceName, string scaffoldDir,
        string apiUrl, string? token,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateHttpClient(apiUrl, token);
            return await RegisterInternalAsync(serviceName, scaffoldDir, apiUrl, token, null, client, cancellationToken);
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not connect to {apiUrl.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request timed out.");
            return 1;
        }
    }

    private static async Task<int> RegisterInternalAsync(
        string serviceName, string scaffoldDir, string apiUrl, string? token,
        ScaffoldResponse? scaffoldResult, HttpClient client, CancellationToken cancellationToken)
    {
        // Read .nextraceone.json if present for additional context
        var nexConfigPath = Path.Combine(scaffoldDir, ".nextraceone.json");
        var nexConfig = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(nexConfigPath))
        {
            try
            {
                var configJson = await File.ReadAllTextAsync(nexConfigPath, cancellationToken).ConfigureAwait(false);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(configJson, JsonReadOptions);
                if (parsed is not null)
                    foreach (var kv in parsed) nexConfig[kv.Key] = kv.Value;
            }
            catch { /* ignore parse errors */ }
        }

        var registerBody = new RegisterBody(
            ServiceName: serviceName,
            Domain: nexConfig.GetValueOrDefault("domain") ?? scaffoldResult?.Domain,
            TeamName: nexConfig.GetValueOrDefault("team") ?? scaffoldResult?.TeamName,
            TemplateSlug: nexConfig.GetValueOrDefault("templateSlug") ?? scaffoldResult?.TemplateSlug,
            ScaffoldingId: scaffoldResult?.ScaffoldingId,
            RepositoryUrl: scaffoldResult?.RepositoryUrl);

        AnsiConsole.MarkupLine($"Registering [bold]{serviceName.EscapeMarkup()}[/] in NexTraceOne catalog...");

        var bodyJson = JsonSerializer.Serialize(registerBody, JsonReadOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/catalog/scaffold/register")
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[red]Registration failed:[/] {err.EscapeMarkup()}");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold green]✓[/] Service [bold]{serviceName.EscapeMarkup()}[/] registered in the NexTraceOne catalog.");
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string DetermineContractFileName(string? serviceType, string serviceName) =>
        serviceType?.ToUpperInvariant() switch
        {
            "RESTAPI" or "REST" => $"{serviceName}-openapi.json",
            "GRPC" => $"{serviceName}-proto.txt",
            "EVENTCONSUMER" or "EVENTPRODUCER" or "KAFKA" => $"{serviceName}-asyncapi.yaml",
            "SOAP" => $"{serviceName}-service.wsdl",
            _ => $"{serviceName}-contract.json"
        };

    private static HttpClient CreateHttpClient(string baseUrl, string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(60)
        };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

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

    // ─────────────────────────────────────────────────────────────────────────
    // API models (consumed from NexTraceOne Catalog API)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed record TemplateListResponse(
        [property: JsonPropertyName("items")] IReadOnlyList<TemplateSummary> Items,
        [property: JsonPropertyName("total")] int Total);

    private sealed record TemplateSummary(
        [property: JsonPropertyName("templateId")] Guid TemplateId,
        [property: JsonPropertyName("slug")] string? Slug,
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("version")] string? Version,
        [property: JsonPropertyName("serviceType")] object? ServiceType,
        [property: JsonPropertyName("language")] object? Language,
        [property: JsonPropertyName("defaultDomain")] string? DefaultDomain,
        [property: JsonPropertyName("defaultTeam")] string? DefaultTeam,
        [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
        [property: JsonPropertyName("isActive")] bool IsActive,
        [property: JsonPropertyName("usageCount")] int UsageCount,
        [property: JsonPropertyName("hasBaseContract")] bool HasBaseContract,
        [property: JsonPropertyName("hasScaffoldingManifest")] bool HasScaffoldingManifest);

    private sealed record ScaffoldRequestBody(
        [property: JsonPropertyName("serviceName")] string ServiceName,
        [property: JsonPropertyName("teamName")] string? TeamName,
        [property: JsonPropertyName("domain")] string? Domain,
        [property: JsonPropertyName("repositoryUrl")] string? RepositoryUrl,
        [property: JsonPropertyName("extraVariables")] IDictionary<string, string>? ExtraVariables);

    private sealed record ScaffoldResponse(
        [property: JsonPropertyName("scaffoldingId")] Guid ScaffoldingId,
        [property: JsonPropertyName("serviceName")] string ServiceName,
        [property: JsonPropertyName("templateId")] Guid TemplateId,
        [property: JsonPropertyName("templateSlug")] string? TemplateSlug,
        [property: JsonPropertyName("templateVersion")] string? TemplateVersion,
        [property: JsonPropertyName("serviceType")] string? ServiceType,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("domain")] string? Domain,
        [property: JsonPropertyName("teamName")] string? TeamName,
        [property: JsonPropertyName("governancePolicyIds")] IReadOnlyList<Guid>? GovernancePolicyIds,
        [property: JsonPropertyName("baseContractSpec")] string? BaseContractSpec,
        [property: JsonPropertyName("files")] IReadOnlyList<ScaffoldedFile> Files,
        [property: JsonPropertyName("repositoryUrl")] string? RepositoryUrl,
        [property: JsonPropertyName("variables")] IReadOnlyDictionary<string, string>? Variables);

    private sealed record ScaffoldedFile(
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("content")] string? Content);

    private sealed record RegisterBody(
        [property: JsonPropertyName("serviceName")] string ServiceName,
        [property: JsonPropertyName("domain")] string? Domain,
        [property: JsonPropertyName("teamName")] string? TeamName,
        [property: JsonPropertyName("templateSlug")] string? TemplateSlug,
        [property: JsonPropertyName("scaffoldingId")] Guid? ScaffoldingId,
        [property: JsonPropertyName("repositoryUrl")] string? RepositoryUrl);
}
