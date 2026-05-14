using Spectre.Console;

namespace NexTraceOne.Cli.Services;

public class AuthenticationService
{
    private readonly ConfigurationService _configService;
    private readonly ApiService _apiService;

    public AuthenticationService(ConfigurationService configService)
    {
        _configService = configService;
        _apiService = new ApiService();
        _apiService.SetBaseUrl(_configService.GetEndpoint());
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            AnsiConsole.MarkupLine("[yellow]Autenticando...[/]");

            var response = await _apiService.PostAsync<LoginRequest, LoginResponse>(
                "/api/v1/identity/auth/login",
                new LoginRequest { Email = email, Password = password });

            if (!string.IsNullOrEmpty(response.Token))
            {
                _configService.SetToken(response.Token);
                _apiService.SetToken(response.Token);
                
                AnsiConsole.MarkupLine($"[green]✓ Login bem-sucedido![/]");
                AnsiConsole.MarkupLine($"[dim]Token válido até: {response.ExpiresAt}[/]");
                return true;
            }

            AnsiConsole.MarkupLine("[red]✗ Falha na autenticação[/]");
            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Erro: {ex.Message}[/]");
            return false;
        }
    }

    public void Logout()
    {
        _configService.ClearToken();
        AnsiConsole.MarkupLine("[green]✓ Logout realizado com sucesso[/]");
    }

    public bool IsAuthenticated()
    {
        return _configService.IsTokenValid();
    }

    public void ShowStatus()
    {
        if (IsAuthenticated())
        {
            AnsiConsole.MarkupLine("[green]✓ Autenticado[/]");
            AnsiConsole.MarkupLine($"[dim]Endpoint: {_configService.GetEndpoint()}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]○ Não autenticado[/]");
            AnsiConsole.MarkupLine("[dim]Use 'ntrace auth login' para autenticar[/]");
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (!_configService.IsTokenValid())
        {
            AnsiConsole.MarkupLine("[red]✗ Nenhum token válido para renovar[/]");
            return false;
        }

        try
        {
            AnsiConsole.MarkupLine("[yellow]Renovando token...[/]");

            var response = await _apiService.PostAsync<object, LoginResponse>(
                "/api/v1/identity/auth/refresh",
                new { });

            if (!string.IsNullOrEmpty(response.Token))
            {
                _configService.SetToken(response.Token);
                _apiService.SetToken(response.Token);
                
                AnsiConsole.MarkupLine($"[green]✓ Token renovado com sucesso![/]");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Erro ao renovar token: {ex.Message}[/]");
            return false;
        }
    }

    public string? GetToken()
    {
        return _configService.GetToken();
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
}
