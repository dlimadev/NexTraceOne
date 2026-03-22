using System.CommandLine;
using System.Text.Json;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex catalog' — consulta o catálogo de serviços e APIs do NexTraceOne.
/// Subcomandos: list, get.
/// </summary>
public static class CatalogCommand
{
    private static readonly JsonSerializerOptions JsonPrintOptions = new() { WriteIndented = true };

    public static Command Create()
    {
        var command = new Command("catalog", "Query the NexTraceOne service catalog.");

        command.Add(CreateListCommand());
        command.Add(CreateGetCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var urlOption = CreateUrlOption();
        var formatOption = CreateFormatOption();

        var command = new Command("list", "List all services in the catalog.");
        command.Add(urlOption);
        command.Add(formatOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var url = parseResult.GetValue(urlOption)!;
            var format = parseResult.GetValue(formatOption) ?? "text";
            return ListAsync(url, format, cancellationToken);
        });

        return command;
    }

    private static Command CreateGetCommand()
    {
        var idArgument = new Argument<string>("id")
        {
            Description = "The service identifier to retrieve."
        };

        var urlOption = CreateUrlOption();
        var formatOption = CreateFormatOption();

        var command = new Command("get", "Get details of a specific service from the catalog.");
        command.Add(idArgument);
        command.Add(urlOption);
        command.Add(formatOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArgument)!;
            var url = parseResult.GetValue(urlOption)!;
            var format = parseResult.GetValue(formatOption) ?? "text";
            return GetAsync(id, url, format, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ListAsync(string apiUrl, string format, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new CatalogApiClient(apiUrl);
            var response = await client.ListServicesAsync(cancellationToken).ConfigureAwait(false);

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(response, JsonPrintOptions));
                return 0;
            }

            if (response.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No services found in the catalog.[/]");
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Service Catalog[/]")
                .AddColumn(new TableColumn("[bold]ID[/]"))
                .AddColumn(new TableColumn("[bold]Name[/]"))
                .AddColumn(new TableColumn("[bold]Domain[/]"))
                .AddColumn(new TableColumn("[bold]Owner[/]"))
                .AddColumn(new TableColumn("[bold]Criticality[/]"))
                .AddColumn(new TableColumn("[bold]Status[/]"));

            foreach (var service in response.Items)
            {
                var statusMarkup = FormatStatus(service.Status);
                table.AddRow(
                    (service.ServiceId ?? "-").EscapeMarkup(),
                    (service.Name ?? "-").EscapeMarkup(),
                    (service.Domain ?? "-").EscapeMarkup(),
                    (service.Owner ?? "-").EscapeMarkup(),
                    FormatCriticality(service.Criticality),
                    statusMarkup);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Total: {response.TotalCount} service(s)[/]");
            return 0;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not connect to the catalog API at [yellow]{apiUrl.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request to catalog API timed out.");
            return 1;
        }
    }

    private static async Task<int> GetAsync(string serviceId, string apiUrl, string format, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new CatalogApiClient(apiUrl);
            var service = await client.GetServiceAsync(serviceId, cancellationToken).ConfigureAwait(false);

            if (service is null)
            {
                AnsiConsole.MarkupLine($"[yellow]Service '{serviceId.EscapeMarkup()}' not found.[/]");
                return 1;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(service, JsonPrintOptions));
                return 0;
            }

            var panel = new Panel(BuildServiceGrid(service))
                .Header($"[bold cyan]Service: {(service.Name ?? serviceId).EscapeMarkup()}[/]")
                .Border(BoxBorder.Rounded)
                .Expand();

            AnsiConsole.Write(panel);
            return 0;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            AnsiConsole.MarkupLine($"[yellow]Service '{serviceId.EscapeMarkup()}' not found.[/]");
            return 1;
        }
        catch (UriFormatException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid API URL: [yellow]{apiUrl.EscapeMarkup()}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not connect to the catalog API at [yellow]{apiUrl.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[grey]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Request to catalog API timed out.");
            return 1;
        }
    }

    private static Grid BuildServiceGrid(ServiceDetail service)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().PadRight(4))
            .AddColumn();

        AddGridRow(grid, "ID", service.ServiceId);
        AddGridRow(grid, "Name", service.Name);
        AddGridRow(grid, "Domain", service.Domain);
        AddGridRow(grid, "Owner", service.Owner);
        AddGridRow(grid, "Team", service.Team);
        AddGridRow(grid, "Type", service.Type);
        AddGridRow(grid, "Version", service.Version);
        AddGridRow(grid, "Criticality", service.Criticality);
        AddGridRow(grid, "Status", service.Status);
        AddGridRow(grid, "Description", service.Description);

        if (service.Tags is { Length: > 0 })
            AddGridRow(grid, "Tags", string.Join(", ", service.Tags));

        if (service.CreatedAt.HasValue)
            AddGridRow(grid, "Created", service.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture));

        if (service.UpdatedAt.HasValue)
            AddGridRow(grid, "Updated", service.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture));

        return grid;
    }

    private static void AddGridRow(Grid grid, string label, string? value)
    {
        grid.AddRow(
            new Markup($"[bold]{label.EscapeMarkup()}:[/]"),
            new Markup((value ?? "-").EscapeMarkup()));
    }

    private static string FormatStatus(string? status) =>
        (status?.ToLowerInvariant()) switch
        {
            "active" or "healthy" => $"[green]{(status ?? "-").EscapeMarkup()}[/]",
            "degraded" or "warning" => $"[yellow]{(status ?? "-").EscapeMarkup()}[/]",
            "inactive" or "critical" or "down" => $"[red]{(status ?? "-").EscapeMarkup()}[/]",
            _ => (status ?? "-").EscapeMarkup()
        };

    private static string FormatCriticality(string? criticality) =>
        (criticality?.ToLowerInvariant()) switch
        {
            "critical" or "tier1" => $"[red]{(criticality ?? "-").EscapeMarkup()}[/]",
            "high" or "tier2" => $"[yellow]{(criticality ?? "-").EscapeMarkup()}[/]",
            _ => (criticality ?? "-").EscapeMarkup()
        };

    private static Option<string> CreateUrlOption()
    {
        return new Option<string>("--url")
        {
            Description = "NexTraceOne API base URL (default: NEX_API_URL env var or http://localhost:8080).",
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("NEX_API_URL") ?? "http://localhost:8080"
        };
    }

    private static Option<string> CreateFormatOption()
    {
        return new Option<string>("--format")
        {
            Description = "Output format: text (default) or json.",
            DefaultValueFactory = _ => "text"
        };
    }
}
