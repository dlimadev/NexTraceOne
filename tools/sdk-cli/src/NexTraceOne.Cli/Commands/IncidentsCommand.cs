using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class IncidentsCommand
{
    public static Command Create(ApiService apiService, AuthenticationService authService)
    {
        var incidentsService = new IncidentsService(apiService);
        var incidentsCommand = new Command("incidents", "Incident management");

        // List subcommand
        var listCommand = new Command("list", "List incidents");
        var statusOption = new Option<string?>("--status", "Filter by status (open/resolved/closed)");
        var severityOption = new Option<string?>("--severity", "Filter by severity (Low/Medium/High/Critical)");
        
        listCommand.AddOption(statusOption);
        listCommand.AddOption(severityOption);
        
        listCommand.SetHandler(async (status, severity) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                var incidents = await incidentsService.ListIncidentsAsync(status, severity);
                
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Title");
                table.AddColumn("Severity");
                table.AddColumn("Status");
                table.AddColumn("Environment");
                table.AddColumn("Created");

                foreach (var incident in incidents)
                {
                    table.AddRow(
                        incident.Id,
                        incident.Title,
                        GetSeverityColor(incident.Severity),
                        GetStatusColor(incident.Status),
                        incident.Environment,
                        incident.CreatedAt.ToString("yyyy-MM-dd"));
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {incidents.Count} incidentes[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, statusOption, severityOption);

        // Get subcommand
        var getCommand = new Command("get", "Get incident details");
        var idOption = new Option<string>("--id", "Incident ID") { IsRequired = true };
        
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
                var incident = await incidentsService.GetIncidentAsync(id);
                
                var panel = new Panel($"[bold]{incident.Title}[/]")
                {
                    Border = BoxBorder.Rounded
                };
                
                var grid = new Grid();
                grid.AddColumn();
                grid.AddColumn();
                
                grid.AddRow("[yellow]ID:[/]", incident.Id);
                grid.AddRow("[yellow]Severidade:[/]", GetSeverityColor(incident.Severity));
                grid.AddRow("[yellow]Status:[/]", GetStatusColor(incident.Status));
                grid.AddRow("[yellow]Ambiente:[/]", incident.Environment);
                grid.AddRow("[yellow]Descrição:[/]", incident.Description);
                grid.AddRow("[yellow]Criado:[/]", incident.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                if (incident.ResolvedAt.HasValue)
                    grid.AddRow("[yellow]Resolvido:[/]", incident.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm"));
                
                panel.Content(grid);
                AnsiConsole.Write(panel);
                
                if (incident.Comments.Any())
                {
                    AnsiConsole.MarkupLine("\n[bold]Comentários:[/]");
                    foreach (var comment in incident.Comments)
                    {
                        AnsiConsole.MarkupLine($"  [dim]{comment.Author} ({comment.CreatedAt:yyyy-MM-dd}):[/] {comment.Text}");
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
        var createCommand = new Command("create", "Create new incident");
        var titleOption = new Option<string>("--title", "Incident title") { IsRequired = true };
        var descriptionOption = new Option<string>("--description", "Incident description") { IsRequired = true };
        var severityOption2 = new Option<string>("--severity", "Severity (Low/Medium/High/Critical)") { IsRequired = true };
        var environmentOption = new Option<string>("--environment", "Environment (dev/staging/prod)") { IsRequired = true };
        
        createCommand.AddOption(titleOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(severityOption2);
        createCommand.AddOption(environmentOption);
        
        createCommand.SetHandler(async (title, description, severity, environment) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                var request = new CreateIncidentRequest
                {
                    Title = title,
                    Description = description,
                    Severity = severity,
                    Environment = environment
                };

                AnsiConsole.MarkupLine("[yellow]Criando incidente...[/]");
                var incident = await incidentsService.CreateIncidentAsync(request);
                
                AnsiConsole.MarkupLine($"[green]✓ Incidente criado com sucesso![/]");
                AnsiConsole.MarkupLine($"[dim]ID: {incident.Id}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, titleOption, descriptionOption, severityOption2, environmentOption);

        // Update subcommand
        var updateCommand = new Command("update", "Update incident");
        var updateIdOption = new Option<string>("--id", "Incident ID") { IsRequired = true };
        var updateStatusOption = new Option<string?>("--status", "New status");
        var updateSeverityOption = new Option<string?>("--severity", "New severity");
        
        updateCommand.AddOption(updateIdOption);
        updateCommand.AddOption(updateStatusOption);
        updateCommand.AddOption(updateSeverityOption);
        
        updateCommand.SetHandler(async (id, status, severity) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                var request = new UpdateIncidentRequest
                {
                    Status = status,
                    Severity = severity
                };

                AnsiConsole.MarkupLine("[yellow]Atualizando incidente...[/]");
                await incidentsService.UpdateIncidentAsync(id, request);
                
                AnsiConsole.MarkupLine($"[green]✓ Incidente atualizado com sucesso![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, updateIdOption, updateStatusOption, updateSeverityOption);

        // Comment subcommand
        var commentCommand = new Command("comment", "Add comment to incident");
        var commentIdOption = new Option<string>("--id", "Incident ID") { IsRequired = true };
        var textOption = new Option<string>("--text", "Comment text") { IsRequired = true };
        
        commentCommand.AddOption(commentIdOption);
        commentCommand.AddOption(textOption);
        
        commentCommand.SetHandler(async (id, text) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                AnsiConsole.MarkupLine("[yellow]Adicionando comentário...[/]");
                await incidentsService.AddCommentAsync(id, text);
                
                AnsiConsole.MarkupLine($"[green]✓ Comentário adicionado com sucesso![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, commentIdOption, textOption);

        incidentsCommand.AddCommand(listCommand);
        incidentsCommand.AddCommand(getCommand);
        incidentsCommand.AddCommand(createCommand);
        incidentsCommand.AddCommand(updateCommand);
        incidentsCommand.AddCommand(commentCommand);

        return incidentsCommand;
    }

    private static string GetSeverityColor(string severity)
    {
        return severity.ToLower() switch
        {
            "critical" => "[red bold]Critical[/]",
            "high" => "[red]High[/]",
            "medium" => "[yellow]Medium[/]",
            "low" => "[green]Low[/]",
            _ => $"[{severity}]"
        };
    }

    private static string GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "open" => "[yellow]Open[/]",
            "resolved" => "[green]Resolved[/]",
            "closed" => "[dim]Closed[/]",
            _ => $"[{status}]"
        };
    }
}
