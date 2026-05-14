using System.CommandLine;
using NexTraceOne.Cli.Commands;
using NexTraceOne.Cli.Services;

namespace NexTraceOne.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Criar root command
        var rootCommand = new RootCommand("NexTraceOne CLI - Command-line interface for NexTraceOne platform");

        // Registrar serviços
        var configService = new ConfigurationService();
        var apiService = new ApiService();
        apiService.SetBaseUrl(configService.GetEndpoint());
        
        var authService = new AuthenticationService(configService);
        
        // Se token válido, configurar API service
        if (configService.IsTokenValid())
        {
            var token = configService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                apiService.SetToken(token);
            }
        }

        // Adicionar comandos
        rootCommand.AddCommand(AuthCommand.Create(authService));
        rootCommand.AddCommand(ContractsCommand.Create(apiService, authService));
        rootCommand.AddCommand(IncidentsCommand.Create(apiService, authService));
        rootCommand.AddCommand(NotificationsCommand.Create(apiService, authService));
        rootCommand.AddCommand(HealthCommand.Create(apiService));
        rootCommand.AddCommand(ConfigCommand.Create(configService));

        // Executar
        return await rootCommand.InvokeAsync(args);
    }
}
