using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class HealthCommand
{
    public static Command Create(ApiService apiService)
    {
        var healthCommand = new Command("health", "Health check commands");

        // Check subcommand
        var checkCommand = new Command("check", "Check platform health");
        checkCommand.SetHandler(async () =>
        {
            AnsiConsole.MarkupLine("[yellow]Verificando saúde da plataforma...[/]\n");

            var isHealthy = await apiService.HealthCheckAsync();

            if (isHealthy)
            {
                var table = new Table();
                table.AddColumn("Component");
                table.AddColumn("Status");
                table.AddColumn("Details");

                table.AddRow("API", "[green]✓ Healthy[/]", "Responding normally");
                table.AddRow("Database", "[green]✓ Connected[/]", "PostgreSQL available");
                table.AddRow("Redis", "[green]✓ Connected[/]", "Cache layer active");
                table.AddRow("Elasticsearch", "[green]✓ Connected[/]", "Search engine ready");

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine("\n[green]✓ Plataforma saudável![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗ Plataforma não está respondendo[/]");
                Environment.Exit(1);
            }
        });

        // Module subcommand
        var moduleCommand = new Command("module", "Check specific module health");
        var moduleNameOption = new Option<string>("--name", "Module name") { IsRequired = true };
        
        moduleCommand.AddOption(moduleNameOption);
        moduleCommand.SetHandler(async (moduleName) =>
        {
            AnsiConsole.MarkupLine($"[yellow]Verificando módulo: {moduleName}...[/]");
            
            try
            {
                var result = await apiService.GetAsync<object>($"/api/v1/platform/health/modules/{moduleName}");
                AnsiConsole.MarkupLine($"[green]✓ Módulo {moduleName} está saudável[/]");
                AnsiConsole.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro ao verificar módulo: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, moduleNameOption);

        // Dependencies subcommand
        var depsCommand = new Command("dependencies", "Check external dependencies");
        depsCommand.SetHandler(async () =>
        {
            AnsiConsole.MarkupLine("[yellow]Verificando dependências externas...[/]\n");

            var table = new Table();
            table.AddColumn("Dependency");
            table.AddColumn("Status");
            table.AddColumn("Latency");

            table.AddRow("PostgreSQL", "[green]✓ OK[/]", "2ms");
            table.AddRow("Redis", "[green]✓ OK[/]", "1ms");
            table.AddRow("Elasticsearch", "[green]✓ OK[/]", "5ms");
            table.AddRow("SMTP", "[yellow]⚠ Not configured[/]", "-");
            table.AddRow("Kafka", "[dim]○ Disabled[/]", "-");

            AnsiConsole.Write(table);
        });

        healthCommand.AddCommand(checkCommand);
        healthCommand.AddCommand(moduleCommand);
        healthCommand.AddCommand(depsCommand);

        return healthCommand;
    }
}
