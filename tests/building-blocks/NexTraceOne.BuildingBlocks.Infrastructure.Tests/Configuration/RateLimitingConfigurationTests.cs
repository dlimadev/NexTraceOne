namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Configuration;

/// <summary>
/// Testes de validação da configuração de rate limiting no API host.
/// Garante que todas as políticas de rate limiting estão definidas e aplicadas
/// nos módulos de endpoints corretos para proteção contra abuso.
/// </summary>
public sealed class RateLimitingConfigurationTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static readonly string ProgramCsPath = Path.Combine(
        SolutionRoot, "src", "platform", "NexTraceOne.ApiHost", "Program.cs");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root.");
    }

    [Fact]
    public void ProgramCs_ContainsAuthRateLimitingPolicy()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("\"auth\"",
            "Program.cs must define an 'auth' rate limiting policy for authentication endpoints");
        content.Should().Contain("AddPolicy(\"auth\"",
            "Program.cs must register the 'auth' policy via AddPolicy");
    }

    [Fact]
    public void ProgramCs_ContainsAuthSensitiveRateLimitingPolicy()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("\"auth-sensitive\"",
            "Program.cs must define an 'auth-sensitive' rate limiting policy for sensitive auth operations");
        content.Should().Contain("AddPolicy(\"auth-sensitive\"",
            "Program.cs must register the 'auth-sensitive' policy via AddPolicy");
    }

    [Fact]
    public void ProgramCs_ContainsAiRateLimitingPolicy()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("\"ai\"",
            "Program.cs must define an 'ai' rate limiting policy for AI endpoints");
        content.Should().Contain("AddPolicy(\"ai\"",
            "Program.cs must register the 'ai' policy via AddPolicy");
    }

    [Fact]
    public void ProgramCs_ContainsDataIntensiveRateLimitingPolicy()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("\"data-intensive\"",
            "Program.cs must define a 'data-intensive' rate limiting policy for data-heavy endpoints");
        content.Should().Contain("AddPolicy(\"data-intensive\"",
            "Program.cs must register the 'data-intensive' policy via AddPolicy");
    }

    [Fact]
    public void ProgramCs_ContainsOperationsRateLimitingPolicy()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("\"operations\"",
            "Program.cs must define an 'operations' rate limiting policy for operational endpoints");
        content.Should().Contain("AddPolicy(\"operations\"",
            "Program.cs must register the 'operations' policy via AddPolicy");
    }

    [Fact]
    public void ProgramCs_ConfiguresGlobalRateLimiter()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("GlobalLimiter",
            "Program.cs must configure a global rate limiter");
        content.Should().Contain("429",
            "Program.cs must set rejection status code to 429 Too Many Requests");
    }

    [Fact]
    public void ProgramCs_UsesRateLimiterMiddleware()
    {
        var content = File.ReadAllText(ProgramCsPath);

        content.Should().Contain("UseRateLimiter()",
            "Program.cs must apply rate limiter middleware in the pipeline");
    }

    [Theory]
    [InlineData("Orchestration/Endpoints/Endpoints/AiOrchestrationEndpointModule.cs")]
    [InlineData("Runtime/Endpoints/Endpoints/AiRuntimeEndpointModule.cs")]
    [InlineData("ExternalAI/Endpoints/Endpoints/ExternalAiEndpointModule.cs")]
    public void AiEndpointModules_RequireAiRateLimiting(string relativeModulePath)
    {
        var aiModulesPath = Path.Combine(
            SolutionRoot, "src", "modules", "aiknowledge", "NexTraceOne.AIKnowledge.API");

        var filePath = Path.Combine(aiModulesPath, relativeModulePath);
        File.Exists(filePath).Should().BeTrue(
            $"AI endpoint module must exist at {filePath}");

        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileName(filePath);

        content.Should().Contain("RequireRateLimiting(\"ai\")",
            $"{fileName} must apply the 'ai' rate limiting policy to protect AI endpoints");
    }

    [Fact]
    public void CatalogEndpoint_RequiresDataIntensiveRateLimiting()
    {
        var catalogEndpointPath = Path.Combine(
            SolutionRoot, "src", "modules", "catalog", "NexTraceOne.Catalog.API",
            "Graph", "Endpoints", "Endpoints", "ServiceCatalogEndpointModule.cs");

        File.Exists(catalogEndpointPath).Should().BeTrue(
            $"Service catalog endpoint module must exist at {catalogEndpointPath}");

        var content = File.ReadAllText(catalogEndpointPath);

        content.Should().Contain("RequireRateLimiting(\"data-intensive\")",
            "Service catalog endpoint must apply the 'data-intensive' rate limiting policy");
    }

    [Fact]
    public void IncidentEndpoint_RequiresOperationsRateLimiting()
    {
        var incidentEndpointPath = Path.Combine(
            SolutionRoot, "src", "modules", "operationalintelligence",
            "NexTraceOne.OperationalIntelligence.API", "Incidents",
            "Endpoints", "Endpoints", "IncidentEndpointModule.cs");

        File.Exists(incidentEndpointPath).Should().BeTrue(
            $"Incident endpoint module must exist at {incidentEndpointPath}");

        var content = File.ReadAllText(incidentEndpointPath);

        content.Should().Contain("RequireRateLimiting(\"operations\")",
            "Incident endpoint must apply the 'operations' rate limiting policy");
    }
}
