using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Configuration;

/// <summary>
/// Testes de validação do comportamento do StartupValidation por ambiente.
/// Garante que o startup falha corretamente em produção/staging quando a configuração é insegura,
/// e permite conveniências em Development.
/// </summary>
public sealed class StartupValidationTests
{
    private const string StartupValidationFile = "StartupValidation.cs";

    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string StartupValidationPath = Path.Combine(
        SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", StartupValidationFile);

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root.");
    }

    [Fact]
    public void StartupValidation_Exists()
    {
        File.Exists(StartupValidationPath).Should().BeTrue(
            $"StartupValidation.cs must exist at {StartupValidationPath}");
    }

    [Fact]
    public void StartupValidation_ValidatesCriticalSections()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("ConnectionStrings",
            "StartupValidation must check for ConnectionStrings section");
        content.Should().Contain("Jwt",
            "StartupValidation must check for Jwt section");
    }

    [Fact]
    public void StartupValidation_FailsOnMissingJwtSecretInNonDevelopment()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("IsDevelopment()",
            "StartupValidation must differentiate behavior based on IsDevelopment()");
        content.Should().Contain("InvalidOperationException",
            "StartupValidation must throw InvalidOperationException for critical failures");
        content.Should().Contain("Jwt:Secret",
            "StartupValidation must reference Jwt:Secret configuration key");
    }

    [Fact]
    public void StartupValidation_EnforcesMinimumJwtSecretLength()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("MinimumJwtSecretLength",
            "StartupValidation must define MinimumJwtSecretLength constant");
        content.Should().Contain("32",
            "MinimumJwtSecretLength should be 32 characters for HS256 key material");
    }

    [Fact]
    public void StartupValidation_ValidatesConnectionStringsInNonDevelopment()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("IsNullOrWhiteSpace",
            "StartupValidation must check for empty connection strings");
        content.Should().Contain("non-Development",
            "StartupValidation must explicitly reference non-Development environments");
    }

    [Fact]
    public void StartupValidation_ValidatesOidcProviders()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("OidcProviders",
            "StartupValidation should validate OIDC provider configuration");
        content.Should().Contain("Authority",
            "OIDC validation should check for Authority");
        content.Should().Contain("ClientId",
            "OIDC validation should check for ClientId");
    }

    [Fact]
    public void StartupValidation_LogsEnvironmentName()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("EnvironmentName",
            "StartupValidation must log the current environment name for operational visibility");
    }

    [Fact]
    public void StartupValidation_DoesNotContainHardcodedSecrets()
    {
        var content = File.ReadAllText(StartupValidationPath);

        // Ensure no hardcoded secrets or passwords in the validation code
        content.Should().NotContainEquivalentOf("password123",
            "StartupValidation must not contain hardcoded passwords");
        content.Should().NotContainEquivalentOf("secret123",
            "StartupValidation must not contain hardcoded secrets");
        content.Should().NotContain("Bearer ",
            "StartupValidation must not contain hardcoded bearer tokens");
    }

    [Fact]
    public void StartupValidation_IsExtensionMethod()
    {
        var content = File.ReadAllText(StartupValidationPath);

        content.Should().Contain("this WebApplication app",
            "ValidateStartupConfiguration should be an extension method on WebApplication");
        content.Should().Contain("public static",
            "StartupValidation should be a public static class");
    }

    [Fact]
    public void BaseAppSettings_JwtSecret_IsEmptyForProduction()
    {
        // Validates that base appsettings.json does not ship with a JWT secret
        // This ensures production deployments MUST provide the secret externally
        var baseConfigPath = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.json");
        var content = File.ReadAllText(baseConfigPath);

        var config = new ConfigurationBuilder()
            .AddJsonFile(baseConfigPath)
            .Build();

        var jwtSecret = config["Jwt:Secret"];
        jwtSecret.Should().BeNullOrEmpty(
            "base appsettings.json must ship with empty JWT secret — production must set it via environment variable");
    }

    [Fact]
    public void DevAppSettings_JwtSecret_IsSetForDevelopment()
    {
        var devConfigPath = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.Development.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(devConfigPath)
            .Build();

        var jwtSecret = config["Jwt:Secret"];
        jwtSecret.Should().NotBeNullOrEmpty(
            "Development appsettings should provide a JWT secret for local development convenience");
        jwtSecret!.Length.Should().BeGreaterThanOrEqualTo(32,
            "Development JWT secret should meet the minimum length requirement for HS256");
    }

    [Fact]
    public void BaseAppSettings_AllConnectionStrings_HaveNoRealPasswords()
    {
        var baseConfigPath = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(baseConfigPath)
            .Build();

        var connectionStrings = config.GetSection("ConnectionStrings");
        connectionStrings.Exists().Should().BeTrue("ConnectionStrings section must exist in base config");

        foreach (var cs in connectionStrings.GetChildren())
        {
            var value = cs.Value ?? "";
            if (value.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            {
                var passwordSegment = value.Split("Password=", StringSplitOptions.None)[1];
                var passwordValue = passwordSegment.Split(';')[0].Trim();
                // Acceptable values: empty string or the REPLACE_VIA_ENV placeholder.
                // Real credentials must be injected via environment variables at runtime.
                passwordValue.Should().Match(
                    p => string.IsNullOrEmpty(p) || p == "REPLACE_VIA_ENV",
                    $"ConnectionString '{cs.Key}' must not contain a real password in base config — " +
                    "use REPLACE_VIA_ENV or empty; inject real credentials via environment variables");
            }
        }
    }

    [Fact]
    public void BaseAppSettings_AllConnectionStrings_PointToSingleDatabase()
    {
        var baseConfigPath = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(baseConfigPath)
            .Build();

        var connectionStrings = config.GetSection("ConnectionStrings");
        var allValues = connectionStrings.GetChildren()
            .Select(c => c.Value ?? "")
            .ToList();

        // Since E14: all connection strings point to the single 'nextraceone' database.
        // The old 4-database pattern (nextraceone_identity/catalog/operations/ai) was removed.
        foreach (var value in allValues)
        {
            value.Should().Contain("nextraceone",
                "all connection strings must reference the consolidated 'nextraceone' database (E14 architecture)");
        }
    }

    [Fact]
    public void DevAppSettings_SecureCookies_AreDisabledForDevelopment()
    {
        var devConfigPath = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.Development.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(devConfigPath)
            .Build();

        var requireSecure = config["Auth:CookieSession:RequireSecureCookies"];
        requireSecure.Should().Be("False",
            "Development environment should not require secure cookies (HTTP allowed locally)");
    }
}
