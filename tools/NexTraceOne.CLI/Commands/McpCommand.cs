using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex mcp' — gestão e integração com o MCP Server do NexTraceOne.
/// Subcomandos: tools, configure, call.
/// </summary>
public static class McpCommand
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
        var command = new Command("mcp", "Interact with the NexTraceOne MCP (Model Context Protocol) server.");
        command.Add(CreateToolsCommand());
        command.Add(CreateConfigureCommand());
        command.Add(CreateCallCommand());
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
        Description = "API authentication token."
    };

    // ── TOOLS subcommand ───────────────────────────────────────────────────────

    private static Command CreateToolsCommand()
    {
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format: text (default) or json.",
            DefaultValueFactory = _ => "text"
        };

        var command = new Command("tools", "List available tools in the NexTraceOne MCP server.");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(formatOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var format = parseResult.GetValue(formatOpt) ?? "text";

            return await ListToolsAsync(url, token, format, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ListToolsAsync(string url, string? token, string format, CancellationToken cancellationToken)
    {
        // Use JSON-RPC 2.0 to call the MCP tools/list method
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { }
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync("/api/v1/ai/mcp", payload, cancellationToken)
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

            McpToolsListResponse? result = null;
            try
            {
                var rpcResponse = JsonSerializer.Deserialize<JsonElement>(body, JsonReadOptions);
                if (rpcResponse.TryGetProperty("result", out var resultEl))
                    result = JsonSerializer.Deserialize<McpToolsListResponse>(resultEl.GetRawText(), JsonReadOptions);
            }
            catch { /* fallback */ }

            if (result?.Tools is null)
            {
                Console.WriteLine(body);
                return ExitSuccess;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]NexTraceOne MCP Tools[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var tool in result.Tools)
            {
                table.AddRow(
                    (tool.Name ?? "-").EscapeMarkup(),
                    (tool.Description ?? "-").EscapeMarkup());
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Total: {result.Tools.Count} tool(s)[/]");
            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to MCP server: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitError;
        }
    }

    // ── CONFIGURE subcommand ───────────────────────────────────────────────────

    private static Command CreateConfigureCommand()
    {
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();
        var targetOpt = new Option<string>("--target")
        {
            Description = "Target to configure: vscode (default), claude-desktop, or custom path.",
            DefaultValueFactory = _ => "vscode"
        };
        var outputOpt = new Option<string>("--output", "Custom output path for the MCP config file.");

        var command = new Command("configure", "Generate MCP client configuration for VS Code, Claude Desktop, etc.");
        command.Add(urlOpt);
        command.Add(tokenOpt);
        command.Add(targetOpt);
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));
            var target = parseResult.GetValue(targetOpt) ?? "vscode";
            var outputPath = parseResult.GetValue(outputOpt);

            return await ConfigureMcpAsync(url, token, target, outputPath, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ConfigureMcpAsync(
        string serverUrl, string? token, string target, string? outputPath, CancellationToken cancellationToken)
    {
        var mcpServerUrl = serverUrl.TrimEnd('/') + "/api/v1/ai/mcp";

        var mcpConfig = new
        {
            servers = new Dictionary<string, object>
            {
                ["nextraceone"] = new
                {
                    url = mcpServerUrl,
                    type = "http",
                    headers = token is not null
                        ? new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
                        : null
                }
            }
        };

        var json = JsonSerializer.Serialize(mcpConfig, JsonPrintOptions);

        string resolvedPath;

        if (outputPath is not null)
        {
            resolvedPath = outputPath;
        }
        else if (string.Equals(target, "vscode", StringComparison.OrdinalIgnoreCase))
        {
            var vscodeDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".vscode");
            resolvedPath = Path.Combine(vscodeDir, "mcp.json");
        }
        else if (string.Equals(target, "claude-desktop", StringComparison.OrdinalIgnoreCase))
        {
            var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            resolvedPath = Path.Combine(configDir, "Claude", "claude_desktop_config.json");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Unknown target [yellow]{target.EscapeMarkup()}[/]. Use --output to specify a custom path.");
            return ExitError;
        }

        try
        {
            var dir = Path.GetDirectoryName(resolvedPath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(resolvedPath, json, cancellationToken).ConfigureAwait(false);

            AnsiConsole.MarkupLine($"[green]✓[/] MCP configuration written to [yellow]{resolvedPath.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  [dim]MCP Server URL:[/] {mcpServerUrl.EscapeMarkup()}");
            return ExitSuccess;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not write config file: {ex.Message.EscapeMarkup()}");
            return ExitError;
        }
    }

    // ── CALL subcommand ────────────────────────────────────────────────────────

    private static Command CreateCallCommand()
    {
        var toolArg = new Argument<string>("tool")
        {
            Description = "MCP tool name (e.g., get_service_info)."
        };
        var paramsOpt = new Option<string>("--params")
        {
            Description = "JSON string of tool parameters (e.g., '{\"serviceId\":\"svc-001\"}').",
            DefaultValueFactory = _ => "{}"
        };
        var urlOpt = CreateUrlOption();
        var tokenOpt = CreateTokenOption();

        var command = new Command("call", "Invoke an MCP tool directly.");
        command.Add(toolArg);
        command.Add(paramsOpt);
        command.Add(urlOpt);
        command.Add(tokenOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var tool = parseResult.GetValue(toolArg)!;
            var paramsJson = parseResult.GetValue(paramsOpt) ?? "{}";
            var url = CliConfig.ResolveUrl(parseResult.GetValue(urlOpt));
            var token = CliConfig.ResolveToken(parseResult.GetValue(tokenOpt));

            return await CallToolAsync(tool, paramsJson, url, token, cancellationToken).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> CallToolAsync(
        string tool, string paramsJson, string url, string? token, CancellationToken cancellationToken)
    {
        JsonElement parsedParams;
        try
        {
            parsedParams = JsonSerializer.Deserialize<JsonElement>(paramsJson);
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Invalid JSON in --params.");
            return ExitError;
        }

        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = tool,
                arguments = parsedParams
            }
        };

        try
        {
            using var client = CreateHttpClient(url, token);
            var response = await client.PostAsJsonAsync("/api/v1/ai/mcp", payload, cancellationToken)
                .ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] API returned {(int)response.StatusCode}: [yellow]{body.EscapeMarkup()}[/]");
                return ExitError;
            }

            // Pretty-print the result
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(body);
                Console.WriteLine(JsonSerializer.Serialize(element, JsonPrintOptions));
            }
            catch
            {
                Console.WriteLine(body);
            }

            return ExitSuccess;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Cannot connect to MCP server: [yellow]{ex.Message.EscapeMarkup()}[/]");
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

    private sealed class McpToolsListResponse
    {
        [JsonPropertyName("tools")]
        public IReadOnlyList<McpTool> Tools { get; init; } = [];
    }

    private sealed class McpTool
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
