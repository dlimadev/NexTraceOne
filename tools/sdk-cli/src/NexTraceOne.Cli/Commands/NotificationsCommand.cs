using System;

namespace NexTraceOne.Cli.Models;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using NexTraceOne.Cli.Models;

namespace NexTraceOne.Cli.Services;

public class NotificationsService
{
    private readonly ApiService _apiService;

    public NotificationsService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<NotificationDto>> ListNotificationsAsync(bool unreadOnly = false)
    {
        string endpoint = "/api/v1/notifications";
        if (unreadOnly)
        {
            endpoint += "?unread=true";
        }

        return await _apiService.GetAsync<List<NotificationDto>>(endpoint);
    }

    public async Task MarkAsReadAsync(string id)
    {
        await _apiService.PostAsync<object>($"/api/v1/notifications/{id}/read", null);
    }

    public async Task SendTestNotificationAsync(string message)
    {
        var payload = new { message };
        await _apiService.PostAsync<object>("/api/v1/notifications/test", payload);
    }
}
using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class NotificationsCommand
{
    public static Command Create(ApiService apiService, AuthenticationService authService)
    {
        var notificationsService = new NotificationsService(apiService);
        var notificationsCommand = new Command("notifications", "Notification management");

        // List subcommand
        var listCommand = new Command("list", "List notifications");
        var unreadOption = new Option<bool>("--unread", () => false, "Show only unread");
        
        listCommand.AddOption(unreadOption);
        listCommand.SetHandler(async (unread) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                var notifications = await notificationsService.ListNotificationsAsync(unread);
                
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Message");
                table.AddColumn("Type");
                table.AddColumn("Status");
                table.AddColumn("Date");

                foreach (var notification in notifications)
                {
                    table.AddRow(
                        notification.Id,
                        notification.Message,
                        notification.Type,
                        notification.IsRead ? "[green]✓ Read[/]" : "[yellow]○ Unread[/]",
                        notification.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {notifications.Count} notificações[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, unreadOption);

        // Read subcommand
        var readCommand = new Command("read", "Mark notification as read");
        var idOption = new Option<string>("--id", "Notification ID") { IsRequired = true };
        
        readCommand.AddOption(idOption);
        readCommand.SetHandler(async (id) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                AnsiConsole.MarkupLine("[yellow]Marcando como lida...[/]");
                await notificationsService.MarkAsReadAsync(id);
                
                AnsiConsole.MarkupLine($"[green]✓ Notificação marcada como lida![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, idOption);

        // Test subcommand
        var testCommand = new Command("test", "Send test notification");
        var messageOption = new Option<string>("--message", "Test message") { IsRequired = true };
        
        testCommand.AddOption(messageOption);
        testCommand.SetHandler(async (message) =>
        {
            if (!authService.IsAuthenticated())
            {
                AnsiConsole.MarkupLine("[red]✗ Autenticação necessária[/]");
                Environment.Exit(1);
            }

            try
            {
                AnsiConsole.MarkupLine("[yellow]Enviando notificação de teste...[/]");
                await notificationsService.SendTestNotificationAsync(message);
                
                AnsiConsole.MarkupLine($"[green]✓ Notificação de teste enviada com sucesso![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, messageOption);

        notificationsCommand.AddCommand(listCommand);
        notificationsCommand.AddCommand(readCommand);
        notificationsCommand.AddCommand(testCommand);

        return notificationsCommand;
    }
}
