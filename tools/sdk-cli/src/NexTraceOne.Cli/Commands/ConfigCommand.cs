using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class ConfigCommand
{
    public static Command Create(ConfigurationService configService)
    {
        var configCommand = new Command("config", "Configuration management");

        // Set subcommand
        var setCommand = new Command("set", "Set a configuration value");
        var keyArg = new Argument<string>("key", "Configuration key");
        var valueArg = new Argument<string>("value", "Configuration value");
        
        setCommand.AddArgument(keyArg);
        setCommand.AddArgument(valueArg);
        
        setCommand.SetHandler((key, value) =>
        {
            switch (key.ToLower())
            {
                case "endpoint":
                    configService.SetEndpoint(value);
                    AnsiConsole.MarkupLine($"[green]✓ Endpoint definido: {value}[/]");
                    break;
                case "timeout":
                    if (int.TryParse(value, out int timeout))
                    {
                        configService.SetTimeout(timeout);
                        AnsiConsole.MarkupLine($"[green]✓ Timeout definido: {timeout}s[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Valor inválido para timeout[/]");
                    }
                    break;
                case "output":
                    configService.SetOutputFormat(value);
                    AnsiConsole.MarkupLine($"[green]✓ Formato de output definido: {value}[/]");
                    break;
                case "colors":
                    if (bool.TryParse(value, out bool colors))
                    {
                        configService.SetColors(colors);
                        AnsiConsole.MarkupLine($"[green]✓ Cores {(colors ? "habilitadas" : "desabilitadas")}[/]");
                    }
                    break;
                default:
                    AnsiConsole.MarkupLine($"[red]✗ Chave de configuração desconhecida: {key}[/]");
                    break;
            }
        }, keyArg, valueArg);

        // Get subcommand
        var getCommand = new Command("get", "Get a configuration value");
        var getKeyArg = new Argument<string>("key", "Configuration key");
        
        getCommand.AddArgument(getKeyArg);
        getCommand.SetHandler((key) =>
        {
            var settings = configService.GetAllSettings();
            if (settings.ContainsKey(key.ToLower()))
            {
                AnsiConsole.WriteLine($"{key}: {settings[key.ToLower()]}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Chave não encontrada: {key}[/]");
            }
        }, getKeyArg);

        // List subcommand
        var listCommand = new Command("list", "List all configuration values");
        listCommand.SetHandler(() =>
        {
            var settings = configService.GetAllSettings();
            
            var table = new Table();
            table.AddColumn("Setting");
            table.AddColumn("Value");

            foreach (var kvp in settings)
            {
                table.AddRow(kvp.Key, kvp.Value.ToString() ?? "");
            }

            AnsiConsole.Write(table);
        });

        // Reset subcommand
        var resetCommand = new Command("reset", "Reset configuration to defaults");
        resetCommand.SetHandler(() =>
        {
            configService.Reset();
            AnsiConsole.MarkupLine("[green]✓ Configurações resetadas para valores padrão[/]");
        });

        configCommand.AddCommand(setCommand);
        configCommand.AddCommand(getCommand);
        configCommand.AddCommand(listCommand);
        configCommand.AddCommand(resetCommand);

        return configCommand;
    }
}
