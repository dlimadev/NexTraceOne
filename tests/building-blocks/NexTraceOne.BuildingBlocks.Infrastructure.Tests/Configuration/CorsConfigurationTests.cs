namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Configuration;

/// <summary>
/// Testes de validação da configuração CORS no API host.
/// Garante que origens wildcard são rejeitadas, credenciais são permitidas,
/// e que headers e métodos obrigatórios estão configurados.
/// </summary>
public sealed class CorsConfigurationTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static readonly string CorsConfigPath = Path.Combine(
        SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "WebApplicationBuilderExtensions.cs");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root.");
    }

    [Fact]
    public void CorsConfig_Exists()
    {
        File.Exists(CorsConfigPath).Should().BeTrue(
            $"WebApplicationBuilderExtensions.cs must exist at {CorsConfigPath}");
    }

    [Fact]
    public void CorsConfig_RequiresExplicitOrigins_InNonDevelopmentEnvironments()
    {
        var content = File.ReadAllText(CorsConfigPath);

        content.Should().Contain("isNonDevelopmentEnvironment",
            "CORS configuration must distinguish between development and non-development environments");
        content.Should().Contain("InvalidOperationException",
            "CORS configuration must throw when origins are missing in non-development environments");
        content.Should().Contain("Cors:AllowedOrigins",
            "CORS configuration must reference 'Cors:AllowedOrigins' configuration key");
    }

    [Fact]
    public void CorsConfig_RejectsWildcardOrigins()
    {
        var content = File.ReadAllText(CorsConfigPath);

        content.Should().Contain("Contains('*')",
            "CORS configuration must check for wildcard characters in origins");
        content.Should().Contain("Wildcards are not allowed",
            "CORS configuration must reject wildcard origins with a clear error message");
    }

    [Fact]
    public void CorsConfig_AllowsCredentials()
    {
        var content = File.ReadAllText(CorsConfigPath);

        content.Should().Contain("AllowCredentials()",
            "CORS configuration must enable AllowCredentials for cookie/auth-based requests");
    }

    [Fact]
    public void CorsConfig_AllowsRequiredHeaders()
    {
        var content = File.ReadAllText(CorsConfigPath);

        var requiredHeaders = new[]
        {
            "Content-Type",
            "Authorization",
            "X-Tenant-Id",
            "X-Environment-Id",
            "X-Correlation-Id",
            "X-Csrf-Token"
        };

        foreach (var header in requiredHeaders)
        {
            content.Should().Contain(header,
                $"CORS configuration must allow the '{header}' header");
        }
    }

    [Fact]
    public void CorsConfig_AllowsRequiredMethods()
    {
        var content = File.ReadAllText(CorsConfigPath);

        var requiredMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };

        foreach (var method in requiredMethods)
        {
            content.Should().Contain(method,
                $"CORS configuration must allow the '{method}' HTTP method");
        }
    }

    [Fact]
    public void CorsConfig_HasDevelopmentFallbackOrigins()
    {
        var content = File.ReadAllText(CorsConfigPath);

        content.Should().Contain("localhost:5173",
            "CORS configuration must include localhost:5173 as a development fallback origin");
        content.Should().Contain("localhost:3000",
            "CORS configuration must include localhost:3000 as a development fallback origin");
    }

    [Fact]
    public void CorsConfig_IsExtensionMethod()
    {
        var content = File.ReadAllText(CorsConfigPath);

        content.Should().Contain("this WebApplicationBuilder builder",
            "AddCorsConfiguration must be an extension method on WebApplicationBuilder");
        content.Should().Contain("public static void AddCorsConfiguration",
            "AddCorsConfiguration must be a public static void method");
    }

    [Fact]
    public void ProgramCs_UsesCorsMiddleware()
    {
        var programPath = Path.Combine(
            SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "Program.cs");

        var content = File.ReadAllText(programPath);

        content.Should().Contain("UseCors()",
            "Program.cs must apply CORS middleware in the pipeline");
        content.Should().Contain("AddCorsConfiguration()",
            "Program.cs must call AddCorsConfiguration during service registration");
    }
}
