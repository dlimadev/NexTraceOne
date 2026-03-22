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
    public void BaseAppSettings_JwtSecret_ShouldBeEmpty()
    {
        var json = ReadJson(BaseAppSettings);
        var secret = json.GetProperty("Jwt").GetProperty("Secret").GetString();

        secret.Should().BeNullOrEmpty("base appsettings.json must not contain a real JWT secret");
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
            // Password= should be empty (Password=;) — no actual value after it
            if (value.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            {
                var passwordSegment = value.Split("Password=", StringSplitOptions.None)[1];
                var passwordValue = passwordSegment.Split(';')[0].Trim();
                passwordValue.Should().BeNullOrEmpty($"connection string '{prop.Name}' must not have a real password in base appsettings.json");
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
    public void BaseAppSettings_ShouldHave19ConnectionStrings()
    {
        var json = ReadJson(BaseAppSettings);
        var connStrings = json.GetProperty("ConnectionStrings");

        connStrings.EnumerateObject().Count().Should().Be(19, "expected 19 connection strings for all DbContexts");
    }

    [Fact]
    public void DevAppSettings_JwtSecret_ShouldBeExplicitlyDevelopmentOnly()
    {
        var json = ReadJson(DevAppSettings);
        var secret = json.GetProperty("Jwt").GetProperty("Secret").GetString();

        secret.Should().NotBeNullOrEmpty("dev config should have a JWT secret for local development");
        secret.Should().Contain("Development", "dev JWT secret should clearly identify as development-only");
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
