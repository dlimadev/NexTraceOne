using NexTraceOne.Catalog.Application.Templates.Features.GenerateEnvironmentBlueprint;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Tests.Phase7Acceleration;

/// <summary>
/// Testes de unidade Phase 7.3 — GenerateEnvironmentBlueprint.
/// Cobre geração de Dockerfile, docker-compose, CI pipelines e Helm chart.
/// </summary>
public sealed class GenerateEnvironmentBlueprintTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Happy-path básico — DotNet com GitHub Actions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DotNet_GitHub_GeneratesExpectedFiles()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "my-service",
            Domain: "payments",
            Language: TemplateLanguage.DotNet,
            CiProvider: "github-actions");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("my-service");
        result.Value.Language.Should().Be("DotNet");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Files.Select(f => f.FileName).Should().Contain("Dockerfile");
        result.Value.Files.Select(f => f.FileName).Should().Contain("docker-compose.yml");
        result.Value.Files.Select(f => f.FileName).Should().Contain(".github/workflows/ci.yml");
    }

    [Fact]
    public async Task Handle_Dockerfile_DotNet_ContainsCorrectBaseImage()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "order-api",
            Domain: "orders",
            Language: TemplateLanguage.DotNet,
            ServicePort: 5000);

        var result = await handler.Handle(command, CancellationToken.None);

        var dockerfile = result.Value.Files.First(f => f.FileName == "Dockerfile");
        dockerfile.Content.Should().Contain("mcr.microsoft.com/dotnet/aspnet:10.0");
        dockerfile.Content.Should().Contain("5000");
        dockerfile.Content.Should().Contain("order-api.dll");
    }

    [Fact]
    public async Task Handle_Dockerfile_NodeJs_ContainsNodeBaseImage()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "frontend-api",
            Domain: "ui",
            Language: TemplateLanguage.NodeJs,
            ServicePort: 3000);

        var result = await handler.Handle(command, CancellationToken.None);

        var dockerfile = result.Value.Files.First(f => f.FileName == "Dockerfile");
        dockerfile.Content.Should().Contain("node:22-alpine");
        dockerfile.Content.Should().Contain("3000");
    }

    [Fact]
    public async Task Handle_Dockerfile_Java_ContainsJreBaseImage()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "java-svc",
            Domain: "backend",
            Language: TemplateLanguage.Java);

        var result = await handler.Handle(command, CancellationToken.None);

        var dockerfile = result.Value.Files.First(f => f.FileName == "Dockerfile");
        dockerfile.Content.Should().Contain("eclipse-temurin:21");
    }

    [Fact]
    public async Task Handle_DockerCompose_ContainsServiceAndNetwork()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "billing-service",
            Domain: "finance",
            Language: TemplateLanguage.DotNet);

        var result = await handler.Handle(command, CancellationToken.None);

        var compose = result.Value.Files.First(f => f.FileName == "docker-compose.yml");
        compose.Content.Should().Contain("billing-service");
        compose.Content.Should().Contain("finance-net");
        compose.Content.Should().Contain("healthcheck");
    }

    [Fact]
    public async Task Handle_DockerCompose_WithPostgres_ContainsPostgresService()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "user-service",
            Domain: "identity",
            Language: TemplateLanguage.DotNet,
            DatabaseType: "postgres");

        var result = await handler.Handle(command, CancellationToken.None);

        var compose = result.Value.Files.First(f => f.FileName == "docker-compose.yml");
        compose.Content.Should().Contain("postgres:16-alpine");
        compose.Content.Should().Contain("user_service");
    }

    [Fact]
    public async Task Handle_GitHubActions_CiYaml_ContainsBuildAndTestJobs()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "payment-api",
            Domain: "payments",
            Language: TemplateLanguage.DotNet,
            CiProvider: "github-actions");

        var result = await handler.Handle(command, CancellationToken.None);

        var ci = result.Value.Files.First(f => f.FileName == ".github/workflows/ci.yml");
        ci.Content.Should().Contain("build-and-test");
        ci.Content.Should().Contain("dotnet test");
        ci.Content.Should().Contain("payment-api");
    }

    [Fact]
    public async Task Handle_GitLabCi_ContainsStages()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "catalog-svc",
            Domain: "catalog",
            Language: TemplateLanguage.DotNet,
            CiProvider: "gitlab-ci");

        var result = await handler.Handle(command, CancellationToken.None);

        var ci = result.Value.Files.First(f => f.FileName == ".gitlab-ci.yml");
        ci.Content.Should().Contain("stages:");
        ci.Content.Should().Contain("- build");
        ci.Content.Should().Contain("- test");
    }

    [Fact]
    public async Task Handle_AzureDevOps_ContainsTriggerAndPool()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "auth-service",
            Domain: "identity",
            Language: TemplateLanguage.DotNet,
            CiProvider: "azure-devops");

        var result = await handler.Handle(command, CancellationToken.None);

        var ci = result.Value.Files.First(f => f.FileName == "azure-pipelines.yml");
        ci.Content.Should().Contain("trigger:");
        ci.Content.Should().Contain("vmImage: ubuntu-latest");
    }

    [Fact]
    public async Task Handle_WithHelmChart_GeneratesHelmFiles()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "notification-svc",
            Domain: "comms",
            Language: TemplateLanguage.DotNet,
            IncludeHelmChart: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Select(f => f.FileName).Should().Contain("helm/values.yaml");
        result.Value.Files.Select(f => f.FileName).Should().Contain("helm/Chart.yaml");
        var chart = result.Value.Files.First(f => f.FileName == "helm/Chart.yaml");
        chart.Content.Should().Contain("notification-svc");
    }

    [Fact]
    public async Task Handle_NoDockerCompose_DoesNotContainDockerComposeFile()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "simple-svc",
            Domain: "misc",
            Language: TemplateLanguage.DotNet,
            IncludeDockerCompose: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Select(f => f.FileName).Should().NotContain("docker-compose.yml");
    }

    [Fact]
    public async Task Handle_SummaryContainsGeneratedFileCount()
    {
        var handler = new GenerateEnvironmentBlueprint.Handler();
        var command = new GenerateEnvironmentBlueprint.Command(
            ServiceName: "test-svc",
            Domain: "tests",
            Language: TemplateLanguage.DotNet);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Summary.Should().Contain("file(s) generated");
        result.Value.Summary.Should().Contain(result.Value.Files.Count.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyServiceName_Fails()
    {
        var validator = new GenerateEnvironmentBlueprint.Validator();
        var result = validator.Validate(new GenerateEnvironmentBlueprint.Command(
            ServiceName: "",
            Domain: "payments",
            Language: TemplateLanguage.DotNet));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public void Validator_UpperCaseServiceName_Fails()
    {
        var validator = new GenerateEnvironmentBlueprint.Validator();
        var result = validator.Validate(new GenerateEnvironmentBlueprint.Command(
            ServiceName: "MyService",
            Domain: "payments",
            Language: TemplateLanguage.DotNet));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_InvalidCiProvider_Fails()
    {
        var validator = new GenerateEnvironmentBlueprint.Validator();
        var result = validator.Validate(new GenerateEnvironmentBlueprint.Command(
            ServiceName: "my-service",
            Domain: "payments",
            Language: TemplateLanguage.DotNet,
            CiProvider: "jenkins"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CiProvider");
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new GenerateEnvironmentBlueprint.Validator();
        var result = validator.Validate(new GenerateEnvironmentBlueprint.Command(
            ServiceName: "valid-service",
            Domain: "finance",
            Language: TemplateLanguage.DotNet,
            CiProvider: "gitlab-ci",
            ServicePort: 8080));
        result.IsValid.Should().BeTrue();
    }
}
