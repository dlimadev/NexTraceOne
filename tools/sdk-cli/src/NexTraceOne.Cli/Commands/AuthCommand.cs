using System.CommandLine;
using NexTraceOne.Cli.Services;
using Spectre.Console;

namespace NexTraceOne.Cli.Commands;

public static class AuthCommand
{
    public static Command Create(AuthenticationService authService)
    {
        var authCommand = new Command("auth", "Authentication commands");

        // Login subcommand
        var loginCommand = new Command("login", "Login to NexTraceOne");
        var emailOption = new Option<string>("--email", "Email address") { IsRequired = true };
        var passwordOption = new Option<string>("--password", "Password") { IsRequired = true };
        
        loginCommand.AddOption(emailOption);
        loginCommand.AddOption(passwordOption);
        
        loginCommand.SetHandler(async (email, password) =>
        {
            var success = await authService.LoginAsync(email, password);
            Environment.Exit(success ? 0 : 1);
        }, emailOption, passwordOption);

        // Logout subcommand
        var logoutCommand = new Command("logout", "Logout from NexTraceOne");
        logoutCommand.SetHandler(() =>
        {
            authService.Logout();
        });

        // Status subcommand
        var statusCommand = new Command("status", "Check authentication status");
        statusCommand.SetHandler(() =>
        {
            authService.ShowStatus();
        });

        // Refresh subcommand
        var refreshCommand = new Command("refresh", "Refresh authentication token");
        refreshCommand.SetHandler(async () =>
        {
            var success = await authService.RefreshTokenAsync();
            Environment.Exit(success ? 0 : 1);
        });

        authCommand.AddCommand(loginCommand);
        authCommand.AddCommand(logoutCommand);
        authCommand.AddCommand(statusCommand);
        authCommand.AddCommand(refreshCommand);

        return authCommand;
    }
}
