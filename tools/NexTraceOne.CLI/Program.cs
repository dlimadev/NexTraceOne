using System.CommandLine;
using NexTraceOne.CLI.Commands;
using Spectre.Console;

// ═══════════════════════════════════════════════════════════════════════════════
// NEX — NexTraceOne Command Line Interface
// Uso: nex <command> [options]
// Consome apenas a camada Contracts de cada módulo (consumidor externo)
// ═══════════════════════════════════════════════════════════════════════════════

var rootCommand = new RootCommand("NexTraceOne CLI — Sovereign Change Intelligence Platform");

rootCommand.Add(ValidateCommand.Create());
rootCommand.Add(CatalogCommand.Create());
rootCommand.Add(ContractCommand.Create());
rootCommand.Add(ChangeCommand.Create());
rootCommand.Add(IncidentCommand.Create());
rootCommand.Add(HealthCommand.Create());
rootCommand.Add(ConfigCommand.Create());
rootCommand.Add(McpCommand.Create());
rootCommand.Add(ReportCommand.Create());
rootCommand.Add(ScaffoldCommand.Create());
rootCommand.Add(CompletionCommand.Create());

// Show banner only when invoked with no arguments
if (args.Length == 0)
{
    AnsiConsole.Write(new FigletText("NexTraceOne CLI").Color(Color.Cyan1));
    AnsiConsole.MarkupLine("[grey]Use [bold]nex --help[/] to see available commands.[/]\n");
}

return await rootCommand.Parse(args).InvokeAsync(new InvocationConfiguration());
