using System.Text.Json;
using FluentAssertions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Configuration;

/// <summary>
/// Testes que garantem que appsettings.json base não contém segredos reais,
/// que a estrutura de MaxPoolSize é coerente e que configuraçoes críticas existem.
/// </summary>
public sealed class AppSettingsSecurityTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string BaseAppSettings = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.json");
    private static readonly string DevAppSettings = Path.Combine(SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "appsettings.Development.json");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root.");
    }

    [Fact]
    public void BaseAppSettings_JwtSecret_ShouldBeAbsent()
    {
        var json = ReadJson(BaseAppSettings);
        var jwtSection = json.GetProperty("Jwt");

        // The "Secret" key must not exist in base appsettings.json.
        // Production deployments must supply Jwt__Secret via environment variable or secrets manager.
        jwtSection.TryGetProperty("Secret", out _).Should().BeFalse(
            "base appsettings.json must not contain a Jwt:Secret key — " +
            "supply the secret at runtime via the Jwt__Secret environment variable");
    }

    [Fact]
    public void BaseAppSettings_OpenAiApiKey_ShouldBeEmpty()
    {
        var json = ReadJson(BaseAppSettings);
        var apiKey = json.GetProperty("AiRuntime").GetProperty("OpenAI").GetProperty("ApiKey").GetString();

        apiKey.Should().BeNullOrEmpty("base appsettings.json must not contain a real OpenAI API key");
    }

    [Fact]
    public void BaseAppSettings_ConnectionStrings_ShouldNotContainRealPasswords()
    {
        var json = ReadJson(BaseAppSettings);
        var connStrings = json.GetProperty("ConnectionStrings");

        foreach (var prop in connStrings.EnumerateObject())
        {
            var value = prop.Value.GetString()!;
            // Password must be empty or the REPLACE_VIA_ENV placeholder — no real credentials in base config.
            // Real credentials must be injected via environment variables (Npgsql env var or NEXTRACE_ prefix).
            if (value.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            {
                var passwordSegment = value.Split("Password=", StringSplitOptions.None)[1];
                var passwordValue = passwordSegment.Split(';')[0].Trim();
                passwordValue.Should().Match(
                    p => string.IsNullOrEmpty(p) || p == "REPLACE_VIA_ENV",
                    $"connection string '{prop.Name}' must not have a real password in base appsettings.json (use REPLACE_VIA_ENV or empty)");
            }
        }
    }

    [Fact]
    public void BaseAppSettings_MaxPoolSize_ShouldBe10ForAllConnections()
    {
        var json = ReadJson(BaseAppSettings);
        var connStrings = json.GetProperty("ConnectionStrings");

        foreach (var prop in connStrings.EnumerateObject())
        {
            var value = prop.Value.GetString()!;
            value.Should().Contain("Maximum Pool Size=10", $"connection string '{prop.Name}' in base config should have pool size 10");
        }
    }

    [Fact]
    public void DevAppSettings_MaxPoolSize_ShouldNotExceed20()
    {
        var json = ReadJson(DevAppSettings);
        var connStrings = json.GetProperty("ConnectionStrings");

        foreach (var prop in connStrings.EnumerateObject())
        {
            var value = prop.Value.GetString()!;
            if (value.Contains("Maximum Pool Size="))
            {
                var poolStr = value.Split("Maximum Pool Size=")[1].Split(';')[0].Trim();
                var poolSize = int.Parse(poolStr);
                poolSize.Should().BeLessThanOrEqualTo(20, $"connection string '{prop.Name}' in dev config should not exceed pool size 20");
            }
        }
    }

    [Fact]
    public void BaseAppSettings_ShouldHave21ConnectionStrings()
    {
        var json = ReadJson(BaseAppSettings);
        var connStrings = json.GetProperty("ConnectionStrings");

        connStrings.EnumerateObject().Count().Should().Be(21, "expected 21 connection strings for all DbContexts (E14+E15 architecture: NexTraceOne + 19 module-specific + IntegrationsDatabase added in P2.1)");
    }

    [Fact]
    public void DevAppSettings_JwtSecret_ShouldNotBeHardcoded()
    {
        var json = ReadJson(DevAppSettings);

        // The dev config must NOT have a hardcoded JWT secret committed to source control.
        // The secret must be provided externally via dotnet user-secrets or environment variable.
        if (json.TryGetProperty("Jwt", out var jwtSection) && jwtSection.TryGetProperty("Secret", out var secretProp))
        {
            var secret = secretProp.GetString();
            secret.Should().Match(
                s => string.IsNullOrEmpty(s) || s == "REPLACE_VIA_ENV",
                "dev config must not contain a real hardcoded JWT secret — " +
                "configure via dotnet user-secrets or the Jwt__Secret environment variable");
        }
        // If the Jwt section or Secret key is absent, the constraint is automatically satisfied.
    }

    [Fact]
    public void DevAppSettings_ShouldHaveCommentAboutNotCommittingSecrets()
    {
        var content = File.ReadAllText(DevAppSettings);
        content.Should().Contain("Do NOT commit real passwords", "dev config should warn against committing real secrets");
    }

    private static JsonElement ReadJson(string path)
    {
        var content = File.ReadAllText(path);
        return JsonDocument.Parse(content).RootElement;
    }
}
