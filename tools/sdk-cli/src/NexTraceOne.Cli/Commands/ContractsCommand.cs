using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class ContractsCommand
{
    public static Command Create(ApiService apiService, AuthenticationService authService)
    {
        var contractsService = new ContractsService(apiService);
        var contractsCommand = new Command("contracts", "Contract management");

        // List subcommand
        var listCommand = new Command("list", "List contracts");
        var pageOption = new Option<int>("--page", () => 1, "Page number");
        var pageSizeOption = new Option<int>("--pageSize", () => 20, "Items per page");
        var statusOption = new Option<string?>("--status", "Filter by status");
        
        listCommand.AddOption(pageOption);
        listCommand.AddOption(pageSizeOption);
        listCommand.AddOption(statusOption);
        
        listCommand.SetHandler(async (page, pageSize, status) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária. Use 'ntrace auth login'[/]");
                Environment.Exit(1);
            }

            try
            {
                var contracts = await contractsService.ListContractsAsync(page, pageSize, status);
                
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Version");
                table.AddColumn("Status");
                table.AddColumn("Created");

                foreach (var contract in contracts)
                {
                    table.AddRow(
                        contract.Id,
                        contract.Name,
                        contract.Version,
                        GetStatusColor(contract.Status),
                        contract.CreatedAt.ToString("yyyy-MM-dd"));
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {contracts.Count} contratos[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, pageOption, pageSizeOption, statusOption);

        // Get subcommand
        var getCommand = new Command("get", "Get contract details");
        var idOption = new Option<string>("--id", "Contract ID") { IsRequired = true };
        
        getCommand.AddOption(idOption);
        getCommand.SetHandler(async (id) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                var contract = await contractsService.GetContractAsync(id);
                
                var panel = new Panel($"[bold]{contract.Name} v{contract.Version}[/]")
                {
                    Border = BoxBorder.Rounded
                };
                
                var grid = new Grid();
                grid.AddColumn();
                grid.AddColumn();
                
                grid.AddRow("[yellow]ID:[/]", contract.Id);
                grid.AddRow("[yellow]Status:[/]", GetStatusColor(contract.Status));
                grid.AddRow("[yellow]Descrição:[/]", contract.Description);
                grid.AddRow("[yellow]Criado:[/]", contract.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                grid.AddRow("[yellow]Atualizado:[/]", contract.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));
                
                panel.Content(grid);
                AnsiConsole.Write(panel);
                
                if (contract.Metadata.Any())
                {
                    AnsiConsole.MarkupLine("\n[bold]Metadata:[/]");
                    foreach (var kvp in contract.Metadata)
                    {
                        AnsiConsole.MarkupLine($"  [dim]{kvp.Key}:[/] {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, idOption);

        // Create subcommand
        var createCommand = new Command("create", "Create new contract");
        var nameOption = new Option<string>("--name", "Contract name") { IsRequired = true };
        var versionOption = new Option<string>("--version", "Contract version") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", "Contract description");
        var specFileOption = new Option<string?>("--spec", "Path to OpenAPI spec file");
        
        createCommand.AddOption(nameOption);
        createCommand.AddOption(versionOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(specFileOption);
        
        createCommand.SetHandler(async (name, version, description, specFile) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                string specContent = "";
                if (!string.IsNullOrEmpty(specFile))
                {
                    specContent = File.ReadAllText(specFile);
                    AnsiConsole.MarkupLine($"[dim]Lendo spec de: {specFile}[/]");
                }

                var request = new CreateContractRequest
                {
                    Name = name,
                    Version = version,
                    Description = description ?? "",
                    SpecContent = specContent
                };

                AnsiConsole.MarkupLine("[yellow]Criando contrato...[/]");
                var contract = await contractsService.CreateContractAsync(request);
                
                AnsiConsole.MarkupLine($"[green]✓ Contrato criado com sucesso![/]");
                AnsiConsole.MarkupLine($"[dim]ID: {contract.Id}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, nameOption, versionOption, descriptionOption, specFileOption);

        // Update subcommand
        var updateCommand = new Command("update", "Update existing contract");
        var updateIdOption = new Option<string>("--id", "Contract ID") { IsRequired = true };
        var updateNameOption = new Option<string?>("--name", "New name");
        var updateVersionOption = new Option<string?>("--version", "New version");
        var updateSpecOption = new Option<string?>("--spec", "New spec file");
        
        updateCommand.AddOption(updateIdOption);
        updateCommand.AddOption(updateNameOption);
        updateCommand.AddOption(updateVersionOption);
        updateCommand.AddOption(updateSpecOption);
        
        updateCommand.SetHandler(async (id, name, version, specFile) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                string? specContent = null;
                if (!string.IsNullOrEmpty(specFile))
                {
                    specContent = File.ReadAllText(specFile);
                }

                var request = new UpdateContractRequest
                {
                    Name = name,
                    Version = version,
                    SpecContent = specContent
                };

                AnsiConsole.MarkupLine("[yellow]Atualizando contrato...[/]");
                await contractsService.UpdateContractAsync(id, request);
                
                AnsiConsole.MarkupLine($"[green]✓ Contrato atualizado com sucesso![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, updateIdOption, updateNameOption, updateVersionOption, updateSpecOption);

        // Delete subcommand
        var deleteCommand = new Command("delete", "Delete contract");
        var deleteIdOption = new Option<string>("--id", "Contract ID") { IsRequired = true };
        var forceOption = new Option<bool>("--force", () => false, "Skip confirmation");
        
        deleteCommand.AddOption(deleteIdOption);
        deleteCommand.AddOption(forceOption);
        
        deleteCommand.SetHandler(async (id, force) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            if (!force)
            {
                var confirm = AnsiConsole.Confirm($"Tem certeza que deseja deletar o contrato {id}?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Operação cancelada[/]");
                    return;
                }
            }

            try
            {
                AnsiConsole.MarkupLine("[yellow]Deletando contrato...[/]");
                await contractsService.DeleteContractAsync(id);
                
                AnsiConsole.MarkupLine($"[green]✓ Contrato deletado com sucesso![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, deleteIdOption, forceOption);

        // Export subcommand
        var exportCommand = new Command("export", "Export contract");
        var exportIdOption = new Option<string>("--id", "Contract ID") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "postman", "Export format (postman/openapi)");
        var outputOption = new Option<string?>("--output", "Output file path");
        
        exportCommand.AddOption(exportIdOption);
        exportCommand.AddOption(formatOption);
        exportCommand.AddOption(outputOption);
        
        exportCommand.SetHandler(async (id, format, output) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                AnsiConsole.MarkupLine($"[yellow]Exportando contrato em formato {format}...[/]");
                var content = await contractsService.ExportContractAsync(id, format);
                
                if (!string.IsNullOrEmpty(output))
                {
                    File.WriteAllText(output, content);
                    AnsiConsole.MarkupLine($"[green]✓ Exportado para: {output}[/]");
                }
                else
                {
                    AnsiConsole.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, exportIdOption, formatOption, outputOption);

        contractsCommand.AddCommand(listCommand);
        contractsCommand.AddCommand(getCommand);
        contractsCommand.AddCommand(createCommand);
        contractsCommand.AddCommand(updateCommand);
        contractsCommand.AddCommand(deleteCommand);
        contractsCommand.AddCommand(exportCommand);

        return contractsCommand;
    }

    private static string GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "active" => "[green]Active[/]",
            "draft" => "[yellow]Draft[/]",
            "deprecated" => "[red]Deprecated[/]",
            _ => $"[{status}]"
        };
    }
}
