using System.CommandLine;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex config' — gere a configuração local do CLI.
/// Persiste em ~/.nex/config.json.
/// Subcomandos: set, get.
/// </summary>
public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Manage local CLI configuration (~/.nex/config.json).");
        command.Add(CreateSetCommand());
        command.Add(CreateGetCommand());
        return command;
    }

    // === SET subcommand ===

    private static Command CreateSetCommand()
    {
        var keyArg = new Argument<string>("key")
        {
            Description = "Configuration key: url | token"
        };
        var valueArg = new Argument<string>("value")
        {
            Description = "Value to set."
        };

        var command = new Command("set", "Set a configuration value.");
        command.Add(keyArg);
        command.Add(valueArg);

        command.SetAction((parseResult, _) =>
        {
            var key = parseResult.GetValue(keyArg)!;
            var value = parseResult.GetValue(valueArg)!;

            var config = CliConfig.Load();

            switch (key.ToLowerInvariant())
            {
                case "url":
                    config.Url = value;
                    config.Save();
                    AnsiConsole.MarkupLine($"[green]✓[/] [bold]url[/] set to [yellow]{value.EscapeMarkup()}[/]");
                    break;

                case "token":
                    config.Token = value;
                    config.Save();
                    AnsiConsole.MarkupLine("[green]✓[/] [bold]token[/] saved to [grey]~/.nex/config.json[/]");
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]Error:[/] Unknown key [yellow]{key.EscapeMarkup()}[/]. Valid keys: [bold]url[/], [bold]token[/]");
                    return Task.FromResult(1);
            }

            return Task.FromResult(0);
        });

        return command;
    }

    // === GET subcommand ===

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Show the current CLI configuration.");

        command.SetAction((_, _) =>
        {
            var config = CliConfig.Load();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]NexTraceOne CLI Configuration[/]")
                .AddColumn(new TableColumn("[bold]Key[/]"))
                .AddColumn(new TableColumn("[bold]Value[/]"))
                .AddColumn(new TableColumn("[bold]Source[/]"));

            AddConfigRow(table, "url", config.Url,
                Environment.GetEnvironmentVariable("NEX_API_URL"),
                "http://localhost:8080");

            var tokenEnv = Environment.GetEnvironmentVariable("NEXTRACE_TOKEN");
            var tokenDisplay = !string.IsNullOrWhiteSpace(config.Token)
                ? "[dim]****** (set)[/]"
                : "[grey](not set)[/]";
            var tokenEnvDisplay = !string.IsNullOrWhiteSpace(tokenEnv)
                ? "[dim]****** (env)[/]"
                : null;

            table.AddRow(
                "[bold]token[/]",
                tokenEnvDisplay ?? tokenDisplay,
                !string.IsNullOrWhiteSpace(tokenEnv) ? "[dim]NEXTRACE_TOKEN[/]"
                : !string.IsNullOrWhiteSpace(config.Token) ? "[dim]~/.nex/config.json[/]"
                : "[grey]none[/]");

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Config file: ~/.nex/config.json[/]");

            return Task.FromResult(0);
        });

        return command;
    }

    private static void AddConfigRow(Table table, string key, string? configValue, string? envValue, string defaultValue)
    {
        string displayValue;
        string source;

        if (!string.IsNullOrWhiteSpace(envValue))
        {
            displayValue = envValue.EscapeMarkup();
            source = $"[dim]{key.ToUpperInvariant().Replace("URL", "NEX_API_URL", StringComparison.OrdinalIgnoreCase)}[/]";
        }
        else if (!string.IsNullOrWhiteSpace(configValue))
        {
            displayValue = configValue.EscapeMarkup();
            source = "[dim]~/.nex/config.json[/]";
        }
        else
        {
            displayValue = $"[grey]{defaultValue.EscapeMarkup()}[/]";
            source = "[grey]default[/]";
        }

        table.AddRow($"[bold]{key}[/]", displayValue, source);
    }
}
