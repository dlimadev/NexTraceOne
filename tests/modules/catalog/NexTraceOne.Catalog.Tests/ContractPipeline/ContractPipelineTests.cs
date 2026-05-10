using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateClientSdkFromContract;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateContractTests;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GeneratePostmanCollection;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateServerFromContract;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.ContractPipeline;

/// <summary>
/// Testes de unidade Phase 4 — Contract-to-Code Pipeline.
/// </summary>
public sealed class ContractPipelineTests
{
    private static readonly Guid ContractVersionId = Guid.NewGuid();

    private static readonly string SampleOpenApiJson = """
        {
            "openapi": "3.0.0",
            "info": { "title": "Users API", "version": "1.0.0" },
            "paths": {
                "/users": {
                    "get": { "summary": "List users", "responses": {} },
                    "post": { "summary": "Create user", "responses": {} }
                },
                "/users/{id}": {
                    "get": { "summary": "Get user by id", "responses": {} },
                    "delete": { "summary": "Delete user", "responses": {} }
                }
            }
        }
        """;

    /// <summary>
    /// Cria um mock de IContractVersionRepository que retorna uma ContractVersion com o spec indicado.
    /// </summary>
    private static IContractVersionRepository MockRepo(string specContent)
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", specContent, "json", "test").Value;
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(version);
        return repo;
    }

    /// <summary>Cria um mock de repositório que retorna null (versão não encontrada).</summary>
    private static IContractVersionRepository MockRepoNotFound()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        return repo;
    }

    // ── GenerateServerFromContract ────────────────────────────────────────

    [Fact]
    public async Task GenerateServerFromContract_DotNet_GeneratesControllerFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "dotnet", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Files.Should().Contain(f => f.FileName.EndsWith(".cs"));
        result.Value.TargetLanguage.Should().Be("dotnet");
    }

    [Fact]
    public async Task GenerateServerFromContract_TypeScript_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "nodejs", "UsersSvc");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Files.Should().Contain(f => f.Language == "javascript");
    }

    [Fact]
    public async Task GenerateServerFromContract_Java_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "java", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "java");
    }

    [Fact]
    public async Task GenerateServerFromContract_Go_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "go", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "go");
    }

    [Fact]
    public async Task GenerateServerFromContract_Python_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "python", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "python");
    }

    [Fact]
    public void GenerateServerFromContract_Validator_UnsupportedLanguage_Fails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(ContractVersionId, "pascal", "Svc"));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateMockServer ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateMockServer_WireMock_GeneratesStubFiles()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GenerateMockServer.Handler(repo, NullLogger<GenerateMockServer.Handler>.Instance);
        var command = new GenerateMockServer.Command(ContractVersionId, "wiremock");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MockServerType.Should().Be("wiremock");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Instructions.Should().Contain("WireMock");
    }

    [Fact]
    public async Task GenerateMockServer_JsonServer_GeneratesDbAndRoutes()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GenerateMockServer.Handler(repo, NullLogger<GenerateMockServer.Handler>.Instance);
        var command = new GenerateMockServer.Command(ContractVersionId, "json-server");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.FileName == "db.json");
        result.Value.Files.Should().Contain(f => f.FileName == "routes.json");
        result.Value.Instructions.Should().Contain("json-server");
    }

    [Fact]
    public async Task GenerateMockServer_ContractNotFound_ReturnsNotFoundError()
    {
        var repo = MockRepoNotFound();
        var handler = new GenerateMockServer.Handler(repo, NullLogger<GenerateMockServer.Handler>.Instance);
        var command = new GenerateMockServer.Command(ContractVersionId, "wiremock");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("contract.version.not_found");
    }

    [Fact]
    public void GenerateMockServer_Validator_InvalidType_Fails()
    {
        var validator = new GenerateMockServer.Validator();
        var result = validator.Validate(new GenerateMockServer.Command(ContractVersionId, "swagger-mock"));
        result.IsValid.Should().BeFalse();
    }

    // ── GeneratePostmanCollection ─────────────────────────────────────────

    [Fact]
    public async Task GeneratePostmanCollection_ValidContract_GeneratesCollection()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GeneratePostmanCollection.Handler(repo);
        var command = new GeneratePostmanCollection.Command(ContractVersionId, "Users API Collection");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CollectionJson.Should().Contain("Users API Collection");
        result.Value.EndpointCount.Should().Be(4); // GET /users, POST /users, GET /users/{id}, DELETE /users/{id}
    }

    [Fact]
    public async Task GeneratePostmanCollection_InvalidJson_GeneratesEmptyCollection()
    {
        var repo = MockRepo("invalid json");
        var handler = new GeneratePostmanCollection.Handler(repo);
        var command = new GeneratePostmanCollection.Command(ContractVersionId, "Test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EndpointCount.Should().Be(0);
    }

    [Fact]
    public async Task GeneratePostmanCollection_ContractNotFound_ReturnsNotFoundError()
    {
        var repo = MockRepoNotFound();
        var handler = new GeneratePostmanCollection.Handler(repo);
        var command = new GeneratePostmanCollection.Command(ContractVersionId, "Test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("contract.version.not_found");
    }

    [Fact]
    public void GeneratePostmanCollection_Validator_EmptyName_Fails()
    {
        var validator = new GeneratePostmanCollection.Validator();
        var result = validator.Validate(new GeneratePostmanCollection.Command(ContractVersionId, ""));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateContractTests ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateContractTests_XUnit_GeneratesTestFile()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GenerateContractTests.Handler(repo);
        var command = new GenerateContractTests.Command(ContractVersionId, "UsersService", "xunit");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TestFramework.Should().Be("xunit");
        result.Value.TestCount.Should().Be(4);
        result.Value.Files.Should().Contain(f => f.Language == "csharp");
    }

    [Fact]
    public async Task GenerateContractTests_Jest_GeneratesTestFile()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GenerateContractTests.Handler(repo);
        var command = new GenerateContractTests.Command(ContractVersionId, "UsersService", "jest");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "javascript");
    }

    [Fact]
    public async Task GenerateContractTests_Robot_GeneratesTestFile()
    {
        var repo = MockRepo(SampleOpenApiJson);
        var handler = new GenerateContractTests.Handler(repo);
        var command = new GenerateContractTests.Command(ContractVersionId, "UsersService", "robot");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "robot");
    }

    [Fact]
    public async Task GenerateContractTests_ContractNotFound_ReturnsNotFoundError()
    {
        var repo = MockRepoNotFound();
        var handler = new GenerateContractTests.Handler(repo);
        var command = new GenerateContractTests.Command(ContractVersionId, "UsersService", "xunit");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("contract.version.not_found");
    }

    [Fact]
    public void GenerateContractTests_Validator_UnsupportedFramework_Fails()
    {
        var validator = new GenerateContractTests.Validator();
        var result = validator.Validate(new GenerateContractTests.Command(ContractVersionId, "Svc", "mocha-chai"));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateClientSdkFromContract ─────────────────────────────────────

    [Fact]
    public async Task GenerateClientSdkFromContract_DotNet_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "dotnet", "UsersApiClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "csharp");
        result.Value.ClientName.Should().Be("UsersApiClient");
    }

    [Fact]
    public async Task GenerateClientSdkFromContract_TypeScript_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "typescript", "UsersClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "typescript");
    }

    [Fact]
    public async Task GenerateClientSdkFromContract_Python_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "python", "UsersClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "python");
    }

    [Fact]
    public void GenerateClientSdkFromContract_Validator_UnsupportedLanguage_Fails()
    {
        var validator = new GenerateClientSdkFromContract.Validator();
        var result = validator.Validate(new GenerateClientSdkFromContract.Command(ContractVersionId, "ruby", "Client"));
        result.IsValid.Should().BeFalse();
    }

    // ── Shared tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateServerFromContract_EmptyServiceName_ValidatorFails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(ContractVersionId, "dotnet", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateServerFromContract_EmptyContractVersion_ValidatorFails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(Guid.Empty, "dotnet", "Svc"));
        result.IsValid.Should().BeFalse();
    }

    // ── OPS-02: GenerateServerFromContract template quality ───────────────

    [Theory]
    [InlineData("dotnet")]
    [InlineData("java")]
    [InlineData("nodejs")]
    [InlineData("go")]
    [InlineData("python")]
    public async Task GenerateServerFromContract_NoLanguage_HasNoTodoComments(string language)
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, language, "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        foreach (var file in result.Value.Files)
        {
            file.Content.Should().NotContain("// TODO:", $"file {file.FileName} should not contain TODO comments");
            file.Content.Should().NotContain("# TODO:", $"file {file.FileName} should not contain TODO comments");
        }
    }

    [Fact]
    public async Task GenerateServerFromContract_DotNet_GeneratesProjectFileAndInterface()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "dotnet", "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Should().Contain(f => f.FileName.EndsWith(".csproj"), "should include .csproj project file");
        result.Value.Files.Should().Contain(f => f.FileName.Contains("Service") && f.FileName.EndsWith(".cs"), "should include service interface");
        result.Value.Files.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateServerFromContract_Java_GeneratesPomAndServiceInterface()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "java", "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Should().Contain(f => f.FileName == "pom.xml", "should include Maven POM");
        result.Value.Files.Should().Contain(f => f.FileName.Contains("Service.java"), "should include Spring service interface");
        result.Value.Files.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateServerFromContract_NodeJs_GeneratesPackageJsonAndApp()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "nodejs", "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Should().Contain(f => f.FileName == "package.json", "should include package.json");
        result.Value.Files.Should().Contain(f => f.FileName.Contains("app.js"), "should include app entrypoint");
        result.Value.Files.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateServerFromContract_Go_GeneratesGoModAndMain()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "go", "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Should().Contain(f => f.FileName == "go.mod", "should include go.mod module file");
        result.Value.Files.Should().Contain(f => f.FileName.Contains("main.go"), "should include main.go entrypoint");
        result.Value.Files.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateServerFromContract_Python_GeneratesPyprojectAndApp()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "python", "OrderService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Files.Should().Contain(f => f.FileName == "pyproject.toml", "should include pyproject.toml");
        result.Value.Files.Should().Contain(f => f.FileName.Contains("main.py"), "should include FastAPI entrypoint");
        result.Value.Files.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateServerFromContract_AllLanguages_IncludeServiceName()
    {
        var handler = new GenerateServerFromContract.Handler();
        var serviceName = "PaymentGateway";

        foreach (var lang in new[] { "dotnet", "java", "nodejs", "go", "python" })
        {
            var command = new GenerateServerFromContract.Command(ContractVersionId, lang, serviceName);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var allContent = string.Join("\n", result.Value.Files.Select(f => f.Content));
            allContent.Should().Contain(serviceName, $"language {lang} should reference service name");
        }
    }
}
